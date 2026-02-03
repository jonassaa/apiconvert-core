import { XMLBuilder, XMLParser } from "fast-xml-parser";
import {
  expect,
  test,
  type APIRequestContext,
  type BrowserContext,
  type Page,
} from "@playwright/test";

type WebhookEntry = {
  content?: string;
  headers?: Record<string, string[]>;
  method?: string;
  url?: string;
};

const xmlParser = new XMLParser({
  ignoreAttributes: false,
  attributeNamePrefix: "@_",
  allowBooleanAttributes: true,
  parseTagValue: true,
  parseAttributeValue: true,
  trimValues: true,
});

const xmlBuilder = new XMLBuilder({
  ignoreAttributes: false,
  attributeNamePrefix: "@_",
  format: false,
});

function parseXml(text: string) {
  return xmlParser.parse(text);
}

function extractRequests(payload: unknown): WebhookEntry[] {
  if (Array.isArray(payload)) return payload as WebhookEntry[];
  if (payload && typeof payload === "object") {
    const record = payload as Record<string, unknown>;
    if (Array.isArray(record.data)) {
      return record.data as WebhookEntry[];
    }
    if (Array.isArray(record.requests)) {
      return record.requests as WebhookEntry[];
    }
  }
  return [];
}

function getEntryUrl(entry: WebhookEntry) {
  const record = entry as Record<string, unknown>;
  const candidate =
    record.url ?? record.request_url ?? record.requestUrl ?? record.requestURI;
  return typeof candidate === "string" ? candidate : "";
}

async function createWebhookUrl(context: BrowserContext) {
  const webhookPage = await context.newPage();
  await webhookPage.goto("https://webhook.site", {
    waitUntil: "domcontentloaded",
  });

  const urlCode = webhookPage.locator("code[ga-event-action='copy-url']");
  await expect(urlCode).toBeVisible();
  await expect(urlCode).toHaveText(
    /https:\/\/webhook\.site\/[A-Za-z0-9-]+/
  );

  const webhookUrl = (await urlCode.innerText()).trim();
  const token = webhookUrl.split("/").pop();
  if (!token) {
    throw new Error(`Unable to parse webhook token from ${webhookUrl}`);
  }

  console.log(`Webhook URL: ${webhookUrl}`);
  return { webhookUrl, token };
}

async function waitForWebhookRequest(
  request: APIRequestContext,
  token: string,
  predicate: (entry: WebhookEntry) => boolean
) {
  return expect
    .poll(
      async () => {
        const res = await request.get(
          `https://webhook.site/token/${token}/requests?sorting=newest`,
          { headers: { Accept: "application/json" } }
        );
        if (!res.ok()) return null;
        const payload = (await res.json()) as unknown;
        const entries = extractRequests(payload);
        const matchEntry = entries.find(predicate);
        return matchEntry ?? null;
      },
      { timeout: 25_000, intervals: [1000, 2000, 3000, 5000] }
    )
    .not.toBeNull();
}

async function login(page: Page) {
  await page.goto("/login");
  await page.getByLabel(/email/i).fill("asdf@asdf.com");
  await page.getByLabel(/password/i).fill("P@ssw0rd");
  await page.getByRole("button", { name: /sign in with email/i }).click();
  await page.waitForURL(/\/org(\/|$)/);
}

async function createOrg(page: Page) {
  await page.goto("/org");
  const orgName = `E2E Organization ${Date.now()}`;
  await page.getByLabel(/organization name/i).fill(orgName);
  await page.getByRole("button", { name: /create organization/i }).click();
  await page.waitForURL(/\/org\/.+\/dashboard/);
  const url = new URL(page.url());
  return url.pathname.split("/")[2];
}

async function openRulesJsonEditor(page: Page) {
  await page.getByRole("tab", { name: /conversion/i }).click();
  await expect(page.getByRole("tab", { name: /conversion/i })).toHaveAttribute(
    "data-state",
    "active"
  );

  const rulesCard = page
    .locator("text=Rules JSON")
    .locator("xpath=ancestor::div[@data-slot='card']");

  const rulesTextarea = rulesCard.locator("textarea");
  await expect(rulesTextarea).toBeVisible();
  return rulesTextarea;
}

async function applyRulesJson(
  page: Page,
  rulesJson: string
) {
  const rulesTextarea = await openRulesJsonEditor(page);
  await rulesTextarea.fill(rulesJson);
  await page.getByRole("button", { name: /apply json/i }).click();
}

async function saveRules(page: Page) {
  await page.getByRole("button", { name: /save rules/i }).click();
  await expect(page.getByRole("status")).toContainText(/rules saved/i);
}

test.describe("conversion flow (complex)", () => {
  test("JSON array mapping forwards transformed payload", async ({
    page,
    context,
    request,
  }) => {
    test.setTimeout(30_000);
    const { webhookUrl, token } = await createWebhookUrl(context);

    await login(page);
    const orgId = await createOrg(page);

    await page.goto(`/org/${orgId}/converters/new`);
    const now = Date.now();
    const converterName = `JSON Arrays ${now}`;
    const inboundPath = `json-arrays-${now}`;
    await page.getByLabel(/^name$/i).fill(converterName);
    await page.getByLabel(/inbound path/i).fill(inboundPath);
    await page.getByLabel(/forward url/i).fill(webhookUrl);
    await page.getByRole("button", { name: /create converter/i }).click();
    await page.waitForURL(/\/org\/.+\/converters\/.+/);

    const marker = `json-${Date.now()}`;
    const inputPayload = {
      marker,
      orderId: "A123",
      customer: { name: "Ada" },
      items: [
        { sku: "X", qty: 2 },
        { sku: "Y", qty: 1 },
      ],
    };

    const outputPayload = {
      marker,
      id: "A123",
      buyer: "Ada",
      lines: [
        { sku: "X", qty: 2 },
        { sku: "Y", qty: 1 },
      ],
    };

    const rules = {
      version: 2,
      inputFormat: "json",
      outputFormat: "json",
      fieldMappings: [
        {
          outputPath: "marker",
          source: { type: "path", path: "marker" },
          defaultValue: "",
        },
        {
          outputPath: "id",
          source: { type: "path", path: "orderId" },
          defaultValue: "",
        },
        {
          outputPath: "buyer",
          source: { type: "path", path: "customer.name" },
          defaultValue: "",
        },
      ],
      arrayMappings: [
        {
          inputPath: "items",
          outputPath: "lines",
          coerceSingle: true,
          itemMappings: [
            {
              outputPath: "sku",
              source: { type: "path", path: "sku" },
              defaultValue: "",
            },
            {
              outputPath: "qty",
              source: { type: "path", path: "qty" },
              defaultValue: "",
            },
          ],
        },
      ],
    };

    await applyRulesJson(page, JSON.stringify(rules, null, 2));
    await page.getByRole("textbox", { name: /example input/i }).fill(
      JSON.stringify(inputPayload, null, 2)
    );
    await page.getByRole("textbox", { name: /expected output/i }).fill(
      JSON.stringify(outputPayload, null, 2)
    );

    await saveRules(page);

    const inboundResponse = await request.post(
      `/api/inbound/${orgId}/${inboundPath}`,
      {
        data: inputPayload,
        headers: { "Content-Type": "application/json" },
      }
    );
    expect(inboundResponse.ok()).toBeTruthy();

    await waitForWebhookRequest(request, token, (entry) =>
      (entry.content || "").includes(marker)
    );

    const payloadRes = await request.get(
      `https://webhook.site/token/${token}/requests?sorting=newest`,
      { headers: { Accept: "application/json" } }
    );
    const payloadJson = (await payloadRes.json()) as unknown;
    const entries = extractRequests(payloadJson);
    const matchEntry = entries.find((entry) =>
      (entry.content || "").includes(marker)
    );
    expect(matchEntry).toBeTruthy();
    const forwarded = JSON.parse(matchEntry?.content || "{}");
    expect(forwarded).toEqual(outputPayload);
  });

  test("XML input with array mapping forwards JSON output", async ({
    page,
    context,
    request,
  }) => {
    test.setTimeout(30_000);
    const { webhookUrl, token } = await createWebhookUrl(context);

    await login(page);
    const orgId = await createOrg(page);

    await page.goto(`/org/${orgId}/converters/new`);
    const now = Date.now();
    const converterName = `XML Arrays ${now}`;
    const inboundPath = `xml-arrays-${now}`;
    await page.getByLabel(/^name$/i).fill(converterName);
    await page.getByLabel(/inbound path/i).fill(inboundPath);
    await page.getByLabel(/forward url/i).fill(webhookUrl);
    await page.getByRole("button", { name: /create converter/i }).click();
    await page.waitForURL(/\/org\/.+\/converters\/.+/);

    const marker = `xml-${Date.now()}`;
    const xmlInput = `<?xml version=\"1.0\"?>\n<order>\n  <marker>${marker}</marker>\n  <orderId>B234</orderId>\n  <customer>\n    <name>Linus</name>\n  </customer>\n  <items>\n    <item>\n      <sku>Z1</sku>\n      <qty>3</qty>\n    </item>\n    <item>\n      <sku>Z2</sku>\n      <qty>4</qty>\n    </item>\n  </items>\n</order>`;

    const outputPayload = {
      marker,
      id: "B234",
      buyer: "Linus",
      lines: [
        { sku: "Z1", qty: 3 },
        { sku: "Z2", qty: 4 },
      ],
    };

    const rules = {
      version: 2,
      inputFormat: "xml",
      outputFormat: "json",
      fieldMappings: [
        {
          outputPath: "marker",
          source: { type: "path", path: "order.marker" },
          defaultValue: "",
        },
        {
          outputPath: "id",
          source: { type: "path", path: "order.orderId" },
          defaultValue: "",
        },
        {
          outputPath: "buyer",
          source: { type: "path", path: "order.customer.name" },
          defaultValue: "",
        },
      ],
      arrayMappings: [
        {
          inputPath: "order.items.item",
          outputPath: "lines",
          coerceSingle: true,
          itemMappings: [
            {
              outputPath: "sku",
              source: { type: "path", path: "sku" },
              defaultValue: "",
            },
            {
              outputPath: "qty",
              source: { type: "path", path: "qty" },
              defaultValue: "",
            },
          ],
        },
      ],
    };

    await applyRulesJson(page, JSON.stringify(rules, null, 2));
    await page.getByRole("textbox", { name: /example input/i }).fill(xmlInput);
    await page.getByRole("textbox", { name: /expected output/i }).fill(
      JSON.stringify(outputPayload, null, 2)
    );

    await saveRules(page);

    const inboundResponse = await request.post(
      `/api/inbound/${orgId}/${inboundPath}`,
      {
        data: xmlInput,
        headers: { "Content-Type": "application/xml" },
      }
    );
    expect(inboundResponse.ok()).toBeTruthy();

    await waitForWebhookRequest(request, token, (entry) =>
      (entry.content || "").includes(marker)
    );

    const payloadRes = await request.get(
      `https://webhook.site/token/${token}/requests?sorting=newest`,
      { headers: { Accept: "application/json" } }
    );
    const payloadJson = (await payloadRes.json()) as unknown;
    const entries = extractRequests(payloadJson);
    const matchEntry = entries.find((entry) =>
      (entry.content || "").includes(marker)
    );
    expect(matchEntry).toBeTruthy();
    const forwarded = JSON.parse(matchEntry?.content || "{}");
    expect(forwarded).toEqual(outputPayload);
  });

  test("GET inbound query maps to JSON and forwards payload", async ({
    page,
    context,
    request,
  }) => {
    test.setTimeout(30_000);
    const { webhookUrl, token } = await createWebhookUrl(context);

    await login(page);
    const orgId = await createOrg(page);

    await page.goto(`/org/${orgId}/converters/new`);
    const now = Date.now();
    const converterName = `Query Inbound ${now}`;
    const inboundPath = `query-inbound-${now}`;
    await page.getByLabel(/^name$/i).fill(converterName);
    await page.getByLabel(/inbound path/i).fill(inboundPath);
    await page.getByLabel(/forward url/i).fill(webhookUrl);
    await page.getByRole("button", { name: /create converter/i }).click();
    await page.waitForURL(/\/org\/.+\/converters\/.+/);

    const marker = `query-${Date.now()}`;
    const rules = {
      version: 2,
      inputFormat: "query",
      outputFormat: "json",
      fieldMappings: [
        {
          outputPath: "marker",
          source: { type: "path", path: "marker" },
          defaultValue: "",
        },
        {
          outputPath: "contact.email",
          source: { type: "path", path: "customer.email" },
          defaultValue: "",
        },
        {
          outputPath: "plan",
          source: { type: "path", path: "plan" },
          defaultValue: "",
        },
        {
          outputPath: "tags",
          source: { type: "path", path: "tags" },
          defaultValue: "",
        },
      ],
      arrayMappings: [],
    };

    await applyRulesJson(page, JSON.stringify(rules, null, 2));
    await saveRules(page);

    const inboundResponse = await request.get(
      `/api/inbound/${orgId}/${inboundPath}?marker=${marker}&customer.email=sample@acme.io&plan=premium&tags=blue&tags=gold`
    );
    expect(inboundResponse.ok()).toBeTruthy();

    await waitForWebhookRequest(request, token, (entry) =>
      (entry.content || "").includes(marker)
    );

    const payloadRes = await request.get(
      `https://webhook.site/token/${token}/requests?sorting=newest`,
      { headers: { Accept: "application/json" } }
    );
    const payloadJson = (await payloadRes.json()) as unknown;
    const entries = extractRequests(payloadJson);
    const matchEntry = entries.find((entry) =>
      (entry.content || "").includes(marker)
    );
    expect(matchEntry).toBeTruthy();
    const forwarded = JSON.parse(matchEntry?.content || "{}");
    expect(forwarded).toEqual({
      marker,
      contact: { email: "sample@acme.io" },
      plan: "premium",
      tags: ["blue", "gold"],
    });
  });

  test("JSON input forwards query output", async ({ page, context, request }) => {
    test.setTimeout(30_000);
    const { webhookUrl, token } = await createWebhookUrl(context);

    await login(page);
    const orgId = await createOrg(page);

    await page.goto(`/org/${orgId}/converters/new`);
    const now = Date.now();
    const converterName = `Query Output ${now}`;
    const inboundPath = `query-output-${now}`;
    await page.getByLabel(/^name$/i).fill(converterName);
    await page.getByLabel(/inbound path/i).fill(inboundPath);
    await page.getByLabel(/forward url/i).fill(webhookUrl);
    await page.getByRole("button", { name: /create converter/i }).click();
    await page.waitForURL(/\/org\/.+\/converters\/.+/);

    const marker = `query-output-${Date.now()}`;
    const email = "query@acme.io";
    const rules = {
      version: 2,
      inputFormat: "json",
      outputFormat: "query",
      fieldMappings: [
        {
          outputPath: "marker",
          source: { type: "path", path: "marker" },
          defaultValue: "",
        },
        {
          outputPath: "customer.email",
          source: { type: "path", path: "customer.email" },
          defaultValue: "",
        },
        {
          outputPath: "plan",
          source: { type: "path", path: "plan" },
          defaultValue: "",
        },
      ],
      arrayMappings: [],
    };

    await applyRulesJson(page, JSON.stringify(rules, null, 2));
    await saveRules(page);

    const inboundResponse = await request.post(
      `/api/inbound/${orgId}/${inboundPath}`,
      {
        data: { marker, customer: { email }, plan: "premium" },
        headers: { "Content-Type": "application/json" },
      }
    );
    expect(inboundResponse.ok()).toBeTruthy();

    await waitForWebhookRequest(request, token, (entry) =>
      getEntryUrl(entry).includes(marker)
    );

    const payloadRes = await request.get(
      `https://webhook.site/token/${token}/requests?sorting=newest`,
      { headers: { Accept: "application/json" } }
    );
    const payloadJson = (await payloadRes.json()) as unknown;
    const entries = extractRequests(payloadJson);
    const matchEntry = entries.find((entry) =>
      getEntryUrl(entry).includes(marker)
    );
    expect(matchEntry).toBeTruthy();
    const entryUrl = getEntryUrl(matchEntry as WebhookEntry);
    expect(entryUrl).toContain(`marker=${marker}`);
    expect(entryUrl).toContain(`customer.email=${encodeURIComponent(email)}`);
    expect(entryUrl).toContain("plan=premium");
  });

  test("inbound and outbound bearer auth are enforced", async ({
    page,
    context,
    request,
  }) => {
    test.setTimeout(30_000);
    const { webhookUrl, token } = await createWebhookUrl(context);

    await login(page);
    const orgId = await createOrg(page);

    await page.goto(`/org/${orgId}/converters/new`);
    const now = Date.now();
    const converterName = `Auth Flow ${now}`;
    const inboundPath = `auth-flow-${now}`;
    await page.getByLabel(/^name$/i).fill(converterName);
    await page.getByLabel(/inbound path/i).fill(inboundPath);
    await page.getByLabel(/forward url/i).fill(webhookUrl);

    const inboundToken = `inbound-${now}`;
    const outboundToken = `outbound-${now}`;

    await page.locator("#inbound_auth_mode").click();
    await page.getByRole("option", { name: /bearer token/i }).click();
    await page.getByLabel(/inbound token/i).fill(inboundToken);

    await page.locator("#outbound_auth_mode").click();
    await page.getByRole("option", { name: /bearer token/i }).click();
    await page.getByLabel(/outbound token/i).fill(outboundToken);

    await page.getByRole("button", { name: /create converter/i }).click();
    await page.waitForURL(/\/org\/.+\/converters\/.+/);

    const marker = `auth-${Date.now()}`;
    const rules = {
      version: 2,
      inputFormat: "json",
      outputFormat: "json",
      fieldMappings: [
        {
          outputPath: "marker",
          source: { type: "path", path: "marker" },
          defaultValue: "",
        },
      ],
      arrayMappings: [],
    };

    await applyRulesJson(page, JSON.stringify(rules, null, 2));
    await page.getByRole("textbox", { name: /example input/i }).fill(
      JSON.stringify({ marker }, null, 2)
    );
    await page.getByRole("textbox", { name: /expected output/i }).fill(
      JSON.stringify({ marker }, null, 2)
    );
    await saveRules(page);

    const unauthorized = await request.post(
      `/api/inbound/${orgId}/${inboundPath}`,
      {
        data: { marker },
        headers: { "Content-Type": "application/json" },
      }
    );
    expect(unauthorized.status()).toBe(401);

    const authorized = await request.post(
      `/api/inbound/${orgId}/${inboundPath}`,
      {
        data: { marker },
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${inboundToken}`,
        },
      }
    );
    expect(authorized.ok()).toBeTruthy();

    await waitForWebhookRequest(request, token, (entry) =>
      (entry.content || "").includes(marker)
    );

    const payloadRes = await request.get(
      `https://webhook.site/token/${token}/requests?sorting=newest`,
      { headers: { Accept: "application/json" } }
    );
    const payloadJson = (await payloadRes.json()) as unknown;
    const entries = extractRequests(payloadJson);
    const matchEntry = entries.find((entry) =>
      (entry.content || "").includes(marker)
    );
    expect(matchEntry).toBeTruthy();
    const authHeader =
      matchEntry?.headers?.authorization?.[0] ??
      matchEntry?.headers?.Authorization?.[0];
    expect(authHeader).toBe(`Bearer ${outboundToken}`);
  });

  test("JSON input forwards XML output", async ({ page, context, request }) => {
    test.setTimeout(30_000);
    const { webhookUrl, token } = await createWebhookUrl(context);

    await login(page);
    const orgId = await createOrg(page);

    await page.goto(`/org/${orgId}/converters/new`);
    const now = Date.now();
    const converterName = `JSON XML ${now}`;
    const inboundPath = `json-xml-${now}`;
    await page.getByLabel(/^name$/i).fill(converterName);
    await page.getByLabel(/inbound path/i).fill(inboundPath);
    await page.getByLabel(/forward url/i).fill(webhookUrl);
    await page.getByRole("button", { name: /create converter/i }).click();
    await page.waitForURL(/\/org\/.+\/converters\/.+/);

    const marker = `jsonxml-${Date.now()}`;
    const inputPayload = {
      marker,
      orderId: "C345",
      customer: { name: "Grace" },
      items: [
        { sku: "K1", qty: 5 },
        { sku: "K2", qty: 6 },
      ],
    };

    const outputPayload = {
      invoice: {
        marker,
        id: "C345",
        customer: "Grace",
        lines: [
          { sku: "K1", qty: 5 },
          { sku: "K2", qty: 6 },
        ],
      },
    };

    const rules = {
      version: 2,
      inputFormat: "json",
      outputFormat: "xml",
      fieldMappings: [
        {
          outputPath: "invoice.marker",
          source: { type: "path", path: "marker" },
          defaultValue: "",
        },
        {
          outputPath: "invoice.id",
          source: { type: "path", path: "orderId" },
          defaultValue: "",
        },
        {
          outputPath: "invoice.customer",
          source: { type: "path", path: "customer.name" },
          defaultValue: "",
        },
      ],
      arrayMappings: [
        {
          inputPath: "items",
          outputPath: "invoice.lines",
          coerceSingle: true,
          itemMappings: [
            {
              outputPath: "sku",
              source: { type: "path", path: "sku" },
              defaultValue: "",
            },
            {
              outputPath: "qty",
              source: { type: "path", path: "qty" },
              defaultValue: "",
            },
          ],
        },
      ],
    };

    await applyRulesJson(page, JSON.stringify(rules, null, 2));
    await page.getByRole("textbox", { name: /example input/i }).fill(
      JSON.stringify(inputPayload, null, 2)
    );
    const expectedXml = xmlBuilder.build(outputPayload);
    await page.getByRole("textbox", { name: /expected output/i }).fill(expectedXml);

    await saveRules(page);

    const inboundResponse = await request.post(
      `/api/inbound/${orgId}/${inboundPath}`,
      {
        data: inputPayload,
        headers: { "Content-Type": "application/json" },
      }
    );
    expect(inboundResponse.ok()).toBeTruthy();

    await waitForWebhookRequest(request, token, (entry) =>
      (entry.content || "").includes(marker)
    );

    const payloadRes = await request.get(
      `https://webhook.site/token/${token}/requests?sorting=newest`,
      { headers: { Accept: "application/json" } }
    );
    const payloadJson = (await payloadRes.json()) as unknown;
    const entries = extractRequests(payloadJson);
    const matchEntry = entries.find((entry) =>
      (entry.content || "").includes(marker)
    );
    expect(matchEntry).toBeTruthy();
    const forwardedXml = matchEntry?.content || "";
    expect(parseXml(forwardedXml)).toEqual(parseXml(expectedXml));
  });

  test("XML input forwards XML output", async ({ page, context, request }) => {
    test.setTimeout(30_000);
    const { webhookUrl, token } = await createWebhookUrl(context);

    await login(page);
    const orgId = await createOrg(page);

    await page.goto(`/org/${orgId}/converters/new`);
    const now = Date.now();
    const converterName = `XML XML ${now}`;
    const inboundPath = `xml-xml-${now}`;
    await page.getByLabel(/^name$/i).fill(converterName);
    await page.getByLabel(/inbound path/i).fill(inboundPath);
    await page.getByLabel(/forward url/i).fill(webhookUrl);
    await page.getByRole("button", { name: /create converter/i }).click();
    await page.waitForURL(/\/org\/.+\/converters\/.+/);

    const marker = `xmlxml-${Date.now()}`;
    const xmlInput = `<?xml version=\"1.0\"?>\n<order>\n  <marker>${marker}</marker>\n  <id>D456</id>\n  <customer>\n    <name>Jo</name>\n  </customer>\n  <items>\n    <item>\n      <sku>P1</sku>\n      <qty>7</qty>\n    </item>\n    <item>\n      <sku>P2</sku>\n      <qty>8</qty>\n    </item>\n  </items>\n</order>`;

    const outputPayload = {
      shipment: {
        marker,
        id: "D456",
        customer: "Jo",
        lines: [
          { sku: "P1", qty: 7 },
          { sku: "P2", qty: 8 },
        ],
      },
    };

    const rules = {
      version: 2,
      inputFormat: "xml",
      outputFormat: "xml",
      fieldMappings: [
        {
          outputPath: "shipment.marker",
          source: { type: "path", path: "order.marker" },
          defaultValue: "",
        },
        {
          outputPath: "shipment.id",
          source: { type: "path", path: "order.id" },
          defaultValue: "",
        },
        {
          outputPath: "shipment.customer",
          source: { type: "path", path: "order.customer.name" },
          defaultValue: "",
        },
      ],
      arrayMappings: [
        {
          inputPath: "order.items.item",
          outputPath: "shipment.lines",
          coerceSingle: true,
          itemMappings: [
            {
              outputPath: "sku",
              source: { type: "path", path: "sku" },
              defaultValue: "",
            },
            {
              outputPath: "qty",
              source: { type: "path", path: "qty" },
              defaultValue: "",
            },
          ],
        },
      ],
    };

    await applyRulesJson(page, JSON.stringify(rules, null, 2));
    await page.getByRole("textbox", { name: /example input/i }).fill(xmlInput);
    const expectedXml = xmlBuilder.build(outputPayload);
    await page.getByRole("textbox", { name: /expected output/i }).fill(expectedXml);

    await saveRules(page);

    const inboundResponse = await request.post(
      `/api/inbound/${orgId}/${inboundPath}`,
      {
        data: xmlInput,
        headers: { "Content-Type": "application/xml" },
      }
    );
    expect(inboundResponse.ok()).toBeTruthy();

    await waitForWebhookRequest(request, token, (entry) =>
      (entry.content || "").includes(marker)
    );

    const payloadRes = await request.get(
      `https://webhook.site/token/${token}/requests?sorting=newest`,
      { headers: { Accept: "application/json" } }
    );
    const payloadJson = (await payloadRes.json()) as unknown;
    const entries = extractRequests(payloadJson);
    const matchEntry = entries.find((entry) =>
      (entry.content || "").includes(marker)
    );
    expect(matchEntry).toBeTruthy();
    const forwardedXml = matchEntry?.content || "";
    expect(parseXml(forwardedXml)).toEqual(parseXml(expectedXml));
  });
});

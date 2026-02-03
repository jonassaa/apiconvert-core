import { expect, test, type APIRequestContext, type BrowserContext, type Page } from "@playwright/test";

type WebhookEntry = {
  content?: string;
  headers?: Record<string, string[]>;
  method?: string;
};

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

async function saveSettings(page: Page) {
  await Promise.all([
    page.waitForResponse(
      (response) =>
        ["POST", "PATCH"].includes(response.request().method()) &&
        /\/converters\//.test(response.url())
    ),
    page.getByRole("button", { name: /save settings/i }).click(),
  ]);
  await expect(page.getByRole("status")).toContainText(/settings saved/i);
}

async function saveAuth(page: Page) {
  await Promise.all([
    page.waitForResponse(
      (response) =>
        ["POST", "PATCH"].includes(response.request().method()) &&
        /\/converters\//.test(response.url())
    ),
    page.getByRole("button", { name: /save authentication/i }).click(),
  ]);
  await expect(page.getByRole("status")).toContainText(/authentication saved/i);
}

async function openOverviewTab(page: Page) {
  await page.getByRole("tab", { name: /overview/i }).click();
  await expect(page.getByRole("tab", { name: /overview/i })).toHaveAttribute(
    "data-state",
    "active"
  );
}

async function postInboundWithRetry(
  request: APIRequestContext,
  path: string,
  options: Parameters<APIRequestContext["post"]>[1]
) {
  const delays = [0, 250, 500, 1000, 2000];
  let response = await request.post(path, options);
  for (const delay of delays) {
    if (response.status() !== 404) return response;
    await new Promise((resolve) => setTimeout(resolve, delay));
    response = await request.post(path, options);
  }
  return response;
}

test.describe("converter admin coverage", () => {
  test("rejects invalid forward URL", async ({ page }) => {
    await login(page);
    const orgId = await createOrg(page);

    await page.goto(`/org/${orgId}/converters/new`);
    const now = Date.now();
    await page.getByLabel(/^name$/i).fill(`Bad Forward ${now}`);
    await page.getByLabel(/inbound path/i).fill(`bad-forward-${now}`);
    await page.getByLabel(/forward url/i).fill("http://localhost:123/test");
    await page.getByRole("button", { name: /create converter/i }).click();

    await expect(
      page.getByText(/forward url cannot target local hosts/i)
    ).toBeVisible();
  });

  test("shows error for invalid rules JSON", async ({ page }) => {
    await login(page);
    const orgId = await createOrg(page);

    await page.goto(`/org/${orgId}/converters/new`);
    const now = Date.now();
    await page.getByLabel(/^name$/i).fill(`Rules JSON ${now}`);
    await page.getByLabel(/inbound path/i).fill(`rules-json-${now}`);
    await page.getByLabel(/forward url/i).fill("https://example.com/webhook");
    await page.getByRole("button", { name: /create converter/i }).click();
    await page.waitForURL(/\/org\/.+\/converters\/.+/);

    await page.getByRole("tab", { name: /conversion/i }).click();
    const rulesCard = page
      .locator("text=Rules JSON")
      .locator("xpath=ancestor::div[@data-slot='card']");
    const rulesTextarea = rulesCard.locator("textarea");
    await rulesTextarea.fill("{");
    await page.getByRole("button", { name: /apply json/i }).click();
    await expect(page.getByText(/rules json is invalid/i)).toBeVisible();
  });

  test("updates settings, toggles enabled, and deletes converter", async ({ page, request }) => {
    await login(page);
    const orgId = await createOrg(page);

    await page.goto(`/org/${orgId}/converters/new`);
    const now = Date.now();
    const converterName = `Admin Converter ${now}`;
    const inboundPath = `admin-${now}`;
    await page.getByLabel(/^name$/i).fill(converterName);
    await page.getByLabel(/inbound path/i).fill(inboundPath);
    await page.getByLabel(/forward url/i).fill("https://example.com/webhook");
    await page.getByRole("button", { name: /create converter/i }).click();
    await page.waitForURL(/\/org\/.+\/converters\/.+/);

    await page.getByLabel(/inbound path/i).fill(inboundPath);

    await openOverviewTab(page);
    const enabledToggle = page.locator("input[name='enabled']");
    await enabledToggle.setChecked(false);
    await saveSettings(page);

    const disabledResponse = await postInboundWithRetry(
      request,
      `/api/inbound/${orgId}/${inboundPath}`,
      {
        data: { ping: true },
        headers: { "Content-Type": "application/json" },
      }
    );
    expect(disabledResponse.status()).toBe(403);

    await openOverviewTab(page);
    await enabledToggle.setChecked(true);
    await saveSettings(page);

    await expect
      .poll(
        async () => {
          const response = await postInboundWithRetry(
            request,
            `/api/inbound/${orgId}/${inboundPath}`,
            {
              data: { ping: true },
              headers: { "Content-Type": "application/json" },
            }
          );
          return response.status();
        },
        { timeout: 5000, intervals: [500, 1000, 1500] }
      )
      .not.toBe(403);

    await page.goto(`/org/${orgId}/converters`);
    const row = page.getByRole("row", { name: new RegExp(converterName, "i") });
    await expect(row).toContainText(/enabled/i);

    page.once("dialog", (dialog) => dialog.accept());
    await row.getByRole("button", { name: /delete/i }).click();
    await expect(page.getByText(converterName)).not.toBeVisible();
  });

  test("ack response mode returns 202", async ({ page, context, request }) => {
    const { webhookUrl } = await createWebhookUrl(context);
    await login(page);
    const orgId = await createOrg(page);

    await page.goto(`/org/${orgId}/converters/new`);
    const now = Date.now();
    const inboundPath = `ack-${now}`;
    await page.getByLabel(/^name$/i).fill(`ACK Converter ${now}`);
    await page.getByLabel(/inbound path/i).fill(inboundPath);
    await page.getByLabel(/forward url/i).fill(webhookUrl);
    await page.getByRole("button", { name: /create converter/i }).click();
    await page.waitForURL(/\/org\/.+\/converters\/.+/);

    await openOverviewTab(page);
    await page.locator("#inbound_response_mode").click();
    const ackOption = page.getByRole("option", {
      name: /return minimal ack/i,
    });
    await expect(ackOption).toBeVisible();
    await ackOption.click();
    await expect(
      page.locator("input[name='inbound_response_mode']")
    ).toHaveValue("ack");
    await saveSettings(page);
    await page.reload();
    await expect(
      page.locator("input[name='inbound_response_mode']")
    ).toHaveValue("ack");

    const response = await postInboundWithRetry(
      request,
      `/api/inbound/${orgId}/${inboundPath}`,
      {
        data: { ack: true },
        headers: { "Content-Type": "application/json" },
      }
    );
    expect(response.status()).toBe(202);
    const body = (await response.json()) as { ok?: boolean; request_id?: string };
    expect(body.ok).toBe(true);
    expect(body.request_id).toBeTruthy();
  });

  test("inbound basic auth is enforced", async ({ page, request }) => {
    await login(page);
    const orgId = await createOrg(page);

    await page.goto(`/org/${orgId}/converters/new`);
    const now = Date.now();
    const inboundPath = `basic-auth-${now}`;
    await page.getByLabel(/^name$/i).fill(`Basic Auth ${now}`);
    await page.getByLabel(/inbound path/i).fill(inboundPath);
    await page.getByLabel(/forward url/i).fill("https://example.com/webhook");

    await page.locator("#inbound_auth_mode").click();
    await page.getByRole("option", { name: /basic auth/i }).click();
    await page.getByLabel(/inbound username/i).fill("alice");
    await page.getByLabel(/inbound password/i).fill("s3cret");

    await page.getByRole("button", { name: /create converter/i }).click();
    await page.waitForURL(/\/org\/.+\/converters\/.+/);

    const unauthorized = await postInboundWithRetry(
      request,
      `/api/inbound/${orgId}/${inboundPath}`,
      {
        data: { ping: true },
        headers: { "Content-Type": "application/json" },
      }
    );
    expect(unauthorized.status()).toBe(401);

    const token = Buffer.from("alice:s3cret").toString("base64");
    const authorized = await postInboundWithRetry(
      request,
      `/api/inbound/${orgId}/${inboundPath}`,
      {
        data: { ping: true },
        headers: {
          "Content-Type": "application/json",
          Authorization: `Basic ${token}`,
        },
      }
    );
    expect(authorized.status()).not.toBe(401);
  });

  test("inbound custom header auth is enforced", async ({ page, request }) => {
    await login(page);
    const orgId = await createOrg(page);

    await page.goto(`/org/${orgId}/converters/new`);
    const now = Date.now();
    const inboundPath = `header-auth-${now}`;
    await page.getByLabel(/^name$/i).fill(`Header Auth ${now}`);
    await page.getByLabel(/inbound path/i).fill(inboundPath);
    await page.getByLabel(/forward url/i).fill("https://example.com/webhook");

    await page.locator("#inbound_auth_mode").click();
    await page.getByRole("option", { name: /custom header/i }).click();
    await page.getByLabel(/inbound header name/i).fill("X-Auth");
    await page.getByLabel(/inbound header value/i).fill("header-secret");

    await page.getByRole("button", { name: /create converter/i }).click();
    await page.waitForURL(/\/org\/.+\/converters\/.+/);

    const unauthorized = await postInboundWithRetry(
      request,
      `/api/inbound/${orgId}/${inboundPath}`,
      {
        data: { ping: true },
        headers: { "Content-Type": "application/json" },
      }
    );
    expect(unauthorized.status()).toBe(401);

    const authorized = await postInboundWithRetry(
      request,
      `/api/inbound/${orgId}/${inboundPath}`,
      {
        data: { ping: true },
        headers: { "Content-Type": "application/json", "X-Auth": "header-secret" },
      }
    );
    expect(authorized.status()).not.toBe(401);
  });

  test("outbound basic auth attaches Authorization header", async ({
    page,
    context,
    request,
  }) => {
    const { webhookUrl, token } = await createWebhookUrl(context);
    await login(page);
    const orgId = await createOrg(page);

    await page.goto(`/org/${orgId}/converters/new`);
    const now = Date.now();
    const inboundPath = `outbound-basic-${now}`;
    await page.getByLabel(/^name$/i).fill(`Outbound Basic ${now}`);
    await page.getByLabel(/inbound path/i).fill(inboundPath);
    await page.getByLabel(/forward url/i).fill(webhookUrl);

    await page.locator("#outbound_auth_mode").click();
    await page.getByRole("option", { name: /basic auth/i }).click();
    await page.getByLabel(/outbound username/i).fill("shipper");
    await page.getByLabel(/outbound password/i).fill("truck");

    await page.getByRole("button", { name: /create converter/i }).click();
    await page.waitForURL(/\/org\/.+\/converters\/.+/);

    const marker = `basic-outbound-${Date.now()}`;
    const response = await postInboundWithRetry(
      request,
      `/api/inbound/${orgId}/${inboundPath}`,
      {
        data: { marker },
        headers: { "Content-Type": "application/json" },
      }
    );
    expect(response.ok()).toBeTruthy();

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
    const authHeader =
      matchEntry?.headers?.authorization?.[0] ??
      matchEntry?.headers?.Authorization?.[0];
    const expected = Buffer.from("shipper:truck").toString("base64");
    expect(authHeader).toBe(`Basic ${expected}`);
  });

  test("forwards static and custom headers", async ({
    page,
    context,
    request,
  }) => {
    const { webhookUrl, token } = await createWebhookUrl(context);
    await login(page);
    const orgId = await createOrg(page);

    await page.goto(`/org/${orgId}/converters/new`);
    const now = Date.now();
    const inboundPath = `header-forward-${now}`;
    await page.getByLabel(/^name$/i).fill(`Header Forward ${now}`);
    await page.getByLabel(/inbound path/i).fill(inboundPath);
    await page.getByLabel(/forward url/i).fill(webhookUrl);
    await page.getByRole("button", { name: /create converter/i }).click();
    await page.waitForURL(/\/org\/.+\/converters\/.+/);

    await openOverviewTab(page);
    await page.getByLabel(/forward headers json/i).fill(
      JSON.stringify({ "X-Static": "static-value" }, null, 2)
    );
    await expect(page.getByLabel(/forward headers json/i)).toHaveValue(
      /X-Static/
    );
    await saveSettings(page);
    await page.reload();
    await expect(page.getByLabel(/forward headers json/i)).toHaveValue(
      /X-Static/
    );

    await page.getByRole("tab", { name: /authentication/i }).click();
    await page.getByLabel(/outbound header name/i).fill("X-Custom");
    await page.getByLabel(/outbound header value/i).fill("custom-value");
    await saveAuth(page);
    await page.getByRole("tab", { name: /overview/i }).click();
    await expect(page.getByLabel(/forward headers json/i)).toHaveValue(
      /X-Static/
    );
    await expect(page.getByLabel(/forward headers json/i)).toHaveValue(
      /X-Custom/
    );

    const marker = `headers-${Date.now()}`;
    const response = await postInboundWithRetry(
      request,
      `/api/inbound/${orgId}/${inboundPath}`,
      {
        data: { marker },
        headers: { "Content-Type": "application/json" },
      }
    );
    expect(response.ok()).toBeTruthy();

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
    const staticHeader =
      matchEntry?.headers?.["x-static"]?.[0] ??
      matchEntry?.headers?.["X-Static"]?.[0];
    const customHeader =
      matchEntry?.headers?.["x-custom"]?.[0] ??
      matchEntry?.headers?.["X-Custom"]?.[0];
    expect(staticHeader).toBe("static-value");
    expect(customHeader).toBe("custom-value");
  });

  test("log requests toggle controls log visibility", async ({
    page,
    context,
    request,
  }) => {
    const { webhookUrl } = await createWebhookUrl(context);
    await login(page);
    const orgId = await createOrg(page);

    await page.goto(`/org/${orgId}/converters/new`);
    const now = Date.now();
    const converterName = `Logs Off ${now}`;
    const inboundPath = `logs-off-${now}`;
    await page.getByLabel(/^name$/i).fill(converterName);
    await page.getByLabel(/inbound path/i).fill(inboundPath);
    await page.getByLabel(/forward url/i).fill(webhookUrl);
    await page.getByLabel(/log requests/i).uncheck();
    await page.getByRole("button", { name: /create converter/i }).click();
    await page.waitForURL(/\/org\/.+\/converters\/.+/);

    const response = await postInboundWithRetry(
      request,
      `/api/inbound/${orgId}/${inboundPath}`,
      {
        data: { log: false },
        headers: { "Content-Type": "application/json" },
      }
    );
    expect(response.ok()).toBeTruthy();

    await page.goto(`/org/${orgId}/logs?q=${encodeURIComponent(converterName)}`);
    await expect(page.getByText(/no logs found/i)).toBeVisible();

    await page.goto(`/org/${orgId}/converters/new`);
    const onName = `Logs On ${now}`;
    const onPath = `logs-on-${now}`;
    await page.getByLabel(/^name$/i).fill(onName);
    await page.getByLabel(/inbound path/i).fill(onPath);
    await page.getByLabel(/forward url/i).fill(webhookUrl);
    await page.getByRole("button", { name: /create converter/i }).click();
    await page.waitForURL(/\/org\/.+\/converters\/.+/);

    const responseOn = await postInboundWithRetry(
      request,
      `/api/inbound/${orgId}/${onPath}`,
      {
        data: { log: true },
        headers: { "Content-Type": "application/json" },
      }
    );
    expect(responseOn.ok()).toBeTruthy();

    await page.goto(`/org/${orgId}/logs?q=${encodeURIComponent(onName)}`);
    await expect(page.getByRole("cell", { name: onName })).toBeVisible();
  });

  test("rejects payloads larger than limit", async ({ page, request }) => {
    await login(page);
    const orgId = await createOrg(page);

    await page.goto(`/org/${orgId}/converters/new`);
    const now = Date.now();
    const inboundPath = `large-${now}`;
    await page.getByLabel(/^name$/i).fill(`Large Payload ${now}`);
    await page.getByLabel(/inbound path/i).fill(inboundPath);
    await page.getByLabel(/forward url/i).fill("https://example.com/webhook");
    await page.getByRole("button", { name: /create converter/i }).click();
    await page.waitForURL(/\/org\/.+\/converters\/.+/);

    const bigPayload = { data: "a".repeat(1_000_100) };
    const response = await postInboundWithRetry(
      request,
      `/api/inbound/${orgId}/${inboundPath}`,
      {
        data: bigPayload,
        headers: { "Content-Type": "application/json" },
      }
    );
    expect(response.status()).toBe(413);
  });
});

import { expect, test } from "@playwright/test";

function extractRequests(payload: unknown): Array<Record<string, unknown>> {
  if (Array.isArray(payload)) return payload as Array<Record<string, unknown>>;
  if (payload && typeof payload === "object") {
    const record = payload as Record<string, unknown>;
    if (Array.isArray(record.data)) {
      return record.data as Array<Record<string, unknown>>;
    }
    if (Array.isArray(record.requests)) {
      return record.requests as Array<Record<string, unknown>>;
    }
  }
  return [];
}

test.describe("forwarding", () => {
  test("forwards inbound request to webhook.site", async ({ page, context, request }) => {
    test.setTimeout(30_000);

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
    if (!/^https:\/\/webhook\.site\/[A-Za-z0-9-]+/.test(webhookUrl)) {
      throw new Error(`Unable to find webhook.site URL: ${webhookUrl}`);
    }
    const token = webhookUrl.split("/").pop();
    if (!token) {
      throw new Error(`Unable to parse webhook token from ${webhookUrl}`);
    }
    console.log(`Webhook URL: ${webhookUrl}`);

    await page.goto("/login");

    await page.getByLabel(/email/i).fill("asdf@asdf.com");
    await page.getByLabel(/password/i).fill("P@ssw0rd");
    await page.getByRole("button", { name: /sign in with email/i }).click();
    await page.waitForURL(/\/org(\/|$)/);

    await page.goto("/org");

    const orgName = `E2E Organization ${Date.now()}`;
    await page.getByLabel(/organization name/i).fill(orgName);
    await page.getByRole("button", { name: /create organization/i }).click();

    await page.waitForURL(/\/org\/.+\/dashboard/);
    const url = new URL(page.url());
    const orgId = url.pathname.split("/")[2];

    await page.goto(`/org/${orgId}/converters/new`);

    const now = Date.now();
    const converterName = `E2E Converter ${now}`;
    const inboundPath = `partner-${now}`;
    await page.getByLabel(/^name$/i).fill(converterName);
    await page.getByLabel(/inbound path/i).fill(inboundPath);
    await page.getByLabel(/forward url/i).fill(webhookUrl);
    await page.getByRole("button", { name: /create converter/i }).click();

    await page.waitForURL(/\/org\/.+\/converters\/.+/);

    const marker = `e2e-${Date.now()}`;
    await expect
      .poll(
        async () => {
          const inboundResponse = await request.post(
            `/api/inbound/${orgId}/${inboundPath}`,
            {
              data: { marker, name: "Ada" },
              headers: { "Content-Type": "application/json" },
            }
          );
          return inboundResponse.ok();
        },
        { timeout: 10_000, intervals: [500, 1000, 2000] }
      )
      .toBeTruthy();

    await expect
      .poll(
        async () => {
          const res = await request.get(
            `https://webhook.site/token/${token}/requests?sorting=newest`,
            { headers: { Accept: "application/json" } }
          );
          if (!res.ok()) return null;
          const payload = (await res.json()) as unknown;
          const entries = extractRequests(payload);
          const matchEntry = entries.find((entry) =>
            JSON.stringify(entry).includes(marker)
          );
          return matchEntry ?? null;
        },
        { timeout: 20_000, intervals: [1000, 2000, 3000, 5000] }
      )
      .not.toBeNull();
  });
});

import { expect, test } from "@playwright/test";

test.describe("converter samples", () => {
  test("persists example input/output on save", async ({ page }) => {
    await page.goto("/login");

    await page.getByLabel(/email/i).fill("asdf@asdf.com");
    await page.getByLabel(/password/i).fill("P@ssw0rd");
    await page.getByRole("button", { name: /sign in with email/i }).click();
    await page.waitForURL(/\/org(\/|$)/);

    await page.goto("/org");

    const orgName = `E2E Samples ${Date.now()}`;
    await page.getByLabel(/organization name/i).fill(orgName);
    await page.getByRole("button", { name: /create organization/i }).click();

    await page.waitForURL(/\/org\/.+\/dashboard/);
    const url = new URL(page.url());
    const orgId = url.pathname.split("/")[2];

    await page.goto(`/org/${orgId}/converters/new`);

    const converterName = `Samples Converter ${Date.now()}`;
    await page.getByLabel(/^name$/i).fill(converterName);
    await page.getByLabel(/inbound path/i).fill(`samples-${Date.now()}`);
    await page.getByLabel(/forward url/i).fill("https://example.com/webhook");
    await page.getByRole("button", { name: /create converter/i }).click();

    await page.waitForURL(/\/org\/.+\/converters\/.+/);
    await page.waitForLoadState("networkidle");

    await page.getByRole("tab", { name: /conversion/i }).click();
    await expect(page.getByRole("tab", { name: /conversion/i })).toHaveAttribute(
      "data-state",
      "active"
    );
    await expect(
      page.getByRole("textbox", { name: /example input/i })
    ).toBeVisible({ timeout: 15000 });

    const sampleInput = JSON.stringify({ name: "Ada" }, null, 2);
    const sampleOutput = JSON.stringify({ name: "Ada" }, null, 2);

    const inputField = page.getByRole("textbox", { name: /example input/i });
    const outputField = page.getByRole("textbox", { name: /expected output/i });

    await inputField.fill(sampleInput);
    await outputField.fill(sampleOutput);
    await expect(inputField).toHaveValue(sampleInput);
    await expect(outputField).toHaveValue(sampleOutput);

    await page.getByLabel(/output path/i).first().fill("name");
    await page.getByLabel(/source path/i).first().fill("name");

    await expect(
      page.getByText(/no differences detected/i)
    ).toBeVisible();

    await page.getByRole("button", { name: /save rules/i }).click();
    await expect(page.getByRole("status")).toContainText(/rules saved/i);

    await page.reload();
    await page.waitForLoadState("networkidle");
    await page.getByRole("tab", { name: /conversion/i }).click();
    await expect(page.getByRole("tab", { name: /conversion/i })).toHaveAttribute(
      "data-state",
      "active"
    );

    await expect(
      page.getByRole("textbox", { name: /example input/i })
    ).toHaveValue(sampleInput);
    await expect(
      page.getByRole("textbox", { name: /expected output/i })
    ).toHaveValue(sampleOutput);
    await expect(
      page.getByText(/no differences detected/i)
    ).toBeVisible();
  });
});

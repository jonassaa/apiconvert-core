import { expect, test, type Page } from "@playwright/test";

async function login(page: Page) {
  await page.goto("/login");
  await page.getByLabel(/email/i).fill("asdf@asdf.com");
  await page.getByLabel(/password/i).fill("P@ssw0rd");
  await page.getByRole("button", { name: /sign in with email/i }).click();
  await page.waitForURL(/\/org(\/|$)/);
}

test.describe("organization deletion", () => {
  test("deletes org after confirming DELETE", async ({ page }) => {
    await login(page);

    await page.goto("/org");
    const orgName = `Delete Organization ${Date.now()}`;
    await page.getByLabel(/organization name/i).fill(orgName);
    await page.getByRole("button", { name: /create organization/i }).click();
    await page.waitForURL(/\/org\/.+\/dashboard/);

    const orgId = new URL(page.url()).pathname.split("/")[2];

    await page.goto(`/org/${orgId}/settings`);
    await page.getByLabel(/type delete to confirm/i).fill("DELETE");
    await page.getByRole("button", { name: /delete organization/i }).click();
    await page.waitForURL(/\/org\?success=Organization(\+|%20)deleted/);

    await page.goto(`/org/${orgId}/dashboard`);
    await expect(
      page.getByText(
        /organization not found/i
      )
    ).toBeVisible();
  });
});

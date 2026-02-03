import { expect, test, type Page } from "@playwright/test";

async function login(page: Page) {
  await page.goto("/login");
  await page.getByLabel(/email/i).fill("asdf@asdf.com");
  await page.getByLabel(/password/i).fill("P@ssw0rd");
  await page.getByRole("button", { name: /sign in with email/i }).click();
  await page.waitForURL(/\/org(\/|$)/);
}

test.describe("organization overview", () => {
  test("favorites appear in the user menu", async ({ page }) => {
    await login(page);
    await page.goto("/org");

    const orgName = `Favorite Organization ${Date.now()}`;
    await page.getByLabel(/organization name/i).fill(orgName);
    await page.getByRole("button", { name: /create organization/i }).click();
    await page.waitForURL(/\/org\/.+\/dashboard/);

    await page.goto("/org");
    await page
      .getByRole("button", {
        name: new RegExp(`^Favorite ${orgName}$`, "i"),
      })
      .click();
    await expect(
      page.getByRole("button", {
        name: new RegExp(`^Unfavorite ${orgName}$`, "i"),
      })
    ).toBeVisible();

    await page.getByRole("button", { name: /asdf@asdf\.com/i }).click();
    await expect(
      page.getByRole("menuitemradio", { name: orgName })
    ).toBeVisible();
  });
});

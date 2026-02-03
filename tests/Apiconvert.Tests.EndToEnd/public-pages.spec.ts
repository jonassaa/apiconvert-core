import { expect, test } from "@playwright/test";

test.describe("public pages", () => {
  test("home renders core messaging and CTA", async ({ page }) => {
    await page.goto("/");

    await expect(page).toHaveTitle(/apiconvert/i);
    await expect(
      page.getByRole("heading", {
        name: /ship clean integrations without rewriting core apis/i,
      })
    ).toBeVisible();
    await expect(
      page.getByRole("link", { name: /getting started/i })
    ).toBeVisible();
    await expect(
      page.getByRole("link", { name: /see mapping examples/i })
    ).toBeVisible();
  });

  test("getting started page loads", async ({ page }) => {
    await page.goto("/getting-started");

    await expect(
      page.getByRole("heading", { name: /build your first converter/i })
    ).toBeVisible();
    await expect(
      page.getByRole("link", { name: /create a converter/i })
    ).toHaveAttribute("href", "/login");
  });

  test("login page exposes auth options on localhost", async ({ page }) => {
    await page.goto("/login");

    await expect(
      page.getByRole("button", { name: /continue with github/i })
    ).toBeVisible();
    await expect(page.getByLabel(/email/i)).toBeVisible();
    await expect(page.getByLabel(/password/i)).toBeVisible();
    await expect(
      page.getByRole("button", { name: /sign in with email/i })
    ).toBeVisible();
  });
});

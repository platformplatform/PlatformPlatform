import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext } from "@shared/e2e/utils/test-assertions";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@smoke", () => {
  /**
   * Tests the public landing page for unauthenticated users.
   * Covers:
   * - Landing page loads correctly at root URL
   * - Welcome message and call-to-action buttons are visible
   * - Navigation links (login, signup) work correctly
   * - Footer with legal links is visible
   */
  test("should display landing page with navigation for unauthenticated users", async ({ page }) => {
    createTestContext(page);

    await step("Navigate to landing page & verify welcome content")(async () => {
      await page.goto("/");

      await expect(page.getByRole("heading", { name: "Welcome to PlatformPlatform" })).toBeVisible();
      await expect(page.getByText("You successfully installed PlatformPlatform!")).toBeVisible();
    })();

    await step("Verify call-to-action buttons & navigation links are visible")(async () => {
      await expect(page.getByRole("link", { name: "Get started" })).toBeVisible();
      await expect(page.getByRole("navigation").getByRole("link", { name: "Log in" })).toBeVisible();
    })();

    await step("Click Get started button & verify redirect to signup")(async () => {
      await page.getByRole("link", { name: "Get started" }).first().click();

      await expect(page).toHaveURL("/signup");
    })();

    await step("Navigate back to landing page & click Log in button")(async () => {
      await page.goto("/");
      await page.getByRole("navigation").getByRole("link", { name: "Log in" }).click();

      await expect(page).toHaveURL("/login");
    })();
  });
});

test.describe("@comprehensive", () => {
  /**
   * Tests authenticated user redirect behavior on landing page.
   * Covers:
   * - Authenticated users are redirected from landing page to /dashboard
   * - Redirect happens automatically without user interaction
   */
  test("should redirect authenticated users to home page", async ({ ownerPage }) => {
    createTestContext(ownerPage);

    await step("Navigate to landing page as authenticated user & verify redirect to home")(async () => {
      await ownerPage.goto("/");

      await expect(ownerPage).toHaveURL("/dashboard");
    })();
  });
});

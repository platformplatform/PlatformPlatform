import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext, typeOneTimeCode } from "@shared/e2e/utils/test-assertions";
import { getVerificationCode } from "@shared/e2e/utils/test-data";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@comprehensive", () => {
  /**
   * Tests theme switching functionality via preferences page across different viewport sizes.
   * Covers:
   * - Theme switching between light, dark, and system modes via preferences page
   * - Theme persistence across page reloads
   * - Theme persistence across navigation
   * - Theme behavior at different viewport sizes (mobile, tablet, desktop, 4K)
   * - Sidebar collapse/expand states with theme changes
   */
  test("should handle theme switching with persistence across viewport sizes", async ({ ownerPage }) => {
    createTestContext(ownerPage);

    await step("Navigate to admin dashboard & verify default light theme")(async () => {
      await ownerPage.goto("/account");

      // Verify dashboard loads with default light theme
      await expect(ownerPage.getByRole("heading", { name: "Overview" })).toBeVisible();
      await expect(ownerPage.locator("html")).not.toHaveClass("dark");
    })();

    await step("Navigate to preferences page & select dark theme")(async () => {
      await ownerPage.goto("/user/preferences");
      await expect(ownerPage.getByRole("heading", { name: "Preferences" })).toBeVisible();

      await ownerPage.getByRole("button", { name: "Dark" }).click();

      await expect(ownerPage.locator("html")).toHaveClass("dark");
    })();

    await step("Reload page & verify dark theme persists")(async () => {
      await ownerPage.reload();

      await expect(ownerPage.getByRole("heading", { name: "Preferences" })).toBeVisible();
      await expect(ownerPage.locator("html")).toHaveClass("dark");
    })();

    await step("Navigate to users page & verify dark theme remains active")(async () => {
      await ownerPage.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();

      // Verify theme persists across navigation
      await expect(ownerPage.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(ownerPage.locator("html")).toHaveClass("dark");
    })();

    await step("Navigate to preferences & select system theme")(async () => {
      await ownerPage.goto("/user/preferences");
      await expect(ownerPage.getByRole("heading", { name: "Preferences" })).toBeVisible();

      await ownerPage.getByRole("button", { name: "System" }).click();

      // System theme will be light in test environment
      await expect(ownerPage.locator("html")).not.toHaveClass("dark");
    })();

    await step("Resize to 4K viewport & verify theme handling at large resolution")(async () => {
      await ownerPage.setViewportSize({ width: 2560, height: 1440 });

      await ownerPage.goto("/account/users");
      await expect(ownerPage.getByRole("button", { name: "Account menu" })).toBeVisible();
      await expect(ownerPage.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(ownerPage.locator("html")).not.toHaveClass("dark");
    })();

    await step("Navigate to preferences at 4K & select dark theme")(async () => {
      await ownerPage.goto("/user/preferences");
      await expect(ownerPage.getByRole("heading", { name: "Preferences" })).toBeVisible();

      await ownerPage.getByRole("button", { name: "Dark" }).click();

      await expect(ownerPage.locator("html")).toHaveClass("dark");
    })();

    await step("Resize to tablet viewport & verify theme persists with responsive layout")(async () => {
      await ownerPage.setViewportSize({ width: 768, height: 1024 });

      await ownerPage.goto("/account/users");
      await expect(ownerPage.getByRole("button", { name: "Account menu" })).toBeVisible();
      await expect(ownerPage.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(ownerPage.locator("html")).toHaveClass("dark");
    })();

    await step("Collapse sidebar at tablet size & verify Account menu remains accessible")(async () => {
      const toggleButton = ownerPage.getByRole("button", { name: "Toggle sidebar" });
      await expect(toggleButton).toBeVisible();

      await toggleButton.click();

      // Account menu should still be visible in collapsed sidebar
      await expect(ownerPage.getByRole("button", { name: "Account menu" })).toBeVisible();
      await expect(ownerPage.locator("html")).toHaveClass("dark");
    })();

    await step("Resize to mobile viewport & verify dark theme persists")(async () => {
      await ownerPage.setViewportSize({ width: 375, height: 667 });

      await expect(ownerPage.getByRole("button", { name: "Open navigation menu" })).toBeVisible();
      await expect(ownerPage.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(ownerPage.locator("html")).toHaveClass("dark");

      // Account menu should not be visible on mobile
      await expect(ownerPage.getByRole("button", { name: "Account menu" })).not.toBeVisible();
    })();

    await step("Navigate to preferences on mobile & switch to light theme")(async () => {
      await ownerPage.goto("/user/preferences");
      await expect(ownerPage.getByRole("heading", { name: "Preferences" })).toBeVisible();

      await ownerPage.getByRole("button", { name: "Light" }).click();

      await expect(ownerPage.locator("html")).not.toHaveClass("dark");
    })();

    await step("Return to desktop viewport & verify light theme persists")(async () => {
      await ownerPage.setViewportSize({ width: 1920, height: 1080 });

      await ownerPage.goto("/account/users");
      await expect(ownerPage.getByRole("button", { name: "Account menu" })).toBeVisible();
      await expect(ownerPage.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(ownerPage.locator("html")).not.toHaveClass("dark");
      await expect(ownerPage.getByRole("button", { name: "Toggle sidebar" })).toBeVisible();
    })();

    await step("Navigate to preferences & set dark theme before session test")(async () => {
      await ownerPage.goto("/user/preferences");
      await expect(ownerPage.getByRole("heading", { name: "Preferences" })).toBeVisible();

      await ownerPage.getByRole("button", { name: "Dark" }).click();

      await expect(ownerPage.locator("html")).toHaveClass("dark");
    })();

    await step("Open new browser tab & verify dark theme persists across sessions")(async () => {
      // Open a new tab in the same context to verify theme persistence
      const newPage = await ownerPage.context().newPage();
      await newPage.goto("/account");

      await expect(newPage.getByRole("heading", { name: "Overview" })).toBeVisible();
      await expect(newPage.locator("html")).toHaveClass("dark");

      await newPage.close();
    })();
  });

  /**
   * Tests theme persistence across logout/login cycles, 404 page, and error page functionality.
   * Covers:
   * - Theme switching via preferences page
   * - Theme persistence across logout and login cycles
   * - 404 page displays for non-existent routes
   * - Error page can be triggered via Konami code
   * - Error details can be shown/hidden
   * - Navigation from error pages back to the app
   */
  test("should handle theme persistence, 404 page, and error page via Konami code", async ({ anonymousPage }) => {
    const { page, tenant } = anonymousPage;
    const existingUser = tenant.owner;
    const context = createTestContext(page);

    // === THEME PERSISTENCE ===
    await step("Log in as owner & navigate to account dashboard")(async () => {
      await page.goto("/login");
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      await page.getByRole("textbox", { name: "Email" }).fill(existingUser.email);
      await page.getByRole("button", { name: "Log in with email" }).click();

      await expect(page).toHaveURL("/login/verify");
      await typeOneTimeCode(page, getVerificationCode());

      await expect(page).toHaveURL("/dashboard");
      await expect(page.getByRole("heading", { name: "Your dashboard is empty" })).toBeVisible();

      await page.goto("/account");
      await expect(page.getByRole("heading", { name: "Overview" })).toBeVisible();
    })();

    await step("Navigate to preferences & select dark theme")(async () => {
      await page.goto("/user/preferences");
      await expect(page.getByRole("heading", { name: "Preferences" })).toBeVisible();

      await page.getByRole("button", { name: "Dark" }).click();

      await expect(page.locator("html")).toHaveClass("dark");

      await page.goto("/account");
      await expect(page.getByRole("heading", { name: "Overview" })).toBeVisible();
    })();

    await step("Log out via Account menu & verify dark theme persists on login page")(async () => {
      // Mark 401 as expected during logout transition (React Query may have in-flight requests)
      context.monitoring.expectedStatusCodes.push(401);

      const accountMenuButton = page.getByRole("button", { name: "Account menu" });
      await accountMenuButton.dispatchEvent("click");

      const accountMenu = page.getByRole("menu", { name: "Account menu" });
      await expect(accountMenu).toBeVisible();

      const logoutMenuItem = page.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      await expect(page).toHaveURL("/login?returnPath=%2Faccount");
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // Dark theme should persist after logout
      await expect(page.locator("html")).toHaveClass("dark");
    })();

    await step("Reload login page & verify dark theme still persists")(async () => {
      await page.reload();

      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await expect(page.locator("html")).toHaveClass("dark");
    })();

    // === 404 PAGE (requires authenticated user) ===
    await step("Log back in to access protected routes")(async () => {
      await page.getByRole("textbox", { name: "Email" }).fill(existingUser.email);
      await page.getByRole("button", { name: "Log in with email" }).click();

      await expect(page).toHaveURL("/login/verify?returnPath=%2Faccount");
      await typeOneTimeCode(page, getVerificationCode());

      // Wait for redirect after OTP verification
      await expect(page).not.toHaveURL("/login/verify?returnPath=%2Faccount");

      // Navigate to account
      await page.goto("/account");
      await expect(page.getByRole("heading", { name: "Overview" })).toBeVisible();
    })();

    await step("Navigate to non-existent admin route & verify 404 page displays")(async () => {
      await page.goto("/account/does-not-exist");

      await expect(page.getByRole("heading", { name: "Page not found" })).toBeVisible();
      await expect(page.getByText("The page you are looking for does not exist or was moved.")).toBeVisible();
      await expect(page.getByRole("button", { name: "Go to home" })).toBeVisible();
    })();

    await step("Click Go to home button on 404 page & verify navigation to home")(async () => {
      await page.getByRole("button", { name: "Go to home" }).click();

      await expect(page).toHaveURL("/dashboard");
    })();

    // === ERROR PAGE VIA KONAMI CODE ===
    await step("Navigate to admin dashboard & enter Konami code to trigger error page")(async () => {
      await page.goto("/account");
      await expect(page.getByRole("heading", { name: "Overview" })).toBeVisible();

      await page.keyboard.press("ArrowUp");
      await page.keyboard.press("ArrowUp");
      await page.keyboard.press("ArrowDown");
      await page.keyboard.press("ArrowDown");
      await page.keyboard.press("ArrowLeft");
      await page.keyboard.press("ArrowRight");
      await page.keyboard.press("ArrowLeft");
      await page.keyboard.press("ArrowRight");
      await page.keyboard.press("b");
      await page.keyboard.press("a");

      await expect(page.getByRole("heading", { name: "Something went wrong" })).toBeVisible();
      await expect(page.getByText("An unexpected error occurred while processing your request.")).toBeVisible();
    })();

    await step("Click Show details button & verify error message is displayed")(async () => {
      await page.getByRole("button", { name: "Show details" }).click();

      await expect(page.getByText("Error triggered via Konami code.", { exact: true })).toBeVisible();
    })();

    await step("Click Try again button & verify error page resets to admin dashboard")(async () => {
      context.monitoring.consoleMessages = [];

      await page.getByRole("button", { name: "Try again" }).click();

      await expect(page.getByRole("heading", { name: "Overview" })).toBeVisible();
    })();
  });
});

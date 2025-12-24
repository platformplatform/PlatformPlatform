import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext, typeOneTimeCode } from "@shared/e2e/utils/test-assertions";
import { getVerificationCode } from "@shared/e2e/utils/test-data";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@comprehensive", () => {
  /**
   * Tests theme switching functionality across different viewport sizes and authentication states.
   * Covers:
   * - Theme switching between light, dark, and system modes
   * - Theme persistence across page reloads
   * - Theme persistence across logout/login cycles
   * - Theme behavior at different viewport sizes (mobile, tablet, desktop, 4K)
   * - Sidebar collapse/expand states with theme changes
   */
  test("should handle theme switching with persistence across viewport sizes", async ({ ownerPage }) => {
    createTestContext(ownerPage);

    await step("Navigate to admin dashboard & verify default light theme")(async () => {
      await ownerPage.goto("/admin");

      // Verify dashboard loads with default light theme
      await expect(ownerPage.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await expect(ownerPage.locator("html")).not.toHaveClass("dark");
    })();

    await step("Click theme button and select dark mode & verify dark theme applies")(async () => {
      const themeButton = ownerPage.getByRole("button", { name: "Change theme" });
      await themeButton.click();

      // Wait for menu to open and animation to complete
      const themeMenu = ownerPage.getByRole("menu");
      await expect(themeMenu).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const darkMenuItem = ownerPage.getByRole("menuitem", { name: "Dark" });
      await expect(darkMenuItem).toBeVisible();
      await darkMenuItem.dispatchEvent("click");

      await expect(themeMenu).not.toBeVisible();
      await expect(ownerPage.locator("html")).toHaveClass("dark");
    })();

    await step("Reload page & verify dark theme persists")(async () => {
      await ownerPage.reload();

      // Verify theme persists after reload
      await expect(ownerPage.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await expect(ownerPage.locator("html")).toHaveClass("dark");
    })();

    await step("Navigate to users page & verify dark theme remains active")(async () => {
      await ownerPage.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();

      // Verify theme persists across navigation
      await expect(ownerPage.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(ownerPage.locator("html")).toHaveClass("dark");
    })();

    await step("Click theme button and select system mode & verify theme follows system preference")(async () => {
      const themeButton = ownerPage.getByRole("button", { name: "Change theme" });
      await themeButton.click();

      // Wait for menu to open and animation to complete
      const systemMenu = ownerPage.getByRole("menu");
      await expect(systemMenu).toBeVisible();

      // Wait for menu item to be stable before clicking with JavaScript evaluate
      const systemMenuItem = ownerPage.getByRole("menuitem", { name: "System" });
      await expect(systemMenuItem).toBeVisible();
      await systemMenuItem.dispatchEvent("click");

      await expect(systemMenu).not.toBeVisible();
      // System theme will be light in test environment
      await expect(ownerPage.locator("html")).not.toHaveClass("dark");
    })();

    await step("Resize to 4K viewport & verify theme handling at large resolution")(async () => {
      await ownerPage.setViewportSize({ width: 2560, height: 1440 });

      // Verify 4K layout and theme state
      const themeButton = ownerPage.getByRole("button", { name: "Change theme" });
      await expect(themeButton).toBeVisible();
      await expect(ownerPage.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(ownerPage.locator("html")).not.toHaveClass("dark");
    })();

    await step("Click theme button and select dark at 4K & verify theme applies")(async () => {
      // Click trigger with JavaScript evaluate to ensure reliable opening on Firefox
      const themeButton = ownerPage.getByRole("button", { name: "Change theme" });
      await themeButton.dispatchEvent("click");

      const menu4k = ownerPage.getByRole("menu");
      await expect(menu4k).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const darkMenuItem = ownerPage.getByRole("menuitem", { name: "Dark" });
      await expect(darkMenuItem).toBeVisible();
      await darkMenuItem.dispatchEvent("click");

      await expect(ownerPage.locator("html")).toHaveClass("dark");
    })();

    await step("Resize to tablet viewport & verify theme persists with responsive layout")(async () => {
      await ownerPage.setViewportSize({ width: 768, height: 1024 });

      // Verify tablet layout and theme persistence
      await expect(ownerPage.getByRole("button", { name: "Change theme" })).toBeVisible();
      await expect(ownerPage.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(ownerPage.locator("html")).toHaveClass("dark");
    })();

    await step("Collapse sidebar at tablet size & verify theme button remains accessible")(async () => {
      const toggleButton = ownerPage.getByRole("button", { name: "Toggle sidebar" });
      await expect(toggleButton).toBeVisible();

      await toggleButton.click();

      // Theme button in top menu should still be visible
      await expect(ownerPage.getByRole("button", { name: "Change theme" })).toBeVisible();
      await expect(ownerPage.locator("html")).toHaveClass("dark");
    })();

    await step("Resize to mobile viewport & verify theme menu in mobile navigation")(async () => {
      await ownerPage.setViewportSize({ width: 375, height: 667 });

      // Verify mobile layout
      await expect(ownerPage.getByRole("button", { name: "Open navigation menu" })).toBeVisible();
      await expect(ownerPage.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(ownerPage.locator("html")).toHaveClass("dark");

      // Theme button should not be visible in top menu on mobile
      await expect(ownerPage.getByRole("button", { name: "Change theme" })).not.toBeVisible();

      // Open mobile menu and verify theme option
      await ownerPage.getByRole("button", { name: "Open navigation menu" }).click();
      await expect(ownerPage.getByRole("dialog", { name: "Mobile navigation menu" })).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Theme" })).toBeVisible();
    })();

    await step("Change theme via mobile menu & verify theme updates")(async () => {
      // Click trigger with JavaScript evaluate to ensure reliable opening on Firefox
      const themeButton = ownerPage.getByRole("button", { name: "Theme" });
      await themeButton.dispatchEvent("click");

      const themeSubmenu = ownerPage.getByRole("menu");
      await expect(themeSubmenu).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const lightMenuItem = ownerPage.getByRole("menuitem", { name: "Light" });
      await expect(lightMenuItem).toBeVisible();
      await lightMenuItem.dispatchEvent("click");

      // Mobile menu should close automatically
      await expect(ownerPage.getByRole("dialog", { name: "Mobile navigation menu" })).not.toBeVisible();

      // Verify light theme is applied
      await expect(ownerPage.locator("html")).not.toHaveClass("dark");
    })();

    await step("Return to desktop viewport & verify theme persists")(async () => {
      await ownerPage.setViewportSize({ width: 1920, height: 1080 });

      // Verify desktop layout restoration
      await expect(ownerPage.getByRole("button", { name: "Change theme" })).toBeVisible();
      await expect(ownerPage.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(ownerPage.locator("html")).not.toHaveClass("dark");
      await expect(ownerPage.getByRole("button", { name: "Toggle sidebar" })).toBeVisible();
    })();

    await step("Set dark theme before logout & verify theme applies")(async () => {
      // Click trigger with JavaScript evaluate to ensure reliable opening on Firefox
      const themeButton = ownerPage.getByRole("button", { name: "Change theme" });
      await themeButton.dispatchEvent("click");

      const menu = ownerPage.getByRole("menu");
      await expect(menu).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const darkMenuItem = ownerPage.getByRole("menuitem", { name: "Dark" });
      await expect(darkMenuItem).toBeVisible();
      await darkMenuItem.dispatchEvent("click");

      await expect(ownerPage.locator("html")).toHaveClass("dark");
    })();

    await step("Open new browser tab & verify dark theme persists across sessions")(async () => {
      // Open a new tab in the same context to verify theme persistence
      const newPage = await ownerPage.context().newPage();
      await newPage.goto("/admin");

      await expect(newPage.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await expect(newPage.locator("html")).toHaveClass("dark");

      await newPage.close();
    })();
  });

  /**
   * Tests theme persistence across logout/login cycles, 404 page, and error page functionality.
   * Covers:
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
    await step("Log in as owner & verify navigation to admin")(async () => {
      await page.goto("/login");
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      await page.getByRole("textbox", { name: "Email" }).fill(existingUser.email);
      await page.getByRole("button", { name: "Continue" }).click();

      await expect(page).toHaveURL("/login/verify");
      await typeOneTimeCode(page, getVerificationCode());

      await expect(page).toHaveURL("/admin");
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
    })();

    await step("Click theme button and select dark mode & verify it applies")(async () => {
      const themeButton = page.getByRole("button", { name: "Change theme" });
      await themeButton.click();

      // Wait for menu to open and animation to complete
      const menu = page.getByRole("menu");
      await expect(menu).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const darkMenuItem = page.getByRole("menuitem", { name: "Dark" });
      await expect(darkMenuItem).toBeVisible();
      await darkMenuItem.dispatchEvent("click");

      await expect(page.locator("html")).toHaveClass("dark");
    })();

    await step("Log out & verify dark theme persists on login page")(async () => {
      // Mark 401 as expected during logout transition (React Query may have in-flight requests)
      context.monitoring.expectedStatusCodes.push(401);

      // Click trigger with JavaScript evaluate to ensure reliable opening on Firefox
      const triggerButton = page.getByRole("button", { name: "User profile menu" });
      await triggerButton.dispatchEvent("click");

      const userMenu = page.getByRole("menu", { name: "User profile menu" });
      await expect(userMenu).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem = page.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      await expect(page).toHaveURL("/login?returnPath=%2Fadmin");
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // Dark theme should persist after logout
      await expect(page.locator("html")).toHaveClass("dark");
    })();

    await step("Log back in & verify theme remains dark after authentication")(async () => {
      await page.getByRole("textbox", { name: "Email" }).fill(existingUser.email);
      await page.getByRole("button", { name: "Continue" }).click();

      await expect(page).toHaveURL("/login/verify?returnPath=%2Fadmin");
      await typeOneTimeCode(page, getVerificationCode());

      await expect(page).toHaveURL("/admin");
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // Dark theme should persist after login
      await expect(page.locator("html")).toHaveClass("dark");
    })();

    // === 404 PAGE ===
    await step("Navigate to non-existent admin route & verify 404 page displays")(async () => {
      await page.goto("/admin/does-not-exist");

      await expect(page.getByRole("heading", { name: "Page not found" })).toBeVisible();
      await expect(page.getByText("The page you are looking for does not exist or was moved.")).toBeVisible();
      await expect(page.getByRole("button", { name: "Go to home" })).toBeVisible();
    })();

    await step("Click Go to home button on 404 page & verify navigation to home")(async () => {
      await page.getByRole("button", { name: "Go to home" }).click();

      await expect(page).toHaveURL("/");
    })();

    // === ERROR PAGE VIA KONAMI CODE ===
    await step("Navigate to admin dashboard & enter Konami code to trigger error page")(async () => {
      await page.goto("/admin");
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

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

      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
    })();
  });
});

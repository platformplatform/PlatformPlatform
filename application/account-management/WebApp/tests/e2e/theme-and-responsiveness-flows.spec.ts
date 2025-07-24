import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext } from "@shared/e2e/utils/test-assertions";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@comprehensive", () => {
  /**
   * Tests dark mode theme, viewport responsiveness, and theme persistence across authentication states.
   * Covers:
   * - Dark mode theme switching and persistence
   * - Viewport responsiveness from mobile to 4K desktop
   * - Navigation menu behavior at different breakpoints
   * - Dark theme persistence across logout/login
   */
  test("should handle dark theme across viewport sizes with persistence after logout", async ({ ownerPage }) => {
    createTestContext(ownerPage);

    await step("Navigate to admin dashboard & verify home page loads")(async () => {
      await ownerPage.goto("/admin");
      await expect(ownerPage.getByRole("heading", { name: "Welcome home" })).toBeVisible();
    })();

    await step("Change theme to dark mode & verify theme applies")(async () => {
      const themeButton = ownerPage.getByRole("button", { name: "Change theme" });
      await expect(themeButton).toHaveAttribute("aria-label", "Change theme");

      await themeButton.click();
      await ownerPage.getByRole("menuitem", { name: "Dark" }).click();

      await expect(ownerPage.locator("html")).toHaveClass("dark");
    })();

    await step("Verify desktop navigation menu & confirm all navigation links are visible")(async () => {
      await expect(ownerPage.getByLabel("Main navigation").getByRole("link", { name: "Home" })).toBeVisible();
      await expect(ownerPage.getByLabel("Main navigation").getByRole("link", { name: "Account" })).toBeVisible();
      await expect(ownerPage.getByLabel("Main navigation").getByRole("link", { name: "Users" })).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Toggle sidebar" })).toBeVisible();
    })();

    await step("Collapse desktop sidebar menu & verify content remains accessible")(async () => {
      await ownerPage.getByRole("button", { name: "Toggle sidebar" }).click();

      await expect(ownerPage.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Toggle sidebar" })).toBeVisible();
    })();

    await step("Resize to 4K desktop viewport & verify interface scales correctly")(async () => {
      await ownerPage.setViewportSize({ width: 2560, height: 1440 });

      await expect(ownerPage.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await expect(ownerPage.locator("html")).toHaveClass("dark");
      await expect(ownerPage.getByLabel("Main navigation").getByRole("link", { name: "Home" })).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Toggle sidebar" })).toBeVisible();
    })();

    await step("Resize to tablet viewport & verify responsive layout adapts")(async () => {
      await ownerPage.setViewportSize({ width: 768, height: 1024 });

      await expect(ownerPage.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await expect(ownerPage.locator("html")).toHaveClass("dark");
    })();

    await step("Verify tablet navigation layout & confirm sidebar remains visible")(async () => {
      await expect(ownerPage.getByLabel("Main navigation").getByRole("link", { name: "Home" })).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Toggle sidebar" })).toBeVisible();
    })();

    await step("Resize to mobile viewport & verify navigation collapses into hamburger menu")(async () => {
      await ownerPage.setViewportSize({ width: 375, height: 667 });

      await expect(ownerPage.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await expect(ownerPage.locator("html")).toHaveClass("dark");
      // Verify navigation links are hidden on mobile
      await expect(ownerPage.getByLabel("Main navigation").getByRole("link", { name: "Home" })).not.toBeVisible();
      await expect(ownerPage.getByLabel("Main navigation").getByRole("link", { name: "Users" })).not.toBeVisible();
      await expect(ownerPage.getByLabel("Main navigation").getByRole("link", { name: "Account" })).not.toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Open navigation menu" })).toBeVisible();
    })();

    await step("Verify theme menu is accessible via mobile menu & dark theme persists")(async () => {
      // Open mobile menu
      await ownerPage.getByRole("button", { name: "Open navigation menu" }).click();

      // Verify theme button is accessible in mobile menu
      await expect(ownerPage.getByRole("button", { name: "Theme" })).toBeVisible();

      // Close mobile menu
      await ownerPage.keyboard.press("Escape");

      // Verify we're still in dark mode
      await expect(ownerPage.locator("html")).toHaveClass("dark");
    })();

    await step("Resize to desktop viewport & verify navigation menu restores with dark theme")(async () => {
      await ownerPage.setViewportSize({ width: 1920, height: 1080 });

      await expect(ownerPage.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await expect(ownerPage.locator("html")).toHaveClass("dark");
      await expect(ownerPage.getByLabel("Main navigation").getByRole("link", { name: "Home" })).toBeVisible();
      await expect(ownerPage.getByLabel("Main navigation").getByRole("link", { name: "Account" })).toBeVisible();
      await expect(ownerPage.getByLabel("Main navigation").getByRole("link", { name: "Users" })).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Toggle sidebar" })).toBeVisible();
    })();

    await step("Verify dark theme remains active & prepare for logout test")(async () => {
      // Verify we're still on dark theme
      await expect(ownerPage.locator("html")).toHaveClass("dark");

      // Verify theme button is accessible
      const themeButton = ownerPage.getByRole("button", { name: "Change theme" });
      await expect(themeButton).toBeVisible();
    })();

    await step("Log out from admin dashboard & verify dark theme persists on login page")(async () => {
      await ownerPage.getByRole("button", { name: "User profile menu" }).click();
      await ownerPage.getByRole("menuitem", { name: "Log out" }).click();

      await expect(ownerPage).toHaveURL("/login?returnPath=%2Fadmin");
      // Dark theme should persist after logout (not system default)
      await expect(ownerPage.locator("html")).toHaveClass("dark");
    })();
  });
});

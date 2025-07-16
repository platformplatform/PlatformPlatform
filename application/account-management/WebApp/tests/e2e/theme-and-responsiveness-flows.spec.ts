import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext } from "@shared/e2e/utils/test-assertions";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@comprehensive", () => {
  test("should handle theme and responsiveness across viewport sizes and authentication", async ({ ownerPage }) => {
    createTestContext(ownerPage);

    const initialThemeClass = await ownerPage.locator("html").getAttribute("class");
    const initialIsLight = initialThemeClass?.includes("light");
    const firstTheme = initialIsLight ? "dark" : "light";
    const secondTheme = initialIsLight ? "light" : "dark";

    await step("Start on admin dashboard & verify welcome page loads")(async () => {
      await ownerPage.goto("/admin");

      await expect(ownerPage.getByRole("heading", { name: "Welcome home" })).toBeVisible();
    })();

    await step("Verify theme toggle button accessibility & click to change theme")(async () => {
      const themeButton = ownerPage.getByRole("button", { name: "Toggle theme" });
      await expect(themeButton).toHaveAttribute("aria-label", "Toggle theme");

      await themeButton.click();

      await expect(ownerPage.locator("html")).toHaveClass(firstTheme);
    })();

    await step("Verify desktop side menu navigation elements are visible & verify large screen behavior")(async () => {
      await expect(ownerPage.getByRole("button", { name: "Home" })).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Account" })).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Users" })).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Toggle collapsed menu" })).toBeVisible();
    })();

    await step("Collapse desktop sidebar & verify layout adapts")(async () => {
      await ownerPage.getByRole("button", { name: "Toggle collapsed menu" }).click();

      await expect(ownerPage.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Toggle collapsed menu" })).toBeVisible();
    })();

    await step("Switch to large desktop viewport & verify interface scales properly")(async () => {
      await ownerPage.setViewportSize({ width: 2560, height: 1440 });

      await expect(ownerPage.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await expect(ownerPage.locator("html")).toHaveClass(firstTheme);
      await expect(ownerPage.getByRole("button", { name: "Home" })).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Toggle collapsed menu" })).toBeVisible();
    })();

    await step("Switch to tablet viewport & verify interface adapts and theme persists")(async () => {
      await ownerPage.setViewportSize({ width: 768, height: 1024 });

      await expect(ownerPage.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await expect(ownerPage.locator("html")).toHaveClass(firstTheme);
    })();

    await step("Verify tablet breakpoint navigation behavior & verify elements remain visible")(async () => {
      await expect(ownerPage.getByRole("button", { name: "Home" })).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Toggle collapsed menu" })).toBeVisible();
    })();

    await step("Switch to mobile viewport & verify mobile interface and responsive behavior")(async () => {
      await ownerPage.setViewportSize({ width: 375, height: 667 });

      await expect(ownerPage.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await expect(ownerPage.locator("html")).toHaveClass(firstTheme);
      await expect(ownerPage.getByRole("button", { name: "Home" })).not.toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Users" })).not.toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Account" })).not.toBeVisible();
      const mobileMenuButton = ownerPage.getByRole("button", { name: "Toggle collapsed menu" });
      await expect(mobileMenuButton).toBeVisible();
    })();

    await step("Toggle mobile theme button & verify theme changes")(async () => {
      // Open mobile menu first
      await ownerPage.getByRole("button", { name: "Toggle collapsed menu" }).click();

      // Find and click theme toggle button inside mobile menu
      await ownerPage.getByRole("button", { name: "Theme" }).click();

      await expect(ownerPage.locator("html")).toHaveClass(secondTheme);
    })();

    await step("Return to desktop viewport & verify responsive transitions & side menu restoration")(async () => {
      await ownerPage.setViewportSize({ width: 1920, height: 1080 });

      await expect(ownerPage.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await expect(ownerPage.locator("html")).toHaveClass(secondTheme);
      await expect(ownerPage.getByRole("button", { name: "Home" })).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Account" })).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Users" })).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Toggle collapsed menu" })).toBeVisible();
    })();

    await step("Cycle through theme one more time & verify theme cycle works completely")(async () => {
      const finalThemeButton = ownerPage.getByRole("button", { name: "Toggle theme" });
      await finalThemeButton.click();

      await expect(ownerPage.locator("html")).toHaveClass(firstTheme);
    })();

    await step("Logout from account & verify theme persists across authentication")(async () => {
      await ownerPage.getByRole("button", { name: "User profile menu" }).click();
      await ownerPage.getByRole("menuitem", { name: "Log out" }).click();

      await expect(ownerPage).toHaveURL("/login?returnPath=%2Fadmin");
      await expect(ownerPage.locator("html")).toHaveClass(firstTheme);
    })();
  });
});

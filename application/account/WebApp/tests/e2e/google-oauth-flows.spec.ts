import { faker } from "@faker-js/faker";
import { expect, type Page } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext } from "@shared/e2e/utils/test-assertions";
import { step } from "@shared/e2e/utils/test-step-wrapper";

const MOCK_PROVIDER_COOKIE = "__Test_Use_Mock_Provider";

async function setMockProviderCookie(page: Page, emailPrefix: string): Promise<void> {
  await page.context().addCookies([
    {
      name: MOCK_PROVIDER_COOKIE,
      value: emailPrefix,
      url: "https://localhost:9000"
    }
  ]);
}

test.describe("@smoke", () => {
  /**
   * Tests Google OAuth authentication flows including:
   * 1. Signup with Google OAuth to create new tenant and user
   * 2. Logout and verify redirect to login page
   * 3. Login with Google OAuth and verify authentication
   * 4. Verify user profile shows correct email
   * 5. Logout via menu and verify redirect
   * 6. Attempt signup as existing user - verify redirect to login
   * 7. Complete login from redirect page
   *
   * Note: Uses mock OAuth provider with unique email per test run to avoid conflicts.
   */
  test("should handle Google OAuth signup, login, and existing user signup redirect flow", async ({ page }) => {
    const context = createTestContext(page);
    const emailPrefix = faker.string.alphanumeric(10);
    const mockUserEmail = `${emailPrefix}@test.com`;

    // === SIGNUP: Create mock user via Google OAuth ===

    await step("Navigate to signup page & sign up with Google OAuth")(async () => {
      await page.goto("/signup");

      await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();
      await setMockProviderCookie(page, emailPrefix);
      await page.getByRole("button", { name: "Sign up with Google" }).click();

      await expect(page).toHaveURL("/");
      await expect(page.getByRole("button", { name: "User profile menu" })).toBeVisible();
    })();

    await step("Open user profile menu & log out")(async () => {
      context.monitoring.expectedStatusCodes.push(401);
      await page.getByRole("button", { name: "User profile menu" }).dispatchEvent("click");
      const menu = page.getByRole("menu");
      await expect(menu).toBeVisible();
      const logoutMenuItem = page.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
    })();

    // === LOGIN: Verify Google OAuth login works ===

    await step("Click Google login button & verify successful authentication")(async () => {
      await setMockProviderCookie(page, emailPrefix);
      await page.getByRole("button", { name: "Continue with Google" }).click();

      await expect(page).toHaveURL("/");
      await expect(page.getByRole("button", { name: "User profile menu" })).toBeVisible();
    })();

    await step("Open user profile menu & verify mock user email displays")(async () => {
      await page.getByRole("button", { name: "User profile menu" }).dispatchEvent("click");
      const menu = page.getByRole("menu");
      await expect(menu).toBeVisible();

      await expect(page.getByText(mockUserEmail)).toBeVisible();
    })();

    await step("Log out via menu & verify redirect to login page")(async () => {
      context.monitoring.expectedStatusCodes.push(401);
      const menu = page.getByRole("menu");
      await expect(menu).toBeVisible();
      const logoutMenuItem = page.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
    })();

    // === EXISTING USER SIGNUP: Verify redirect to login ===

    await step("Navigate to signup page & attempt Google signup as existing user")(async () => {
      await page.goto("/signup");

      await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();
      await setMockProviderCookie(page, emailPrefix);
      await page.getByRole("button", { name: "Sign up with Google" }).click();

      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await expect(page).toHaveURL(`/login?email=${encodeURIComponent(mockUserEmail)}`);
    })();

    await step("Click Google login button from redirect & verify successful authentication")(async () => {
      await setMockProviderCookie(page, emailPrefix);
      await page.getByRole("button", { name: "Continue with Google" }).click();

      await expect(page).toHaveURL("/");
      await expect(page.getByRole("button", { name: "User profile menu" })).toBeVisible();
    })();
  });
});

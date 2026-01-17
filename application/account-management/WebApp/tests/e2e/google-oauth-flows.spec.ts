import { expect, type Page } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext } from "@shared/e2e/utils/test-assertions";
import { step } from "@shared/e2e/utils/test-step-wrapper";

const MOCK_USER_EMAIL = "mockuser@test.com";
const MOCK_PROVIDER_COOKIE = "__Test_Use_Mock_Provider";

async function setMockProviderCookie(page: Page): Promise<void> {
  await page.context().addCookies([
    {
      name: MOCK_PROVIDER_COOKIE,
      value: "true",
      url: "https://localhost:9000"
    }
  ]);
}

test.describe("@smoke", () => {
  /**
   * Tests Google OAuth authentication flows including:
   * 1. Login with Google OAuth and verify authentication
   * 2. Logout and verify redirect to login page
   * 3. Attempt signup with Google as existing user - verify redirect to login
   * 4. Complete login from redirect page
   *
   * Note: Uses mock OAuth provider with fixed email (mockuser@test.com).
   * Mock user must exist in database (created by seed data or previous test run).
   */
  test("should handle Google OAuth login and existing user signup redirect flow", async ({ page }) => {
    const context = createTestContext(page);

    await step("Navigate to login page & verify Google login button displays")(async () => {
      await page.goto("/login");

      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await expect(page.getByRole("button", { name: "Continue with Google" })).toBeVisible();
    })();

    await step("Click Google login button & verify successful authentication")(async () => {
      await setMockProviderCookie(page);
      await page.getByRole("button", { name: "Continue with Google" }).click();

      await expect(page).toHaveURL("/");
      await expect(page.getByRole("button", { name: "User profile menu" })).toBeVisible();
    })();

    await step("Open user profile menu & verify mock user email displays")(async () => {
      await page.getByRole("button", { name: "User profile menu" }).click();

      await expect(page.getByText(MOCK_USER_EMAIL)).toBeVisible();
    })();

    await step("Log out & verify redirect to login page")(async () => {
      context.monitoring.expectedStatusCodes.push(401);
      await page.getByRole("menuitem", { name: "Log out" }).click();

      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
    })();

    await step("Navigate to signup page & attempt Google signup as existing user")(async () => {
      await page.goto("/signup");

      await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();
      await setMockProviderCookie(page);
      await page.getByRole("button", { name: "Sign up with Google" }).click();

      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await expect(page).toHaveURL("/login?email=mockuser%40test.com");
    })();

    await step("Complete Google login from redirect & verify successful authentication")(async () => {
      await setMockProviderCookie(page);
      await page.getByRole("button", { name: "Continue with Google" }).click();

      await expect(page).toHaveURL("/");
      await expect(page.getByRole("button", { name: "User profile menu" })).toBeVisible();
    })();
  });
});

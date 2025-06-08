import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import {
  assertToastMessage,
  assertValidationError,
  createTestContext
} from "@shared/e2e/utils/test-assertions";
import { completeSignupFlow, getVerificationCode, testUser } from "@shared/e2e/utils/test-data";

test.describe("Login", () => {
  test.describe("@comprehensive", () => {
    test("should validate email format and show server validation error message", async ({ page }) => {
      const context = createTestContext(page);

      // Act & Assert: Navigate to login page & verify content is displayed
      await page.goto("/login");
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // Act & Assert: Submit invalid email format & verify validation error appears
      await page.getByRole("textbox", { name: "Email" }).fill("invalid-email");
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login");
      await assertValidationError(context, "Email must be in a valid format and no longer than 100 characters.");
    });

    test("should validate email length and show server validation error message", async ({ page }) => {
      const context = createTestContext(page);

      // Act & Assert: Navigate to login page & verify content is displayed
      await page.goto("/login");
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // Act & Assert: Submit email exceeding maximum length & verify validation error appears
      const longEmail = `${"a".repeat(90)}@example.com`; // 101 characters total
      await page.getByRole("textbox", { name: "Email" }).fill(longEmail);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login");
      await assertValidationError(context, "Email must be in a valid format and no longer than 100 characters.");
    });

    test("should handle login with non-existent email address", async ({ page }) => {
      const context = createTestContext(page);
      const nonExistentEmail = `nonexistent.user.${Date.now()}@platformplatform.net`;

      // Act & Assert: Navigate to login page & verify content is displayed
      await page.goto("/login");
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // Act & Assert: Submit non-existent email & verify it appears to proceed with verification page setup
      await page.getByRole("textbox", { name: "Email" }).fill(nonExistentEmail);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login/verify");
      await expect(page.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await expect(
        page.getByText(`Please check your email for a verification code sent to ${nonExistentEmail}`)
      ).toBeVisible();
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
      await expect(page.getByRole("button", { name: "Verify" })).toBeDisabled();

      // Act & Assert: Type verification code & verify button becomes enabled
      await page.keyboard.type(getVerificationCode());
      await expect(page.getByRole("button", { name: "Verify" })).toBeEnabled();

      // Act & Assert: Click verify button & verify authentication fails correctly
      await page.getByRole("button", { name: "Verify" }).click();
      await assertToastMessage(context, 400, "The code is wrong or no longer valid.");
      await expect(page).toHaveURL("/login/verify");
    });

    test("should handle verification code resend functionality during login", async ({ page }) => {
      const context = createTestContext(page);
      const user = testUser();

      // Act & Assert: Create a test user and logout & verify login flow can be tested
      await completeSignupFlow(page, expect, user, context, false);

      // Act & Assert: Navigate to login page and submit email & verify navigation to verification page
      await page.goto("/login");
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login/verify");

      // Act & Assert: Click resend button & verify no errors occur
      await page.getByRole("button", { name: "Didn't receive the code? Resend" }).click(); // Note: This should work similarly to signup resend functionality

      // Act & Assert: Verify resend functionality works & verify still on verification page
      await expect(page).toHaveURL("/login/verify");
      await expect(page.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
    });

    test("should work correctly across different viewport sizes", async ({ anonymousPage }) => {
      const { page, tenant } = anonymousPage;
      const user = tenant.owner;

      // Act & Assert: Test mobile viewport (375x667) and start login process & verify content displays
      await page.setViewportSize({ width: 375, height: 667 });
      await page.goto("/login");
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // Act & Assert: Complete login on mobile viewport & verify navigation to verification
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login/verify");

      // Act & Assert: Change to tablet viewport & verify content displays with proper initial state
      await page.setViewportSize({ width: 768, height: 1024 });
      await expect(page.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
      await expect(page.getByRole("button", { name: "Verify" })).toBeDisabled();

      // Act & Assert: Type verification code & verify button becomes enabled
      await page.keyboard.type(getVerificationCode());
      await expect(page.getByRole("button", { name: "Verify" })).toBeEnabled();

      // Act & Assert: Click verify button & verify navigation to admin
      await page.getByRole("button", { name: "Verify" }).click();
      await expect(page).toHaveURL("/admin");

      // Act & Assert: Test desktop viewport (1920x1080) & verify content displays correctly
      await page.setViewportSize({ width: 1920, height: 1080 });
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
    });

    test("should provide keyboard navigation support with proper focus management", async ({ anonymousPage }) => {
      const { page, tenant } = anonymousPage;
      const user = tenant.owner;

      // Act & Assert: Navigate to login page & verify proper focus is set
      await page.goto("/login");
      await expect(page.getByRole("textbox", { name: "Email" })).toBeFocused();

      // Act & Assert: Complete login form using keyboard navigation & verify verification page setup
      await page.keyboard.type(user.email);
      await page.keyboard.press("Enter"); // Submit form using Enter on input field
      await expect(page).toHaveURL("/login/verify");
      const codeInput = page.getByLabel("Login verification code").locator("input").first();
      await expect(codeInput).toHaveAttribute("type", "text");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
      await expect(page.getByRole("button", { name: "Verify" })).toBeDisabled();

      // Act & Assert: Type verification code & verify button becomes enabled
      await page.keyboard.type(getVerificationCode());
      await expect(page.getByRole("button", { name: "Verify" })).toBeEnabled();

      // Act & Assert: Click verify button & verify successful navigation to admin
      await page.getByRole("button", { name: "Verify" }).click();
      await expect(page).toHaveURL("/admin");
    });

    test("should handle rate limiting for failed login attempts", async ({ page }) => {
      const context = createTestContext(page);
      const user = testUser();

      // Act & Assert: Create a test user and logout & verify login flow can be tested
      await completeSignupFlow(page, expect, user, context, false);

      // Act & Assert: Navigate to login page and submit email & verify navigation to verification
      await page.goto("/login");
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login/verify");

      // Act & Assert: Make three failed attempts quickly & verify rate limiting triggers
      await page.keyboard.type("WRONG1");
      await page.getByRole("button", { name: "Verify" }).click();
      await assertToastMessage(context, 400, "The code is wrong or no longer valid.");
      await page.keyboard.press("Control+A");

      await page.keyboard.type("WRONG2");
      await page.getByRole("button", { name: "Verify" }).click();
      await assertToastMessage(context, 400, "The code is wrong or no longer valid.");
      await page.keyboard.press("Control+A");

      await page.keyboard.type("WRONG3");
      await page.getByRole("button", { name: "Verify" }).click();
      await assertToastMessage(context, 400, "The code is wrong or no longer valid.");
      await page.keyboard.press("Control+A");

      // Act & Assert: Submit fourth attempt & verify it's blocked with rate limiting message
      await page.keyboard.type("WRONG4");
      await page.getByRole("button", { name: "Verify" }).click();
      await expect(page.getByText("Too many attempts, please request a new code.").first()).toBeVisible();
      await assertToastMessage(context, "Forbidden", "Too many attempts, please request a new code.");
    });
  });

  test.describe("@slow", () => {
    test.describe.configure({ timeout: 360000 }); // 6 minutes timeout

    test("should handle verification code expiration during login (5-minute timeout)", async ({ page }) => {
      // NOTE: This test currently expects React errors in the console due to a bug in the application.
      // The /login/expired page tries to call getLoginState() which throws "No active login."
      const context = createTestContext(page);
      const user = testUser();

      // Act & Assert: Create a test user and logout & verify login flow can be tested
      await completeSignupFlow(page, expect, user, context, false);

      // Act & Assert: Navigate to login page and submit email & verify navigation to verification
      await page.goto("/login");
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login/verify");

      // Act & Assert: Verify countdown timer is visible & wait for expiration
      await expect(page.getByText("(5:00)")).toBeVisible();
      await page.waitForTimeout(300000); // 5 minutes

      // Act & Assert: Verify that session has expired & verify error message is shown
      await expect(page).toHaveURL("/login/expired");
      await expect(page.getByText("The verification code you are trying to use has expired").first()).toBeVisible();
    });

    test("should handle rate limiting for verification code resend requests", async ({ page }) => {
      const context = createTestContext(page);
      const user = testUser();

      // Act & Assert: Create a test user and logout & verify login flow can be tested
      await completeSignupFlow(page, expect, user, context, false);

      // Act & Assert: Navigate to login page and submit email & verify navigation to verification page
      await page.goto("/login");
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login/verify");

      // Act & Assert: Test first resend attempt & verify it succeeds
      await page.getByRole("button", { name: "Didn't receive the code? Resend" }).click(); // Note: This should work similarly to signup resend functionality

      // Act & Assert: Test second resend attempt & verify rate limiting occurs
      await page.getByRole("button", { name: "Didn't receive the code? Resend" }).click();
      await assertToastMessage(
        context,
        "Bad Request",
        "You must wait at least 30 seconds before requesting a new code."
      );

      // Act & Assert: Wait 30 seconds for rate limit to expire & verify timeout completes
      await page.waitForTimeout(30000); // 30 seconds

      // Act & Assert: Test third resend attempt after waiting & verify it succeeds
      await page.getByRole("button", { name: "Didn't receive the code? Resend" }).click(); // Note: After the 30-second wait, rate limiting should reset, so this should succeed
    });
  });
});

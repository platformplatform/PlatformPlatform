import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import {
  assertNetworkErrors,
  assertToastMessage,
  assertValidationError,
  blurActiveElement,
  createTestContext
} from "@shared/e2e/utils/test-assertions";
import { completeSignupFlow, getVerificationCode, testUser } from "@shared/e2e/utils/test-data";

test.describe("Login", () => {
  test.describe("@smoke", () => {
    test("should handle complete login flow with validation, security, authentication protection, and logout", async ({
      anonymousPage
    }) => {
      const { page, tenant } = anonymousPage;
      const existingUser = tenant.owner;
      const context = createTestContext(page);

      // === EMAIL VALIDATION EDGE CASES ===
      // Act & Assert: Test empty email validation & verify error message
      await page.goto("/login");
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login");
      await assertValidationError(context, "'Email' must not be empty");

      // Act & Assert: Test invalid email format & verify validation error
      await page.getByRole("textbox", { name: "Email" }).fill("invalid-email");
      await blurActiveElement(page);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login");
      await assertValidationError(context, "Email must be in a valid format and no longer than 100 characters.");

      // Act & Assert: Test email exceeding maximum length & verify validation error
      const longEmail = `${"a".repeat(90)}@example.com`; // 101 characters total
      await page.getByRole("textbox", { name: "Email" }).fill(longEmail);
      await blurActiveElement(page);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login");
      await assertValidationError(context, "Email must be in a valid format and no longer than 100 characters.");

      // Act & Assert: Test email with consecutive dots & verify validation error
      await page.getByRole("textbox", { name: "Email" }).fill("test..user@example.com");
      await blurActiveElement(page);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login");
      await assertValidationError(context, "Email must be in a valid format and no longer than 100 characters.");

      // === SUCCESSFUL LOGIN FLOW ===
      // Act & Assert: Enter valid code & verify navigation
      await page.getByRole("textbox", { name: "Email" }).fill(existingUser.email);
      await blurActiveElement(page);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login/verify");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
      await expect(page.getByRole("button", { name: "Verify" })).toBeDisabled();
      await expect(page.getByText("Can't find your code? Check your spam folder.").first()).toBeVisible();
      await expect(page.getByText("Request a new code")).not.toBeVisible();

      // Act & Assert: Test wrong verification code & verify error and focus reset
      await page.keyboard.type("WRONG1"); // The verification code auto submits the first time
      await assertToastMessage(context, "Bad Request", "The code is wrong or no longer valid.");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();

      // Act & Assert: Complete successful login & verify navigation
      await page.locator('input[autocomplete="one-time-code"]').first().focus();
      await page.keyboard.type(getVerificationCode()); // The verification does not auto submit the second time
      await page.getByRole("button", { name: "Verify" }).click();
      await expect(page).toHaveURL("/admin");
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // === AUTHENTICATION PROTECTION ===
      // Act & Assert: Logout from authenticated session & verify redirect to login
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page).toHaveURL("/login?returnPath=%2Fadmin");

      // Act & Assert: Access protected routes while unauthenticated & verify redirect to login
      await page.goto("/admin/users");
      await expect(page).toHaveURL("/login?returnPath=%2Fadmin%2Fusers");
      await assertNetworkErrors(context, [401]);
      await page.goto("/admin");
      await expect(page).toHaveURL("/login?returnPath=%2Fadmin");
      await assertNetworkErrors(context, [401]);

      // === SECURITY EDGE CASES ===
      // Act & Assert: Test malicious redirect prevention with external URL
      await page.goto("/login?returnPath=http://hacker.com");
      await expect(page).toHaveURL("/login");

      // Act & Assert: Test browser back navigation after authenticated session
      await page.getByRole("textbox", { name: "Email" }).fill(existingUser.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login/verify");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
      await page.keyboard.type(getVerificationCode()); // The verification code auto submits
      await expect(page).toHaveURL("/admin");
    });
  });

  test.describe("@comprehensive", () => {
    test("should enforce rate limiting for failed login attempts", async ({ page }) => {
      const context = createTestContext(page);
      const user = testUser();

      // Act & Assert: Create test user for rate limiting test & verify user created
      await completeSignupFlow(page, expect, user, context, false);

      // Act & Assert: Navigate to login and submit email & verify navigation
      await page.goto("/login");
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login/verify");

      // Act & Assert: Verify initial help text is shown
      await expect(page.getByText("Can't find your code? Check your spam folder.").first()).toBeVisible();
      await expect(page.getByText("Request a new code")).not.toBeVisible();

      // Act & Assert: First failed attempt & verify error and focus reset
      await page.keyboard.type("WRONG1"); // The verification code auto submits the first time
      await assertToastMessage(context, "Bad Request", "The code is wrong or no longer valid.");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();

      // Act & Assert: Second failed attempt & verify error and focus reset
      await page.keyboard.type("WRONG2");
      await page.getByRole("button", { name: "Verify" }).click();
      await assertToastMessage(context, "Bad Request", "The code is wrong or no longer valid.");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();

      // Act & Assert: Third failed attempt & verify error and focus reset
      await page.keyboard.type("WRONG3");
      await page.getByRole("button", { name: "Verify" }).click();
      await assertToastMessage(context, "Bad Request", "The code is wrong or no longer valid.");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();

      // Act & Assert: Fourth failed attempt triggers rate limiting & verify forbidden error
      await page.keyboard.type("WRONG4");
      await page.getByRole("button", { name: "Verify" }).click();
      await expect(page.getByText("Too many attempts, please request a new code.").first()).toBeVisible();
      await assertToastMessage(context, "Forbidden", "Too many attempts, please request a new code.");

      // Act & Assert: Verify rate limiting message is shown & verify UI state
      await expect(page.getByText("Too many attempts, please request a new code.").first()).toBeVisible();
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeDisabled();
      await expect(page.getByRole("button", { name: "Verify" })).toBeDisabled();
    });
  });

  test.describe("@slow", () => {
    const requestNewCodeTimeout = 30_000; // 30 seconds
    const codeValidationTimeout = 300_000; // 5 minutes
    const sessionTimeout = codeValidationTimeout + 60_000; // 6 minutes

    test("should handle resend code 30 seconds after login but then not after code has expired", async ({ page }) => {
      test.setTimeout(sessionTimeout);
      const context = createTestContext(page);
      const user = testUser();

      // Act & Assert: Create test user and navigate to verify & verify initial state
      await completeSignupFlow(page, expect, user, context, false);
      await page.goto("/login");
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login/verify");
      await expect(page.getByText("Can't find your code? Check your spam folder.").first()).toBeVisible();

      // Act & Assert: Wait 30 seconds before & verify Check your spam folder is not visible and that "Request a new code" IS available
      await page.waitForTimeout(requestNewCodeTimeout);
      await expect(
        page.getByRole("textbox", { name: "Can't find your code? Check your spam folder." })
      ).not.toBeVisible();
      await expect(page.getByText("Request a new code")).toBeVisible();

      // Act & Assert: Click Request a new code & verify success toast message and that "Request a new code" is NOT available
      await page.getByRole("button", { name: "Request a new code" }).click();
      await assertToastMessage(context, "Success", "A new verification code has been sent to your email.");
      await expect(page.getByRole("button", { name: "Request a new code" })).not.toBeVisible();
      await expect(page.getByText("Can't find your code? Check your spam folder.")).toBeVisible();

      // Act & Assert: Wait for expiration & verify inline expiration message and that "Request a new code" is NOT available
      await page.waitForTimeout(codeValidationTimeout);
      await expect(page).toHaveURL("/login/verify");
      await expect(page.getByText("Your verification code has expired")).toBeVisible();
      await expect(page.getByRole("button", { name: "Request a new code" })).not.toBeVisible();
      await expect(page.getByText("Can't find your code? Check your spam folder.")).toBeVisible();
    });

    test("should handle resend code 5 minutes after login when code has expired", async ({ page }) => {
      test.setTimeout(sessionTimeout);
      const context = createTestContext(page);
      const user = testUser();

      // Act & Assert: Create test user and start login & verify navigation
      await completeSignupFlow(page, expect, user, context, false);
      await page.goto("/login");
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login/verify");
      await expect(page.getByText("Can't find your code? Check your spam folder.")).toBeVisible();

      // Act & Assert: Wait 5 minutes for code expiration & verify inline expiration message and that "Request a new code" IS available
      await page.waitForTimeout(codeValidationTimeout);
      await expect(page).toHaveURL("/login/verify");
      await expect(page.getByText("Your verification code has expired")).toBeVisible();
      await expect(page.getByText("Can't find your code? Check your spam folder.")).not.toBeVisible();
      await expect(page.getByText("Request a new code")).toBeVisible();

      // Act & Assert: Request a new code & verify success toast message and that "Request a new code" is NOT available
      await page.getByRole("button", { name: "Request a new code" }).click();
      await assertToastMessage(context, "Success", "A new verification code has been sent to your email.");
      await expect(page.getByRole("button", { name: "Request a new code" })).not.toBeVisible();
      await expect(page.getByText("Can't find your code? Check your spam folder.")).toBeVisible();
    });
  });
});

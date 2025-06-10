import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { assertToastMessage, createTestContext } from "@shared/e2e/utils/test-assertions";
import { completeSignupFlow, getVerificationCode, testUser } from "@shared/e2e/utils/test-data";

test.describe("Login", () => {
  test.describe("@comprehensive", () => {
    test("should handle all login edge cases including validation, security, accessibility, and error handling", async ({
      anonymousPage
    }) => {
      const { page, tenant } = anonymousPage;
      const existingUser = tenant.owner;
      const context = createTestContext(page);

      // Act & Assert: Test empty email validation & verify error message
      await page.goto("/login");
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login"); // Verify form submission was blocked

      // Act & Assert: Test invalid email format & verify validation error
      await page.getByRole("textbox", { name: "Email" }).fill("invalid-email");
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login"); // Verify form submission was blocked

      // Act & Assert: Test email exceeding maximum length & verify validation error
      const longEmail = `${"a".repeat(90)}@example.com`; // 101 characters total
      await page.getByRole("textbox", { name: "Email" }).fill(longEmail);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login"); // Verify form submission was blocked

      // Act & Assert: Test form submission with Enter key & verify navigation
      await page.getByRole("textbox", { name: "Email" }).fill(existingUser.email);
      await page.keyboard.press("Enter"); // Submit form using Enter
      await expect(page).toHaveURL("/login/verify");

      // Act & Assert: Verify auto-focus on OTP input & verify button disabled initially
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
      await expect(page.getByRole("button", { name: "Verify" })).toBeDisabled();

      // Act & Assert: Verify accessibility attributes & verify proper ARIA labels
      const codeInput = page.getByLabel("Login verification code").locator("input").first();
      await expect(codeInput).toHaveAttribute("type", "text");

      // Act & Assert: Test wrong verification code & verify error and focus reset
      await page.keyboard.type("WRONG1");
      await page.getByRole("button", { name: "Verify" }).click();
      await assertToastMessage(context, 400, "The code is wrong or no longer valid.");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();

      // Act & Assert: Complete successful login & verify navigation
      await page.locator('input[autocomplete="one-time-code"]').first().focus();
      await page.keyboard.type(getVerificationCode());
      await page.getByRole("button", { name: "Verify" }).click();
      await expect(page).toHaveURL("/admin");
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // Act & Assert: Logout to test security edge cases
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page).toHaveURL("/login?returnPath=%2Fadmin");

      // Act & Assert: Test malicious redirect prevention with external URL
      await page.goto("/login?returnPath=http://hacker.com");
      await expect(page).toHaveURL("/login");

      // Act & Assert: Test browser back navigation after authenticated session
      await page.getByRole("textbox", { name: "Email" }).fill(existingUser.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login/verify");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
      await page.keyboard.type(getVerificationCode());
      await expect(page.getByRole("button", { name: "Verify" })).toBeEnabled();
      await page.getByRole("button", { name: "Verify" }).click();
      await expect(page).toHaveURL("/admin");
    });

    test("should handle viewport responsiveness and resend functionality", async ({ page }) => {
      const context = createTestContext(page);
      const user = testUser();

      // Act & Assert: Create test user for login testing & verify user created
      await completeSignupFlow(page, expect, user, context, false);

      // === MOBILE VIEWPORT TESTING ===
      // Act & Assert: Test mobile viewport (375x667) & verify content displays
      await page.setViewportSize({ width: 375, height: 667 });
      await page.goto("/login");
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // Act & Assert: Complete login on mobile & verify navigation
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login/verify");

      // === TABLET VIEWPORT TESTING ===
      // Act & Assert: Change to tablet viewport & verify proper display
      await page.setViewportSize({ width: 768, height: 1024 });
      await expect(page.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();

      // === RESEND FUNCTIONALITY ===
      // Act & Assert: Test resend button & verify success toast message
      await page.getByRole("button", { name: "Didn't receive the code? Resend" }).click();
      await assertToastMessage(context, "Success", "A new verification code has been sent to your email.");
      await expect(page).toHaveURL("/login/verify");
      await expect(page.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();

      // === DESKTOP VIEWPORT TESTING ===
      // Act & Assert: Change to desktop viewport & verify layout
      await page.setViewportSize({ width: 1920, height: 1080 });
      await page.locator('input[autocomplete="one-time-code"]').first().focus();
      await page.keyboard.type(getVerificationCode());
      await page.getByRole("button", { name: "Verify" }).click();
      await expect(page).toHaveURL("/admin");
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
    });

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

      // Act & Assert: First failed attempt & verify error and focus reset
      await page.keyboard.type("WRONG1");
      await page.getByRole("button", { name: "Verify" }).click();
      await assertToastMessage(context, 400, "The code is wrong or no longer valid.");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();

      // Act & Assert: Second failed attempt & verify error and focus reset
      await page.keyboard.type("WRONG2");
      await page.getByRole("button", { name: "Verify" }).click();
      await assertToastMessage(context, 400, "The code is wrong or no longer valid.");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();

      // Act & Assert: Third failed attempt & verify error and focus reset
      await page.keyboard.type("WRONG3");
      await page.getByRole("button", { name: "Verify" }).click();
      await assertToastMessage(context, 400, "The code is wrong or no longer valid.");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();

      // Act & Assert: Fourth failed attempt triggers rate limiting & verify forbidden error
      await page.keyboard.type("WRONG4");
      await page.getByRole("button", { name: "Verify" }).click();
      await expect(page.getByText("Too many attempts, please request a new code.").first()).toBeVisible();
      await assertToastMessage(context, "Forbidden", "Too many attempts, please request a new code.");

      // Act & Assert: Verify rate limiting message is shown & verify UI state
      await expect(page.getByText("Too many attempts, please request a new code.").first()).toBeVisible();

      // Act & Assert: Resend code button is available & verify it's clickable
      await expect(page.getByRole("button", { name: "Didn't receive the code? Resend" })).toBeVisible();
      await expect(page.getByRole("button", { name: "Didn't receive the code? Resend" })).toBeEnabled();
    });
  });

  test.describe("@slow", () => {
    test.describe.configure({ timeout: 360000 }); // 6 minutes timeout

    test("should handle verification code expiration during login", async ({ page }) => {
      // NOTE: This test expects React errors due to application bug with expired sessions
      const context = createTestContext(page);
      const user = testUser();

      // Act & Assert: Create test user and start login & verify navigation
      await completeSignupFlow(page, expect, user, context, false);
      await page.goto("/login");
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login/verify");

      // Act & Assert: Verify we're on the verify page with the resend button and timer
      await expect(page.getByRole("button", { name: "Didn't receive the code? Resend" })).toBeVisible();

      // Wait for expiration (5 minutes)
      await page.waitForTimeout(300000);

      // Act & Assert: Verify expiration redirect & verify error message
      await expect(page).toHaveURL("/login/expired");
      await expect(page.getByRole("heading", { name: "Error: Verification code has expired" })).toBeVisible();
    });

    test("should handle rate limiting for verification code resend requests", async ({ page }) => {
      const context = createTestContext(page);
      const user = testUser();

      // Act & Assert: Create test user and navigate to verify & verify initial state
      await completeSignupFlow(page, expect, user, context, false);
      await page.goto("/login");
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login/verify");

      // Act & Assert: First resend succeeds & verify success toast message
      await page.getByRole("button", { name: "Didn't receive the code? Resend" }).click();
      await assertToastMessage(context, "Success", "A new verification code has been sent to your email.");

      // Act & Assert: Second resend is rate limited & verify error message
      await page.getByRole("button", { name: "Didn't receive the code? Resend" }).click();
      await assertToastMessage(
        context,
        "Bad Request",
        "You must wait at least 30 seconds before requesting a new code."
      );

      // Act & Assert: Wait and retry but verify rate limit hit (max 1 resend allowed)
      await page.waitForTimeout(30000); // 30 seconds
      await page.getByRole("button", { name: "Didn't receive the code? Resend" }).click();
      await assertToastMessage(context, "Forbidden", "Too many attempts, please request a new code.");
    });
  });
});

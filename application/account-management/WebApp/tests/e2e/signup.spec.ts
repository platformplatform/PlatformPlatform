import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { assertToastMessage, createTestContext } from "@shared/e2e/utils/test-assertions";
import { getVerificationCode, testUser } from "@shared/e2e/utils/test-data";

test.describe("Signup", () => {
  test.describe("@comprehensive", () => {
    test("should handle all signup edge cases including validation, navigation, accessibility, and security", async ({
      browser
    }) => {
      // Create two browser contexts to simulate different sessions
      const context1 = await browser.newContext();
      const context2 = await browser.newContext();
      const page1 = await context1.newPage();
      const page2 = await context2.newPage();

      const testContext1 = createTestContext(page1);
      const testContext2 = createTestContext(page2);
      const user = testUser();

      // === EMAIL VALIDATION EDGE CASES ===
      // Act & Assert: Test empty email validation & verify form submission blocked
      await page1.goto("/signup");
      await page1.getByRole("button", { name: "Create your account" }).click();
      await expect(page1).toHaveURL("/signup"); // Verify form submission was blocked

      // Act & Assert: Test invalid email format & verify form submission blocked
      await page1.getByRole("textbox", { name: "Email" }).fill("invalid-email");
      await page1.getByRole("button", { name: "Create your account" }).click();
      await expect(page1).toHaveURL("/signup"); // Verify form submission was blocked

      // Act & Assert: Test email with consecutive dots & verify form submission blocked
      await page1.getByRole("textbox", { name: "Email" }).fill("test..user@example.com");
      await page1.getByRole("button", { name: "Create your account" }).click();
      await expect(page1).toHaveURL("/signup"); // Verify form submission was blocked

      // Act & Assert: Test email exceeding maximum length & verify form submission blocked
      const longEmail = `${"a".repeat(90)}@example.com`; // 101 characters total
      await page1.getByRole("textbox", { name: "Email" }).fill(longEmail);
      await page1.getByRole("button", { name: "Create your account" }).click();
      await expect(page1).toHaveURL("/signup"); // Verify form submission was blocked

      // === KEYBOARD NAVIGATION AND ACCESSIBILITY ===
      // Act & Assert: Test keyboard navigation with Tab & verify form submission works
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.keyboard.press("Tab"); // Move to region selector
      await page1.keyboard.press("Tab"); // Move to submit button
      await page1.keyboard.press("Enter"); // Submit form
      await expect(page1).toHaveURL("/signup/verify");

      // Act & Assert: Verify OTP input auto-focus & verify button is disabled initially
      await expect(page1.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
      await expect(page1.getByRole("button", { name: "Verify" })).toBeDisabled();

      // Act & Assert: Verify accessibility attributes & verify proper ARIA labels
      const codeInput = page1.getByLabel("Signup verification code").locator("input").first();
      await expect(codeInput).toHaveAttribute("type", "text");

      // === CONCURRENT SESSION HANDLING ===
      // Act & Assert: Attempt signup with same email in second browser & verify conflict error
      await page2.goto("/signup");
      await page2.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page2.getByRole("button", { name: "Create your account" }).click();
      await assertToastMessage(
        testContext2,
        409,
        "Email confirmation for this email has already been started. Please check your spam folder."
      );

      // === BROWSER NAVIGATION AND STATE MANAGEMENT ===
      // Act & Assert: Test browser back navigation & verify email field is cleared for security
      await page1.goBack();
      await expect(page1).toHaveURL("/signup");
      const emailValue = await page1.getByRole("textbox", { name: "Email" }).inputValue();
      expect(emailValue).toBe("");

      // Act & Assert: Navigate forward & verify session lost warning
      await page1.goForward();
      await expect(page1).toHaveURL("/signup");
      await assertToastMessage(testContext1, "No active signup session", "Please start the signup process again.");

      // Act & Assert: Direct navigation to verify page without session & verify redirect
      await page2.goto("/signup/verify");
      await expect(page2).toHaveURL("/signup");
      await assertToastMessage(testContext2, "No active signup session", "Please start the signup process again.");

      // Act & Assert: Restart signup process with new email & verify success
      const user2 = testUser();
      await page1.getByRole("textbox", { name: "Email" }).fill(user2.email);
      await page1.getByRole("button", { name: "Create your account" }).click();
      await expect(page1).toHaveURL("/signup/verify");

      // === VERIFICATION CODE VALIDATION ===
      // Act & Assert: Test wrong verification code & verify error and focus reset
      await page1.keyboard.type("WRONG1");
      await page1.getByRole("button", { name: "Verify" }).click();
      await assertToastMessage(testContext1, "Bad Request", "The code is wrong or no longer valid.");
      await expect(page1.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();

      // Act & Assert: Submit another wrong code & verify error handling
      await page1.keyboard.type("WRONG2");
      await page1.getByRole("button", { name: "Verify" }).click();
      await assertToastMessage(testContext1, "Bad Request", "The code is wrong or no longer valid.");
      await expect(page1.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();

      // Act & Assert: Submit correct code & verify navigation to profile
      await page1.keyboard.type(getVerificationCode());
      await page1.getByRole("button", { name: "Verify" }).click();
      await expect(page1).toHaveURL("/admin");

      // === PROFILE FORM VALIDATION (FROM SMOKE TEST) ===
      // Act & Assert: Submit empty profile form & verify validation blocked form submission
      await page1.getByRole("button", { name: "Save changes" }).click();
      await expect(page1.getByRole("dialog", { name: "User profile" })).toBeVisible(); // Verify form submission was blocked

      // Act & Assert: Test first name exceeding limit & verify validation blocked form submission
      const longName = "A".repeat(31);
      await page1.getByRole("textbox", { name: "First name" }).fill(longName);
      await page1.getByRole("button", { name: "Save changes" }).click();
      await expect(page1.getByRole("dialog", { name: "User profile" })).toBeVisible(); // Verify form submission was blocked

      // Act & Assert: Test title exceeding limit & verify validation blocked form submission
      const longTitle = "B".repeat(51);
      await page1.getByRole("textbox", { name: "Title" }).fill(longTitle);
      await page1.getByRole("button", { name: "Save changes" }).click();
      await expect(page1.getByRole("dialog", { name: "User profile" })).toBeVisible(); // Verify form submission was blocked

      // Act & Assert: Complete profile with valid data & verify success
      await page1.getByRole("textbox", { name: "First name" }).fill(user2.firstName);
      await page1.getByRole("textbox", { name: "Last name" }).fill(user2.lastName);
      await page1.getByRole("textbox", { name: "Title" }).fill("CEO");
      await page1.getByRole("button", { name: "Save changes" }).click();
      await assertToastMessage(testContext1, "Success", "Profile updated successfully");
      await expect(page1.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // === AUTHENTICATED NAVIGATION PROTECTION ===
      // Act & Assert: Try to access signup while authenticated & verify redirect to admin
      await page1.goto("/signup");
      await expect(page1).toHaveURL("/admin");

      // Act & Assert: Try to access signup/verify while authenticated & verify redirect to admin
      await page1.goto("/signup/verify");
      await expect(page1).toHaveURL("/admin");

      // === PROFILE UPDATE SYNCHRONIZATION ===
      // Act & Assert: Update profile in first session & verify update is saved
      await page1.getByRole("button", { name: "User profile menu" }).click();
      await page1.getByRole("menuitem", { name: "Edit profile" }).click();
      await page1.getByRole("textbox", { name: "Title" }).fill("Updated Title");
      await page1.getByRole("button", { name: "Save changes" }).click();
      await assertToastMessage(testContext1, "Success", "Profile updated successfully");

      // === DATA SECURITY AND PERSISTENCE ===
      // Act & Assert: Test form data security after navigation & verify fields are cleared
      await page2.goto("/signup");
      await page2.getByRole("textbox", { name: "Email" }).fill("test@example.com");
      await page2.goto("/");
      await page2.getByRole("button", { name: "Get started today" }).first().click();
      const clearedEmail = await page2.getByRole("textbox", { name: "Email" }).inputValue();
      expect(clearedEmail).toBe("");

      // Act & Assert: Cleanup browser contexts & verify no errors
      await context1.close();
      await context2.close();
    });

    test("should enforce rate limiting for verification attempts and handle edge cases", async ({ page }) => {
      const context = createTestContext(page);
      const user = testUser();

      // Act & Assert: Start signup process & verify navigation
      await page.goto("/signup");
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Create your account" }).click();
      await expect(page).toHaveURL("/signup/verify");

      // Act & Assert: First failed attempt & verify error and focus reset
      await page.keyboard.type("WRONG1");
      await page.getByRole("button", { name: "Verify" }).click();
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

      // Act & Assert: Resend code button is available & verify it's clickable
      await expect(page.getByRole("button", { name: "Didn't receive the code? Resend" })).toBeVisible();
      await expect(page.getByRole("button", { name: "Didn't receive the code? Resend" })).toBeEnabled();
    });
  });

  test.describe("@slow", () => {
    test.describe.configure({ timeout: 360000 }); // 6 minutes timeout

    test("should handle verification code expiration after five minutes", async ({ page }) => {
      // NOTE: This test expects React errors due to application bug with expired sessions
      const user = testUser();

      // Act & Assert: Start signup and wait for expiration & verify countdown timer
      await page.goto("/signup");
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Create your account" }).click();
      await expect(page).toHaveURL("/signup/verify");
      await expect(page.getByRole("button", { name: "Didn't receive the code? Resend" })).toBeVisible();

      // Act & Assert: Wait for expiration & verify redirect to expired page
      await page.waitForTimeout(300000); // 5 minutes
      await expect(page).toHaveURL("/signup/expired");
      await expect(page.getByText("No active signup session.").first()).toBeVisible();
    });

    test("should handle resend rate limiting with actual thirty second waits", async ({ page }) => {
      const context = createTestContext(page);
      const user = testUser();

      // Act & Assert: Start signup and navigate to verify & verify initial state
      await page.goto("/signup");
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Create your account" }).click();
      await expect(page).toHaveURL("/signup/verify");

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

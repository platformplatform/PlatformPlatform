import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { assertToastMessage, assertValidationError, createTestContext } from "@shared/e2e/utils/test-assertions";
import { getVerificationCode, testUser } from "@shared/e2e/utils/test-data";

test.describe("Signup", () => {
  test.describe("@smoke", () => {
    test("should handle complete signup flow with validation, profile setup, and account management", async ({
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

      // === HOMEPAGE NAVIGATION & SIGNUP INITIATION ===
      // Act & Assert: Navigate to homepage & verify marketing content is visible
      await page1.goto("/");
      await expect(page1).toHaveTitle("PlatformPlatform");

      // Act & Assert: Navigate to signup page & verify signup process starts
      await page1.getByRole("button", { name: "Get started today" }).first().click();
      await expect(page1).toHaveURL("/signup");
      await expect(page1.getByRole("heading", { name: "Create your account" })).toBeVisible();

      // === EMAIL VALIDATION EDGE CASES ===
      // Act & Assert: Test empty email validation & verify form submission blocked
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

      // === SUCCESSFUL SIGNUP FLOW ===
      // Act & Assert: Complete signup with valid email & verify navigation to verification page with initial state
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await expect(page1.getByText("Europe")).toBeVisible();
      await page1.getByRole("button", { name: "Create your account" }).click();
      await expect(page1).toHaveURL("/signup/verify");
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

      // === VERIFICATION CODE VALIDATION ===
      // Act & Assert: Test wrong verification code & verify error and focus reset
      await page1.keyboard.type("WRONG1");
      await page1.getByRole("button", { name: "Verify" }).click();
      await assertToastMessage(testContext1, "Bad Request", "The code is wrong or no longer valid.");
      await expect(page1.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();

      // Act & Assert: Type verification code & verify button becomes enabled
      await page1.keyboard.type(getVerificationCode());
      await expect(page1.getByRole("button", { name: "Verify" })).toBeEnabled();

      // Act & Assert: Click verify button & verify navigation to admin
      await page1.getByRole("button", { name: "Verify" }).click();
      await expect(page1).toHaveURL("/admin");

      // === PROFILE FORM VALIDATION & COMPLETION ===
      // Act & Assert: Submit profile form with empty fields & verify validation errors appear
      await expect(page1.getByRole("dialog", { name: "User profile" })).toBeVisible();
      await page1.getByRole("button", { name: "Save changes" }).click();
      await assertValidationError(testContext1, "'First Name' must not be empty.");
      await assertValidationError(testContext1, "'Last Name' must not be empty.");

      // Act & Assert: Fill form with one field too long and one missing & verify all validation errors appear
      const longName = "A".repeat(31);
      const longTitle = "B".repeat(51);
      await page1.getByRole("textbox", { name: "First name" }).fill(longName);
      await page1.getByRole("textbox", { name: "Last name" }).clear();
      await page1.getByRole("textbox", { name: "Title" }).fill(longTitle);
      await page1.getByRole("button", { name: "Save changes" }).click();
      await expect(page1.getByRole("dialog")).toBeVisible();
      await assertValidationError(testContext1, "First name must be no longer than 30 characters.");
      await assertValidationError(testContext1, "'Last Name' must not be empty.");
      await assertValidationError(testContext1, "Title must be no longer than 50 characters.");

      // Act & Assert: Complete profile setup with valid data & verify navigation to dashboard
      await page1.getByRole("textbox", { name: "First name" }).fill(user.firstName);
      await page1.getByRole("textbox", { name: "Last name" }).fill(user.lastName);
      await page1.getByRole("textbox", { name: "Title" }).fill("CEO & Founder");
      await page1.getByRole("button", { name: "Save changes" }).click();
      await assertToastMessage(testContext1, "Success", "Profile updated successfully");
      await expect(page1.getByRole("dialog")).not.toBeVisible();
      await expect(page1.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // === AVATAR & PROFILE FUNCTIONALITY ===
      // Act & Assert: Click avatar button & verify it shows initials and profile information
      const initials = user.firstName.charAt(0) + user.lastName.charAt(0);
      await expect(page1.getByRole("button", { name: "User profile menu" })).toContainText(initials);
      await page1.getByRole("button", { name: "User profile menu" }).click();
      await expect(page1.getByText(`${user.firstName} ${user.lastName}`)).toBeVisible();
      await expect(page1.getByText("CEO & Founder")).toBeVisible();
      await page1.getByRole("menuitem", { name: "Edit profile" }).click();
      await expect(page1.getByRole("textbox", { name: "Title" })).toHaveValue("CEO & Founder");
      await page1.getByRole("button", { name: "Cancel" }).click();
      await expect(page1.getByRole("dialog")).not.toBeVisible();

      // === AUTHENTICATED NAVIGATION PROTECTION ===
      // Act & Assert: Navigate to signup page while authenticated & verify redirect to admin
      await page1.goto("/signup");
      await expect(page1).toHaveURL("/admin");
      await expect(page1.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // === ACCOUNT MANAGEMENT ===
      // Act & Assert: Clear account name field & verify validation error appears
      await page1.getByRole("button", { name: "Account" }).first().click();
      await expect(page1.getByRole("heading", { name: "Account" })).toBeVisible();
      await page1.getByRole("textbox", { name: "Account name" }).clear();
      await page1.getByRole("button", { name: "Save changes" }).click();
      await assertValidationError(testContext1, "'Name' must not be empty.");

      // Act & Assert: Update account name & verify successful save
      const newAccountName = `Tech Corp ${Date.now()}`;
      await page1.getByRole("textbox", { name: "Account name" }).fill(newAccountName);
      await page1.getByRole("button", { name: "Save changes" }).focus(); // WebKit requires explicit focus before clicking
      await page1.getByRole("button", { name: "Save changes" }).click();
      await assertToastMessage(testContext1, "Success", "Account updated successfully");

      // Act & Assert: Update user profile title & verify successful profile update
      await page1.getByRole("button", { name: "User profile menu" }).click();
      await page1.getByRole("menuitem", { name: "Edit profile" }).click();
      await expect(page1.getByRole("dialog", { name: "User profile" })).toBeVisible();
      await page1.getByRole("textbox", { name: "Title" }).fill("Chief Executive Officer");
      await page1.getByRole("button", { name: "Save changes" }).click();
      await assertToastMessage(testContext1, "Success", "Profile updated successfully");
      await expect(page1.getByRole("dialog")).not.toBeVisible();

      // Act & Assert: Access protected account route & verify session maintains authentication
      await page1.getByRole("button", { name: "Account" }).first().click();
      await expect(page1.getByRole("textbox", { name: "Account name" })).toBeVisible();

      // === BROWSER NAVIGATION AND STATE MANAGEMENT ===
      // Act & Assert: Test data security after navigation & verify fields are cleared
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
  });

  test.describe("@comprehensive", () => {
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

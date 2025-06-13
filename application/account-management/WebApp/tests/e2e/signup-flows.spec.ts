import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import {
  assertToastMessage,
  assertValidationError,
  blurActiveElement,
  createTestContext
} from "@shared/e2e/utils/test-assertions";
import { getVerificationCode, testUser, uniqueEmail } from "@shared/e2e/utils/test-data";

test.describe("Signup", () => {
  test.describe("@smoke", () => {
    test("should handle complete signup flow with validation, profile setup, and account management", async ({
      browser
    }) => {
      // Create two browser contexts to simulate different sessions
      const context = await browser.newContext();
      const page = await context.newPage();
      const testContext = createTestContext(page);
      const user = testUser();

      // === SIGNUP INITIATION ===
      // Act & Assert: Navigate directly to signup page & verify signup process starts
      await page.goto("/signup");
      await expect(page).toHaveURL("/signup");
      await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();

      // === EMAIL VALIDATION EDGE CASES ===
      // Act & Assert: Test empty email validation & verify form submission blocked
      await page.getByRole("button", { name: "Create your account" }).click();
      await expect(page).toHaveURL("/signup");
      await expect(page.getByText("Email must be in a valid format and no longer than 100 characters.")).toBeVisible();

      // Act & Assert: Test invalid email format & verify form submission blocked
      await page.getByRole("textbox", { name: "Email" }).fill("invalid-email");
      await blurActiveElement(page);
      await page.getByRole("button", { name: "Create your account" }).click();
      await expect(page).toHaveURL("/signup");
      await expect(page.getByText("Email must be in a valid format and no longer than 100 characters.")).toBeVisible();

      // Act & Assert: Test email with consecutive dots & verify form submission blocked
      await page.getByRole("textbox", { name: "Email" }).fill("test..user@example.com");
      await blurActiveElement(page);
      await page.getByRole("button", { name: "Create your account" }).click();
      await expect(page).toHaveURL("/signup");
      await expect(page.getByText("Email must be in a valid format and no longer than 100 characters.")).toBeVisible();

      // Act & Assert: Test email exceeding maximum length & verify form submission blocked
      const longEmail = `${"a".repeat(90)}@example.com`; // 101 characters total
      await page.getByRole("textbox", { name: "Email" }).fill(longEmail);
      await blurActiveElement(page);
      await page.getByRole("button", { name: "Create your account" }).click();
      await expect(page).toHaveURL("/signup");
      await expect(page.getByText("Email must be in a valid format and no longer than 100 characters.")).toBeVisible();

      // === SUCCESSFUL SIGNUP FLOW ===
      // Act & Assert: Complete signup with valid email & verify navigation to verification page with initial state
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await blurActiveElement(page);
      await expect(page.getByText("Europe")).toBeVisible();
      await page.getByRole("button", { name: "Create your account" }).click();
      await expect(page).toHaveURL("/signup/verify");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
      await expect(page.getByRole("button", { name: "Verify" })).toBeDisabled();

      // === VERIFICATION CODE VALIDATION ===
      // Act & Assert: Test wrong verification code & verify error and focus reset
      await page.keyboard.type("WRONG1");
      await assertToastMessage(testContext, 400, "The code is wrong or no longer valid.");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();

      // Act & Assert: Type verification code & verify button becomes enabled
      await page.keyboard.type(getVerificationCode());
      await expect(page.getByRole("button", { name: "Verify" })).toBeEnabled();

      // Act & Assert: Click verify button & verify navigation to admin
      await page.getByRole("button", { name: "Verify" }).click();
      await expect(page).toHaveURL("/admin");

      // === PROFILE FORM VALIDATION & COMPLETION ===
      // Act & Assert: Submit profile form with empty fields & verify validation errors appear
      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertValidationError(testContext, "First name must be between 1 and 30 characters.");
      await assertValidationError(testContext, "Last name must be between 1 and 30 characters.");

      // Act & Assert: Fill form with one field too long and one missing & verify all validation errors appear
      const longName = "A".repeat(31);
      const longTitle = "B".repeat(51);
      await page.getByRole("textbox", { name: "First name" }).fill(longName);
      await page.getByRole("textbox", { name: "Last name" }).clear();
      await page.getByRole("textbox", { name: "Title" }).fill(longTitle);
      await page.getByRole("button", { name: "Save changes" }).click();
      await expect(page.getByRole("dialog")).toBeVisible();
      await assertValidationError(testContext, "First name must be between 1 and 30 characters.");
      await assertValidationError(testContext, "Last name must be between 1 and 30 characters.");
      await assertValidationError(testContext, "Title must be no longer than 50 characters.");

      // Act & Assert: Complete profile setup with valid data & verify navigation to dashboard
      await page.getByRole("textbox", { name: "First name" }).fill(user.firstName);
      await page.getByRole("textbox", { name: "Last name" }).fill(user.lastName);
      await page.getByRole("textbox", { name: "Title" }).fill("CEO & Founder");
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertToastMessage(testContext, 200, "Profile updated successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // === AVATAR & PROFILE FUNCTIONALITY ===
      // Act & Assert: Click avatar button & verify it shows initials and profile information
      const initials = user.firstName.charAt(0) + user.lastName.charAt(0);
      await expect(page.getByRole("button", { name: "User profile menu" })).toContainText(initials);
      await page.getByRole("button", { name: "User profile menu" }).click();
      await expect(page.getByText(`${user.firstName} ${user.lastName}`)).toBeVisible();
      await expect(page.getByText("CEO & Founder")).toBeVisible();
      await page.getByRole("menuitem", { name: "Edit profile" }).click();
      await expect(page.getByRole("textbox", { name: "Title" })).toHaveValue("CEO & Founder");
      await page.getByRole("button", { name: "Cancel" }).click();
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // === AUTHENTICATED NAVIGATION PROTECTION ===
      // Act & Assert: Navigate to signup page while authenticated & verify redirect to admin
      await page.goto("/signup");
      await expect(page).toHaveURL("/admin");
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // === ACCOUNT MANAGEMENT ===
      // Act & Assert: Clear account name field & verify validation error appears
      await page.getByRole("button", { name: "Account" }).first().click();
      await expect(page.getByRole("heading", { name: "Account" })).toBeVisible();
      await page.getByRole("textbox", { name: "Account name" }).clear();
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertValidationError(testContext, "Name must be between 1 and 30 characters.");

      // Act & Assert: Update account name & verify successful save
      const newAccountName = `Tech Corp ${Date.now()}`;
      await page.getByRole("textbox", { name: "Account name" }).fill(newAccountName);
      await page.getByRole("button", { name: "Save changes" }).focus(); // WebKit requires explicit focus before clicking
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertToastMessage(testContext, 200, "Account updated successfully");

      // Act & Assert: Update user profile title & verify successful profile update
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Edit profile" }).click();
      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();
      await page.getByRole("textbox", { name: "Title" }).fill("Chief Executive Officer");
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertToastMessage(testContext, 200, "Profile updated successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Act & Assert: Access protected account route & verify session maintains authentication
      await page.getByRole("button", { name: "Account" }).first().click();
      await expect(page.getByRole("textbox", { name: "Account name" })).toBeVisible();

      // Act & Assert: Cleanup browser context & verify no errors
      await context.close();
    });
  });

  test.describe("@comprehensive", () => {
    test("should enforce rate limiting for verification attempts and handle direct access", async ({ page }) => {
      const context = createTestContext(page);
      const user = testUser();

      // Act & Assert: Start signup process & verify navigation
      await page.goto("/signup");
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await blurActiveElement(page);
      await page.getByRole("button", { name: "Create your account" }).click();
      await expect(page).toHaveURL("/signup/verify");

      // Act & Assert: First failed attempt & verify error and focus reset
      await page.keyboard.type("WRONG1");
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
      await assertToastMessage(context, 403, "Too many attempts, please request a new code.");

      // Act & Assert: Verify rate limiting message is shown & verify UI state
      await expect(page.getByText("Too many attempts, please request a new code.").first()).toBeVisible();
    });

    test("should show detailed error message when too many signup attempts are made", async ({ page }) => {
      const context = createTestContext(page);
      const testEmail = uniqueEmail();

      // Act & Assert: First 3 signup attempts & verify navigation to verify page
      for (let attempt = 1; attempt <= 3; attempt++) {
        await page.goto("/signup");
        await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();

        await page.getByRole("textbox", { name: "Email" }).fill(testEmail);
        await page.getByRole("button", { name: "Create your account" }).click();
        await expect(page).toHaveURL("/signup/verify");
      }

      // Act & Assert: 4th attempt triggers rate limiting & verify error message
      await page.goto("/signup");
      await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();
      await page.getByRole("textbox", { name: "Email" }).fill(testEmail);
      await page.getByRole("button", { name: "Create your account" }).click();
      await expect(page).toHaveURL("/signup");
      await assertToastMessage(
        context,
        429,
        "Too many attempts to confirm this email address. Please try again later."
      );
    });
  });

  test.describe("@slow", () => {
    const requestNewCodeTimeout = 30_000; // 30 seconds
    const codeValidationTimeout = 60_000; // 5 minutes
    const sessionTimeout = codeValidationTimeout + 60_000; // 6 minutes

    test("should allow resend code 30 seconds after signup but then not after code has expired", async ({ page }) => {
      test.setTimeout(sessionTimeout);
      const context = createTestContext(page);
      const user = testUser();

      // Act & Assert: Start signup and navigate to verify & verify initial state
      await page.goto("/signup");
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await blurActiveElement(page);
      await page.getByRole("button", { name: "Create your account" }).click();
      await expect(page).toHaveURL("/signup/verify");
      await expect(page.getByText("Can't find your code? Check your spam folder.").first()).toBeVisible();

      // Act & Assert: Wait 30 seconds before & verify Check your spam folder is not visible and that "Request a new code" IS available
      await page.waitForTimeout(requestNewCodeTimeout);
      await expect(
        page.getByRole("textbox", { name: "Can't find your code? Check your spam folder." })
      ).not.toBeVisible();
      await expect(page.getByText("Request a new code")).toBeVisible();

      // Act & Assert: Click Request a new code & verify success toast message and that "Request a new code" is MOT available
      await page.getByRole("button", { name: "Request a new code" }).click();
      await assertToastMessage(context, "A new verification code has been sent to your email.");
      await expect(page.getByRole("button", { name: "Request a new code" })).not.toBeVisible();
      await expect(page.getByText("Can't find your code? Check your spam folder.")).toBeVisible();

      // Act & Assert: Wait for expiration & verify inline expiration message and that "Request a new code" is NOT available
      await page.waitForTimeout(codeValidationTimeout);
      await expect(page).toHaveURL("/signup/verify");
      await expect(page.getByText("Your verification code has expired")).toBeVisible();
      await expect(page.getByRole("button", { name: "Request a new code" })).not.toBeVisible();
      await expect(page.getByText("Can't find your code? Check your spam folder.")).toBeVisible();
    });

    test("should allow resend code 5 minutes after signup when code has expired", async ({ page }) => {
      test.setTimeout(sessionTimeout);
      const context = createTestContext(page);
      const user = testUser();

      // Act & Assert: Start signup and navigate to verify & verify initial state
      await page.goto("/signup");
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await blurActiveElement(page);
      await page.getByRole("button", { name: "Create your account" }).click();
      await expect(page).toHaveURL("/signup/verify");
      await expect(page.getByText("Can't find your code? Check your spam folder.")).toBeVisible();

      // Act & Assert: Wait 5 minutes for code expiration  & verify inline expiration message and that "Request a new code" IS  available
      await page.waitForTimeout(codeValidationTimeout);
      await expect(page).toHaveURL("/signup/verify");
      await expect(page.getByText("Your verification code has expired")).toBeVisible();
      await expect(page.getByText("Can't find your code? Check your spam folder.")).not.toBeVisible();
      await expect(page.getByText("Request a new code")).toBeVisible();

      // Act & Assert: Request a new code & verify success toast message and that "Request a new code" is NOT available
      await page.getByRole("button", { name: "Request a new code" }).click();
      await assertToastMessage(context, "A new verification code has been sent to your email.");
      await expect(page.getByRole("button", { name: "Request a new code" })).not.toBeVisible();
      await expect(page.getByText("Can't find your code? Check your spam folder.")).toBeVisible();
    });
  });
});

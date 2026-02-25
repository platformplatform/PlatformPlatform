import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import {
  blurActiveElement,
  createTestContext,
  expectToastMessage,
  expectValidationError,
  typeOneTimeCode
} from "@shared/e2e/utils/test-assertions";
import { getVerificationCode, testUser, uniqueEmail } from "@shared/e2e/utils/test-data";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@smoke", () => {
  test("should handle signup flow with validation, profile setup, and account", async ({ browser }) => {
    // Create two browser contexts to simulate different sessions
    const context = await browser.newContext();
    const page = await context.newPage();
    const testContext = createTestContext(page);
    const user = testUser();

    // === SIGNUP INITIATION ===
    await step("Navigate to signup page")(async () => {
      await page.goto("/signup");

      await expect(page).toHaveURL("/signup");
      await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();
    })();

    // === EMAIL VALIDATION EDGE CASES ===
    await step("Submit form with empty email & verify validation error")(async () => {
      await page.getByRole("button", { name: "Sign up with email" }).click();

      await expect(page).toHaveURL("/signup");
      await expect(page.getByText("Email must be in a valid format and no longer than 100 characters.")).toBeVisible();
    })();

    await step("Enter invalid email format & verify validation error")(async () => {
      await page.getByRole("textbox", { name: "Email" }).fill("invalid-email");
      await blurActiveElement(page);
      await page.getByRole("button", { name: "Sign up with email" }).click();

      await expect(page).toHaveURL("/signup");
      await expect(page.getByText("Email must be in a valid format and no longer than 100 characters.")).toBeVisible();
    })();

    await step("Enter email with consecutive dots & verify validation error")(async () => {
      await page.getByRole("textbox", { name: "Email" }).fill("test..user@example.com");
      await blurActiveElement(page);
      await page.getByRole("button", { name: "Sign up with email" }).click();

      await expect(page).toHaveURL("/signup");
      await expect(page.getByText("Email must be in a valid format and no longer than 100 characters.")).toBeVisible();
    })();

    await step("Enter email exceeding maximum length & verify validation error")(async () => {
      const longEmail = `${"a".repeat(90)}@example.com`; // 101 characters total
      await page.getByRole("textbox", { name: "Email" }).fill(longEmail);
      await blurActiveElement(page);
      await page.getByRole("button", { name: "Sign up with email" }).click();

      await expect(page).toHaveURL("/signup");
      await expect(page.getByText("Email must be in a valid format and no longer than 100 characters.")).toBeVisible();
    })();

    // === SUCCESSFUL SIGNUP FLOW ===
    await step("Complete signup with valid email & verify navigation to verification page")(async () => {
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await blurActiveElement(page);
      await expect(page.getByText("Europe")).toBeVisible();
      await page.getByRole("button", { name: "Sign up with email" }).click();

      // Verify verification page state
      await expect(page).toHaveURL("/signup/verify");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
      await expect(page.getByRole("button", { name: "Verify" })).toBeDisabled();
    })();

    // === VERIFICATION CODE VALIDATION ===
    await step("Enter wrong verification code & verify error and focus reset")(async () => {
      await typeOneTimeCode(page, "WRONG1");

      await expectToastMessage(testContext, 400, "The code is wrong or no longer valid.");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
    })();

    await step("Type verification code & verify submit button enables")(async () => {
      await typeOneTimeCode(page, getVerificationCode());

      await expect(page.getByRole("button", { name: "Verify" })).toBeEnabled();
    })();

    await step("Click verify button & complete welcome flow")(async () => {
      await page.getByRole("button", { name: "Verify" }).click(); // Auto-submit only happens when entering the first OTP

      await expect(page).toHaveURL(/\/welcome/);
      await expect(page.getByRole("heading", { name: "Let's set up your account" })).toBeVisible();
      await page.getByRole("textbox", { name: "Account name" }).fill("Test Organization");
      await page.getByRole("button", { name: "Continue" }).click();

      await expect(page.getByRole("heading", { name: "Let's set up your profile" })).toBeVisible();
      await page.getByRole("textbox", { name: "First name" }).fill(user.firstName);
      await page.getByRole("textbox", { name: "Last name" }).fill(user.lastName);
      await page.getByRole("button", { name: "Continue" }).click();

      await expect(page).toHaveURL("/dashboard");
      await expect(page.getByRole("heading", { name: "Your dashboard is empty" })).toBeVisible();
    })();

    await step("Navigate to profile page & verify profile form is visible")(async () => {
      await page.goto("/user/profile");

      await expect(page.getByRole("heading", { name: "Profile" })).toBeVisible();
    })();

    // === PROFILE FORM VALIDATION & COMPLETION ===
    await step("Clear profile fields & submit empty form & verify validation errors appear")(async () => {
      await page.getByRole("textbox", { name: "First name" }).clear();
      await page.getByRole("textbox", { name: "Last name" }).clear();
      await page.getByRole("button", { name: "Save changes" }).click();

      await expectValidationError(testContext, "First name must be between 1 and 30 characters.");
      await expectValidationError(testContext, "Last name must be between 1 and 30 characters.");
    })();

    await step("Fill form with one field too long and one missing & verify all validation errors appear")(async () => {
      // Create invalid form data
      const longName = "A".repeat(31);
      const longTitle = "B".repeat(51);
      await page.getByRole("textbox", { name: "First name" }).fill(longName);
      await page.getByRole("textbox", { name: "Last name" }).clear();
      await page.getByRole("textbox", { name: "Title" }).fill(longTitle);
      await page.getByRole("button", { name: "Save changes" }).click();

      // Verify all validation errors appear
      await expectValidationError(testContext, "First name must be between 1 and 30 characters.");
      await expectValidationError(testContext, "Last name must be between 1 and 30 characters.");
      await expectValidationError(testContext, "Title must be no longer than 50 characters.");
    })();

    await step("Complete profile setup with valid data & verify success toast")(async () => {
      // Complete profile setup
      await page.getByRole("textbox", { name: "First name" }).fill(user.firstName);
      await page.getByRole("textbox", { name: "Last name" }).fill(user.lastName);
      await page.getByRole("textbox", { name: "Title" }).fill("CEO & Founder");
      await page.getByRole("button", { name: "Save changes" }).click();

      // Verify success
      await expectToastMessage(testContext, 200, "Profile updated successfully");
    })();

    // === AVATAR & PROFILE FUNCTIONALITY ===
    await step("Open account menu & verify profile information")(async () => {
      // Navigate to dashboard first and ensure the account menu is visible
      await page.goto("/dashboard");
      await expect(page.getByRole("button", { name: "Account menu" })).toBeVisible();

      // Open account menu and verify user info - use evaluate for reliable opening on Firefox
      const avatarButton = page.getByRole("button", { name: "Account menu" });
      await avatarButton.dispatchEvent("click");
      const accountMenu = page.getByRole("menu");
      await expect(accountMenu).toBeVisible();
      await expect(accountMenu.getByText(`${user.firstName} ${user.lastName}`)).toBeVisible();
      await expect(accountMenu).toContainText(user.email);

      // Close menu by pressing Escape
      await page.keyboard.press("Escape");
      await expect(accountMenu).not.toBeVisible();
    })();

    await step("Verify profile page shows correct title")(async () => {
      await page.goto("/user/profile");
      await expect(page.getByRole("textbox", { name: "Title" })).toHaveValue("CEO & Founder");
    })();

    // === AUTHENTICATED NAVIGATION PROTECTION ===
    await step("Navigate to signup page while authenticated & verify redirect to dashboard")(async () => {
      await page.goto("/signup");

      await expect(page).toHaveURL("/dashboard");
      await expect(page.getByRole("heading", { name: "Your dashboard is empty" })).toBeVisible();
    })();

    // === ACCOUNT ===
    await step("Clear account name field & verify validation error appears")(async () => {
      await page.goto("/account/settings");
      await expect(page.getByRole("heading", { name: "Account settings" })).toBeVisible();
      await page.getByRole("textbox", { name: "Account name" }).clear();
      await page.getByRole("button", { name: "Save changes" }).click();

      await expectValidationError(testContext, "Name must be between 1 and 30 characters.");
    })();

    await step("Update account name & verify successful save")(async () => {
      const newAccountName = `Tech Corp ${Date.now()}`;
      await page.getByRole("textbox", { name: "Account name" }).fill(newAccountName);
      // WebKit requires explicit focus before clicking
      await page.getByRole("button", { name: "Save changes" }).focus();
      await page.getByRole("button", { name: "Save changes" }).click();

      await expectToastMessage(testContext, 200, "Account settings updated successfully");
    })();

    await step("Update user profile title & verify successful profile update")(async () => {
      // Navigate directly to profile page
      await page.goto("/user/profile");
      await expect(page.getByRole("heading", { name: "Profile" })).toBeVisible();

      await page.getByRole("textbox", { name: "Title" }).fill("Chief Executive Officer");
      await page.getByRole("button", { name: "Save changes" }).click();

      await expectToastMessage(testContext, 200, "Profile updated successfully");
    })();

    await step("Navigate to settings page")(async () => {
      await page.goto("/account/settings");

      await expect(page.getByRole("textbox", { name: "Account name" })).toBeVisible();
    })();

    // Cleanup explicitly created browser context
    await context.close();
  });
});

test.describe("@comprehensive", () => {
  // Rate limiting for verification attempts is comprehensively tested in login-flows.spec.ts

  test("should show detailed error message when too many signup attempts are made", async ({ page }) => {
    const context = createTestContext(page);
    const testEmail = uniqueEmail();

    await step("Make 3 signup attempts & verify each navigates to verify page")(async () => {
      // Make 3 signup attempts within rate limit threshold
      for (let attempt = 1; attempt <= 3; attempt++) {
        await page.goto("/signup");
        await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();

        await page.getByRole("textbox", { name: "Email" }).fill(testEmail);
        await page.getByRole("button", { name: "Sign up with email" }).click();

        await expect(page).toHaveURL("/signup/verify");
      }
    })();

    await step("Make 4th signup attempt & verify rate limiting triggers")(async () => {
      await page.goto("/signup");
      await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();

      await page.getByRole("textbox", { name: "Email" }).fill(testEmail);
      await page.getByRole("button", { name: "Sign up with email" }).click();

      // Verify rate limiting prevents navigation
      await expect(page).toHaveURL("/signup");
      await expectToastMessage(
        context,
        429,
        "Too many attempts to confirm this email address. Please try again later."
      );
    })();
  });
});

test.describe("@slow", () => {
  const requestNewCodeTimeout = 30_000; // 30 seconds
  const codeValidationTimeout = 300_000; // 5 minutes (300 seconds)
  const sessionTimeout = codeValidationTimeout + 60_000; // 6 minutes total

  test("should allow resend code 30 seconds after signup but then not after code has expired", async ({ page }) => {
    test.setTimeout(sessionTimeout);
    const context = createTestContext(page);
    const user = testUser();

    await step("Start signup and navigate to verify & verify page displays")(async () => {
      await page.goto("/signup");
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await blurActiveElement(page);
      await page.getByRole("button", { name: "Sign up with email" }).click();

      await expect(page).toHaveURL("/signup/verify");
      await expect(page.getByText("Can't find your code? Check your spam folder.").first()).toBeVisible();
    })();

    await step("Wait 30 seconds & verify request code button appears")(async () => {
      await page.waitForTimeout(requestNewCodeTimeout);

      // Verify UI changes after timeout
      await expect(
        page.getByRole("textbox", { name: "Can't find your code? Check your spam folder." })
      ).not.toBeVisible();
      await expect(page.getByText("Request a new code")).toBeVisible();
    })();

    await step("Click request new code & verify success message and button hides")(async () => {
      await page.getByRole("button", { name: "Request a new code" }).click();

      await expectToastMessage(context, "A new verification code has been sent to your email.");
      await expect(page.getByRole("button", { name: "Request a new code" })).not.toBeVisible();
      await expect(page.getByText("Can't find your code? Check your spam folder.")).toBeVisible();
    })();

    await step("Wait for code expiration & verify expiration message displays")(async () => {
      await page.waitForTimeout(codeValidationTimeout);

      // Verify expiration state
      await expect(page).toHaveURL("/signup/verify");
      await expect(page.getByText("Your verification code has expired")).toBeVisible();
      await expect(page.getByRole("button", { name: "Request a new code" })).not.toBeVisible();
      await expect(page.getByText("Can't find your code? Check your spam folder.")).toBeVisible();
    })();
  });

  // 5-minute request new code test is already covered in login-flows.spec.ts
});

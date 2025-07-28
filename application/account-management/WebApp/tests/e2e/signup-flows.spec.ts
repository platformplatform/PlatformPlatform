import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import {
  blurActiveElement,
  createTestContext,
  expectToastMessage,
  expectValidationError
} from "@shared/e2e/utils/test-assertions";
import { getVerificationCode, testUser, uniqueEmail } from "@shared/e2e/utils/test-data";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@smoke", () => {
  test("should handle signup flow with validation, profile setup, and account management", async ({ browser }) => {
    // Create two browser contexts to simulate different sessions
    const context = await browser.newContext();
    const page = await context.newPage();
    const testContext = createTestContext(page);
    const user = testUser();

    // === SIGNUP INITIATION ===
    await step("Navigate directly to signup page & verify signup process starts")(async () => {
      await page.goto("/signup");

      await expect(page).toHaveURL("/signup");
      await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();
    })();

    // === EMAIL VALIDATION EDGE CASES ===
    await step("Submit form with empty email & verify validation error")(async () => {
      await page.getByRole("button", { name: "Create your account" }).click();

      await expect(page).toHaveURL("/signup");
      await expect(page.getByText("Email must be in a valid format and no longer than 100 characters.")).toBeVisible();
    })();

    await step("Enter invalid email format & verify validation error")(async () => {
      await page.getByRole("textbox", { name: "Email" }).fill("invalid-email");
      await blurActiveElement(page);
      await page.getByRole("button", { name: "Create your account" }).click();

      await expect(page).toHaveURL("/signup");
      await expect(page.getByText("Email must be in a valid format and no longer than 100 characters.")).toBeVisible();
    })();

    await step("Enter email with consecutive dots & verify validation error")(async () => {
      await page.getByRole("textbox", { name: "Email" }).fill("test..user@example.com");
      await blurActiveElement(page);
      await page.getByRole("button", { name: "Create your account" }).click();

      await expect(page).toHaveURL("/signup");
      await expect(page.getByText("Email must be in a valid format and no longer than 100 characters.")).toBeVisible();
    })();

    await step("Enter email exceeding maximum length & verify validation error")(async () => {
      const longEmail = `${"a".repeat(90)}@example.com`; // 101 characters total
      await page.getByRole("textbox", { name: "Email" }).fill(longEmail);
      await blurActiveElement(page);
      await page.getByRole("button", { name: "Create your account" }).click();

      await expect(page).toHaveURL("/signup");
      await expect(page.getByText("Email must be in a valid format and no longer than 100 characters.")).toBeVisible();
    })();

    // === SUCCESSFUL SIGNUP FLOW ===
    await step("Complete signup with valid email & verify navigation to verification page with initial state")(
      async () => {
        await page.getByRole("textbox", { name: "Email" }).fill(user.email);
        await blurActiveElement(page);
        await expect(page.getByText("Europe")).toBeVisible();
        await page.getByRole("button", { name: "Create your account" }).click();

        await expect(page).toHaveURL("/signup/verify");
        await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
        await expect(page.getByRole("button", { name: "Verify" })).toBeDisabled();
      }
    )();

    // === VERIFICATION CODE VALIDATION ===
    await step("Enter wrong verification code & verify error and focus reset")(async () => {
      await page.keyboard.type("WRONG1");

      await expectToastMessage(testContext, 400, "The code is wrong or no longer valid.");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
    })();

    await step("Type verification code & verify submit button enables")(async () => {
      await page.keyboard.type(getVerificationCode());

      await expect(page.getByRole("button", { name: "Verify" })).toBeEnabled();
    })();

    await step("Click verify button & verify navigation to admin")(async () => {
      await page.getByRole("button", { name: "Verify" }).click();

      await expect(page).toHaveURL("/admin");
    })();

    // === PROFILE FORM VALIDATION & COMPLETION ===
    await step("Submit profile form with empty fields & verify validation errors appear")(async () => {
      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();
      await page.getByRole("button", { name: "Save changes" }).click();

      await expectValidationError(testContext, "First name must be between 1 and 30 characters.");
      await expectValidationError(testContext, "Last name must be between 1 and 30 characters.");
    })();

    await step("Fill form with one field too long and one missing & verify all validation errors appear")(async () => {
      const longName = "A".repeat(31);
      const longTitle = "B".repeat(51);
      await page.getByRole("textbox", { name: "First name" }).fill(longName);
      await page.getByRole("textbox", { name: "Last name" }).clear();
      await page.getByRole("textbox", { name: "Title" }).fill(longTitle);
      await page.getByRole("button", { name: "Save changes" }).click();

      await expect(page.getByRole("dialog")).toBeVisible();
      await expectValidationError(testContext, "First name must be between 1 and 30 characters.");
      await expectValidationError(testContext, "Last name must be between 1 and 30 characters.");
      await expectValidationError(testContext, "Title must be no longer than 50 characters.");
    })();

    await step("Complete profile setup with valid data & verify navigation to dashboard")(async () => {
      await page.getByRole("textbox", { name: "First name" }).fill(user.firstName);
      await page.getByRole("textbox", { name: "Last name" }).fill(user.lastName);
      await page.getByRole("textbox", { name: "Title" }).fill("CEO & Founder");
      await page.getByRole("button", { name: "Save changes" }).click();

      await expectToastMessage(testContext, 200, "Profile updated successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
    })();

    // === AVATAR & PROFILE FUNCTIONALITY ===
    await step("Click avatar button & verify it shows initials and profile information")(async () => {
      const initials = user.firstName.charAt(0) + user.lastName.charAt(0);
      await expect(page.getByRole("button", { name: "User profile menu" })).toContainText(initials);
      await page.getByRole("button", { name: "User profile menu" }).click();
      await expect(page.getByText(`${user.firstName} ${user.lastName}`)).toBeVisible();
      await expect(page.getByText("CEO & Founder")).toBeVisible();
      await page.getByRole("menuitem", { name: "Edit profile" }).click();
      await expect(page.getByRole("textbox", { name: "Title" })).toHaveValue("CEO & Founder");
      await page.getByRole("button", { name: "Cancel" }).click();

      await expect(page.getByRole("dialog")).not.toBeVisible();
    })();

    // === AUTHENTICATED NAVIGATION PROTECTION ===
    await step("Navigate to signup page while authenticated & verify redirect to admin")(async () => {
      await page.goto("/signup");

      await expect(page).toHaveURL("/admin");
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
    })();

    // === ACCOUNT MANAGEMENT ===
    await step("Clear account name field & verify validation error appears")(async () => {
      await page.getByLabel("Main navigation").getByRole("link", { name: "Account" }).click();
      await expect(page.getByRole("heading", { name: "Account settings" })).toBeVisible();
      await page.getByRole("textbox", { name: "Account name" }).clear();
      await page.getByRole("button", { name: "Save changes" }).click();

      await expectValidationError(testContext, "Name must be between 1 and 30 characters.");
    })();

    await step("Update account name & verify successful save")(async () => {
      const newAccountName = `Tech Corp ${Date.now()}`;
      await page.getByRole("textbox", { name: "Account name" }).fill(newAccountName);
      await page.getByRole("button", { name: "Save changes" }).focus(); // WebKit requires explicit focus before clicking
      await page.getByRole("button", { name: "Save changes" }).click();

      await expectToastMessage(testContext, 200, "Account updated successfully");
    })();

    await step("Update user profile title & verify successful profile update")(async () => {
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Edit profile" }).click();
      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();
      await page.getByRole("textbox", { name: "Title" }).fill("Chief Executive Officer");
      await page.getByRole("button", { name: "Save changes" }).click();

      await expectToastMessage(testContext, 200, "Profile updated successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
    })();

    await step("Access protected account route & verify session maintains authentication")(async () => {
      await page.getByLabel("Main navigation").getByRole("link", { name: "Account" }).click();

      await expect(page.getByRole("textbox", { name: "Account name" })).toBeVisible();
    })();

    await step("Cleanup browser context & verify no errors")(async () => {
      await context.close();
    })();
  });
});

test.describe("@comprehensive", () => {
  // Rate limiting for verification attempts is comprehensively tested in login-flows.spec.ts

  test("should show detailed error message when too many signup attempts are made", async ({ page }) => {
    const context = createTestContext(page);
    const testEmail = uniqueEmail();

    await step("First 3 signup attempts & verify navigation to verify page")(async () => {
      for (let attempt = 1; attempt <= 3; attempt++) {
        await page.goto("/signup");
        await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();

        await page.getByRole("textbox", { name: "Email" }).fill(testEmail);
        await page.getByRole("button", { name: "Create your account" }).click();
        await expect(page).toHaveURL("/signup/verify");
      }
    })();

    await step("4th attempt triggers rate limiting & verify error message")(async () => {
      await page.goto("/signup");
      await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();
      await page.getByRole("textbox", { name: "Email" }).fill(testEmail);
      await page.getByRole("button", { name: "Create your account" }).click();

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
  const codeValidationTimeout = 60_000; // 5 minutes
  const sessionTimeout = codeValidationTimeout + 60_000; // 6 minutes

  test("should allow resend code 30 seconds after signup but then not after code has expired", async ({ page }) => {
    test.setTimeout(sessionTimeout);
    const context = createTestContext(page);
    const user = testUser();

    await step("Start signup and navigate to verify & verify initial state")(async () => {
      await page.goto("/signup");
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await blurActiveElement(page);
      await page.getByRole("button", { name: "Create your account" }).click();

      await expect(page).toHaveURL("/signup/verify");
      await expect(page.getByText("Can't find your code? Check your spam folder.").first()).toBeVisible();
    })();

    await step(
      "Wait 30 seconds before & verify Check your spam folder is not visible and that 'Request a new code' IS available"
    )(async () => {
      await page.waitForTimeout(requestNewCodeTimeout);

      await expect(
        page.getByRole("textbox", { name: "Can't find your code? Check your spam folder." })
      ).not.toBeVisible();
      await expect(page.getByText("Request a new code")).toBeVisible();
    })();

    await step(
      "Click Request a new code & verify success toast message and that 'Request a new code' is NOT available"
    )(async () => {
      await page.getByRole("button", { name: "Request a new code" }).click();

      await expectToastMessage(context, "A new verification code has been sent to your email.");
      await expect(page.getByRole("button", { name: "Request a new code" })).not.toBeVisible();
      await expect(page.getByText("Can't find your code? Check your spam folder.")).toBeVisible();
    })();

    await step("Wait for expiration & verify inline expiration message and that 'Request a new code' is NOT available")(
      async () => {
        await page.waitForTimeout(codeValidationTimeout);

        await expect(page).toHaveURL("/signup/verify");
        await expect(page.getByText("Your verification code has expired")).toBeVisible();
        await expect(page.getByRole("button", { name: "Request a new code" })).not.toBeVisible();
        await expect(page.getByText("Can't find your code? Check your spam folder.")).toBeVisible();
      }
    )();
  });

  // 5-minute request new code test is already covered in login-flows.spec.ts
});

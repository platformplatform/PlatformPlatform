import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import {
  blurActiveElement,
  createTestContext,
  expectNetworkErrors,
  expectToastMessage
} from "@shared/e2e/utils/test-assertions";
import { completeSignupFlow, getVerificationCode, testUser } from "@shared/e2e/utils/test-data";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@smoke", () => {
  test("should handle login flow with validation, security, authentication protection, and logout", async ({
    anonymousPage
  }) => {
    const { page, tenant } = anonymousPage;
    const existingUser = tenant.owner;
    const context = createTestContext(page);

    // === EMAIL VALIDATION EDGE CASES ===
    // Email validation is comprehensively tested in signup-flows.spec.ts
    await step("Navigate to login page")(async () => {
      await page.goto("/login");
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
    })();

    // === SUCCESSFUL LOGIN FLOW ===
    await step("Enter valid code & verify navigation")(async () => {
      await page.getByRole("textbox", { name: "Email" }).fill(existingUser.email);
      await blurActiveElement(page);
      await page.getByRole("button", { name: "Continue" }).click();

      await expect(page).toHaveURL("/login/verify");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
      await expect(page.getByRole("button", { name: "Verify" })).toBeDisabled();
      await expect(page.getByText("Can't find your code? Check your spam folder.").first()).toBeVisible();
      await expect(page.getByText("Request a new code")).not.toBeVisible();
    })();

    await step("Enter wrong verification code & verify error and focus reset")(async () => {
      await page.keyboard.type("WRONG1"); // The verification code auto submits the first time

      await expectToastMessage(context, 400, "The code is wrong or no longer valid.");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
    })();

    await step("Complete successful login & verify navigation")(async () => {
      await page.locator('input[autocomplete="one-time-code"]').first().focus();
      await page.keyboard.type(getVerificationCode()); // The verification does not auto submit the second time
      await page.getByRole("button", { name: "Verify" }).click();

      await expect(page).toHaveURL("/admin");
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
    })();

    // === AUTHENTICATION PROTECTION ===
    await step("Logout from authenticated session & verify redirect to login")(async () => {
      // Mark 401 as expected during logout transition (React Query may have in-flight requests)
      context.monitoring.expectedStatusCodes.push(401);

      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Log out" }).click();

      await expect(page).toHaveURL("/login?returnPath=%2Fadmin");
    })();

    await step("Access protected routes while unauthenticated & verify redirect to login")(async () => {
      await page.goto("/admin/users");
      await expect(page).toHaveURL("/login?returnPath=%2Fadmin%2Fusers");

      await expectNetworkErrors(context, [401]);

      await page.goto("/admin");

      await expect(page).toHaveURL("/login?returnPath=%2Fadmin");
      await expectNetworkErrors(context, [401]);
    })();

    // === SECURITY EDGE CASES ===
    await step("Navigate with malicious redirect URL & verify prevention")(async () => {
      await page.goto("/login?returnPath=http://hacker.com");

      await expect(page).toHaveURL("/login");
    })();

    await step("Complete login after back navigation & verify authentication works")(async () => {
      await page.getByRole("textbox", { name: "Email" }).fill(existingUser.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login/verify");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
      await page.keyboard.type(getVerificationCode()); // The verification code auto submits

      await expect(page).toHaveURL("/admin");
    })();
  });
});

test.describe("@comprehensive", () => {
  test("should enforce rate limiting for failed login attempts", async ({ page }) => {
    const context = createTestContext(page);
    const user = testUser();

    await step("Create test user for rate limiting test & verify user created")(async () => {
      await completeSignupFlow(page, expect, user, context, false);
    })();

    await step("Navigate to login and submit email & verify navigation")(async () => {
      await page.goto("/login");
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Continue" }).click();

      await expect(page).toHaveURL("/login/verify");
    })();

    await step("Check verification page state & verify initial help text displays")(async () => {
      await expect(page.getByText("Can't find your code? Check your spam folder.").first()).toBeVisible();
      await expect(page.getByText("Request a new code")).not.toBeVisible();
    })();

    await step("Enter first wrong code & verify error and focus reset")(async () => {
      await page.keyboard.type("WRONG1"); // The verification code auto submits the first time

      await expectToastMessage(context, 400, "The code is wrong or no longer valid.");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
    })();

    await step("Enter second wrong code & verify error and focus reset")(async () => {
      await page.keyboard.type("WRONG2");
      await page.getByRole("button", { name: "Verify" }).click();

      await expectToastMessage(context, 400, "The code is wrong or no longer valid.");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
    })();

    await step("Enter third wrong code & verify error and focus reset")(async () => {
      await page.keyboard.type("WRONG3");
      await page.getByRole("button", { name: "Verify" }).click();

      await expectToastMessage(context, 400, "The code is wrong or no longer valid.");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
    })();

    await step("Enter fourth wrong code & verify rate limiting triggers")(async () => {
      await page.keyboard.type("WRONG4");
      await page.getByRole("button", { name: "Verify" }).click();

      await expect(page.getByText("Too many attempts, please request a new code.").first()).toBeVisible();
      await expectToastMessage(context, 403, "Too many attempts, please request a new code.");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeDisabled();
      await expect(page.getByRole("button", { name: "Verify" })).toBeDisabled();
    })();
  });
});

test.describe("@comprehensive", () => {
  test("should show detailed error message when too many login attempts are made", async ({ page }) => {
    const context = createTestContext(page);
    const user = testUser();

    await step("Create test user for rate limiting test & verify user created")(async () => {
      await completeSignupFlow(page, expect, user, context, false);
    })();

    await step("First 3 login attempts & verify navigation to verify page")(async () => {
      for (let attempt = 1; attempt <= 3; attempt++) {
        await page.goto("/login");
        await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

        await page.getByRole("textbox", { name: "Email" }).fill(user.email);
        await page.getByRole("button", { name: "Continue" }).click();
        await expect(page).toHaveURL("/login/verify");
      }
    })();

    await step("4th attempt triggers rate limiting & verify error message")(async () => {
      await page.goto("/login");
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Continue" }).click();

      await expect(page).toHaveURL("/login");
      await expectToastMessage(
        context,
        429,
        "Too many attempts to confirm this email address. Please try again later."
      );
    })();
  });
});

test.describe("@slow", () => {
  const requestNewCodeTimeout = 30000; // 30 seconds
  const codeValidationTimeout = 60000; // 5 minutes
  const sessionTimeout = codeValidationTimeout + 60000; // 6 minutes

  test("should allow resend code 30 seconds after login but then not after code has expired", async ({ page }) => {
    test.setTimeout(sessionTimeout);
    const context = createTestContext(page);
    const user = testUser();

    await step("Create test user and navigate to verify & verify initial state")(async () => {
      await completeSignupFlow(page, expect, user, context, false);
      await page.goto("/login");
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Continue" }).click();

      await expect(page).toHaveURL("/login/verify");
      await expect(page.getByText("Can't find your code? Check your spam folder.").first()).toBeVisible();
    })();

    await step("Wait 30 seconds & verify request code button appears")(async () => {
      await page.waitForTimeout(requestNewCodeTimeout);

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

      await expect(page).toHaveURL("/login/verify");
      await expect(page.getByText("Your verification code has expired")).toBeVisible();
      await expect(page.getByRole("button", { name: "Request a new code" })).not.toBeVisible();
      await expect(page.getByText("Can't find your code? Check your spam folder.")).toBeVisible();
    })();
  });

  // 5-minute request new code test is kept in the 30-second test above which also tests the 5-minute scenario
});

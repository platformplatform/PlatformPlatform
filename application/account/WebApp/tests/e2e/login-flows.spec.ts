import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import {
  blurActiveElement,
  createTestContext,
  expectToastMessage,
  typeOneTimeCode
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
    await step("Navigate to login page & verify heading displays")(async () => {
      await page.goto("/login");

      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
    })();

    // === SUCCESSFUL LOGIN FLOW ===
    await step("Enter valid email & verify navigation to verification page")(async () => {
      await page.getByRole("textbox", { name: "Email" }).fill(existingUser.email);
      await blurActiveElement(page);
      await page.getByRole("button", { name: "Log in with email" }).click();

      // Verify verification page state
      await expect(page).toHaveURL("/login/verify");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
      await expect(page.getByRole("button", { name: "Verify" })).toBeDisabled();

      // Verify help text is visible but resend button is not yet available
      await expect(page.getByText("Can't find your code? Check your spam folder.").first()).toBeVisible();
      await expect(page.getByText("Request a new code")).not.toBeVisible();
    })();

    await step("Enter wrong verification code & verify error and focus reset")(async () => {
      await typeOneTimeCode(page, "WRONG1");

      await expectToastMessage(context, 400, "The code is wrong or no longer valid.");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
    })();

    await step("Complete successful login & verify navigation to dashboard")(async () => {
      await typeOneTimeCode(page, getVerificationCode());
      await page.getByRole("button", { name: "Verify" }).click(); // Auto-submit only happens when entering the first OTP

      // Verify successful login
      await expect(page).toHaveURL("/dashboard");
      await expect(page.getByRole("heading", { name: "Your dashboard is empty" })).toBeVisible();
    })();

    // === AUTHENTICATION PROTECTION ===
    await step("Click logout from account menu & verify redirect to login")(async () => {
      // Mark 401 as expected during logout transition (React Query may have in-flight requests)
      context.monitoring.expectedStatusCodes.push(401);

      // Click trigger with dispatchEvent for reliable opening on Firefox
      const triggerButton = page.getByRole("button", { name: "Account menu" });
      await triggerButton.dispatchEvent("click");

      const userMenu = page.getByRole("menu");
      await expect(userMenu).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem = page.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      await expect(page).toHaveURL("/login?returnPath=%2Fdashboard");
    })();

    await step("Access protected routes while unauthenticated & verify redirect to login")(async () => {
      // Try accessing users page - route guard redirects client-side without API call
      await page.goto("/account/users");
      // TanStack Router adds default search params, so check that the URL starts with the expected path
      await expect(page).toHaveURL(/\/login\?returnPath=%2Faccount%2Fusers/);

      // Try accessing admin dashboard
      await page.goto("/account");
      await expect(page).toHaveURL("/login?returnPath=%2Faccount");
    })();

    // === SECURITY EDGE CASES ===
    await step("Navigate with malicious redirect URL & verify prevention")(async () => {
      await page.goto("/login?returnPath=http://hacker.com");

      // Verify malicious returnPath is stripped
      await expect(page).toHaveURL("/login");
    })();

    await step("Complete login after security check & verify authentication works")(async () => {
      await page.getByRole("textbox", { name: "Email" }).fill(existingUser.email);
      await page.getByRole("button", { name: "Log in with email" }).click();

      await expect(page).toHaveURL("/login/verify");
      await typeOneTimeCode(page, getVerificationCode());

      await expect(page).toHaveURL("/dashboard");
    })();
  });
});

test.describe("@comprehensive", () => {
  test("should enforce rate limiting for failed login attempts", async ({ page }) => {
    const context = createTestContext(page);
    const user = testUser();

    await step("Create test user")(async () => {
      await completeSignupFlow(page, expect, user, context, false);
    })();

    await step("Navigate to login and submit email & verify navigation to verification page")(async () => {
      await page.goto("/login");
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Log in with email" }).click();

      // Verify initial verification page state
      await expect(page).toHaveURL("/login/verify");
      await expect(page.getByText("Can't find your code? Check your spam folder.").first()).toBeVisible();
      await expect(page.getByText("Request a new code")).not.toBeVisible();
    })();

    await step("Enter first wrong code & verify error and focus reset")(async () => {
      await typeOneTimeCode(page, "WRONG1");

      await expectToastMessage(context, 400, "The code is wrong or no longer valid.");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
    })();

    await step("Enter second wrong code & verify error and focus reset")(async () => {
      await typeOneTimeCode(page, "WRONG2");
      await page.getByRole("button", { name: "Verify" }).click(); // Auto-submit only happens when entering the first OTP

      await expectToastMessage(context, 400, "The code is wrong or no longer valid.");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
    })();

    await step("Enter third wrong code & verify error and focus reset")(async () => {
      await typeOneTimeCode(page, "WRONG3");
      await page.getByRole("button", { name: "Verify" }).click(); // Auto-submit only happens when entering the first OTP

      await expectToastMessage(context, 400, "The code is wrong or no longer valid.");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeFocused();
    })();

    await step("Enter fourth wrong code & verify rate limiting triggers")(async () => {
      await typeOneTimeCode(page, "WRONG4");
      await page.getByRole("button", { name: "Verify" }).click(); // Auto-submit only happens when entering the first OTP

      // Verify rate limiting is enforced
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

    await step("Create test user")(async () => {
      await completeSignupFlow(page, expect, user, context, false);
    })();

    await step("Make 3 login attempts & verify each navigates to verify page")(async () => {
      // Make 3 login attempts within rate limit threshold
      for (let attempt = 1; attempt <= 3; attempt++) {
        await page.goto("/login");
        await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

        await page.getByRole("textbox", { name: "Email" }).fill(user.email);
        await page.getByRole("button", { name: "Log in with email" }).click();

        await expect(page).toHaveURL("/login/verify");
      }
    })();

    await step("Make 4th login attempt & verify rate limiting triggers")(async () => {
      await page.goto("/login");
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Log in with email" }).click();

      // Verify rate limiting prevents navigation
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
  const codeValidationTimeout = 300000; // 5 minutes (300 seconds)
  const sessionTimeout = codeValidationTimeout + 60000; // 6 minutes total

  test("should allow resend code 30 seconds after login but then not after code has expired", async ({ page }) => {
    test.setTimeout(sessionTimeout);
    const context = createTestContext(page);
    const user = testUser();

    await step("Create test user and navigate to verify page")(async () => {
      // Create user and navigate to login verification
      await completeSignupFlow(page, expect, user, context, false);
      await page.goto("/login");
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Log in with email" }).click();

      // Verify initial state
      await expect(page).toHaveURL("/login/verify");
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
      await expect(page).toHaveURL("/login/verify");
      await expect(page.getByText("Your verification code has expired")).toBeVisible();
      await expect(page.getByRole("button", { name: "Request a new code" })).not.toBeVisible();
      await expect(page.getByText("Can't find your code? Check your spam folder.")).toBeVisible();
    })();
  });

  // 5-minute request new code test is kept in the 30-second test above which also tests the 5-minute scenario
});

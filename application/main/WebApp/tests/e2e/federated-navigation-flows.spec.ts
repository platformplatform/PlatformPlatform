import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext, expectToastMessage, typeOneTimeCode } from "@shared/e2e/utils/test-assertions";
import { getVerificationCode, testUser } from "@shared/e2e/utils/test-data";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@smoke", () => {
  /**
   * FEDERATED ACCOUNTAPP ARCHITECTURE TESTS
   *
   * Tests the federated AccountApp module integration verifying:
   * - Login/signup flows render correctly via module federation
   * - Cross-SCS navigation works (main SCS -> account SCS -> main SCS)
   * - Account area routes render correctly for authenticated users
   * - Profile and sessions pages are accessible to all users
   * - Legal pages render correctly
   * - Permission enforcement (Member cannot access restricted routes)
   * - Browser back/forward navigation works across SCS boundaries
   */
  test("should handle federated navigation between main and account SCS with proper route rendering", async ({
    page
  }) => {
    const context = createTestContext(page);
    const owner = testUser();
    const member = testUser();

    // === LOGIN FLOW VIA FEDERATION ===
    await step("Navigate to login page & verify federated rendering")(async () => {
      await page.goto("/login");

      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await expect(page.getByRole("textbox", { name: "Email" })).toBeVisible();
    })();

    await step("Navigate to signup page & verify federated rendering")(async () => {
      await page.goto("/signup");

      await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();
      await expect(page.getByRole("textbox", { name: "Email" })).toBeVisible();
    })();

    // === COMPLETE SIGNUP AND CROSS-SCS NAVIGATION ===
    await step("Submit signup form & verify navigation to verification page")(async () => {
      await page.getByRole("textbox", { name: "Email" }).fill(owner.email);
      await page.getByRole("button", { name: "Sign up with email" }).click();

      await expect(page).toHaveURL("/signup/verify");
    })();

    await step("Enter verification code & complete welcome flow (cross-SCS navigation)")(async () => {
      await typeOneTimeCode(page, getVerificationCode());

      await expect(page).toHaveURL(/\/welcome/);
      await expect(page.getByRole("heading", { name: "Let's set up your account" })).toBeVisible();
      await page.getByRole("textbox", { name: "Account name" }).fill("Test Organization");
      await page.getByRole("button", { name: "Continue" }).click();

      await expect(page.getByRole("heading", { name: "Let's set up your profile" })).toBeVisible();
      await page.getByRole("textbox", { name: "First name" }).fill(owner.firstName);
      await page.getByRole("textbox", { name: "Last name" }).fill(owner.lastName);
      await page.getByRole("button", { name: "Continue" }).click();

      await expect(page).toHaveURL("/dashboard");
    })();

    // === ACCOUNT AREA NAVIGATION ===
    await step("Navigate to account page & verify dashboard renders")(async () => {
      await page.goto("/account");

      await expect(page).toHaveURL("/account");
      await expect(page.getByRole("heading", { name: "Overview" })).toBeVisible();
      await expect(page.getByRole("link", { name: "View users" })).toBeVisible();
    })();

    await step("Navigate to users page & verify account users page renders")(async () => {
      await page.getByRole("link", { name: "Users", exact: true }).click();

      await expect(page).toHaveURL("/account/users");
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
    })();

    // === PROFILE AND SESSIONS (ALL USERS) ===
    await step("Navigate to profile page & verify profile page renders")(async () => {
      await page.getByRole("link", { name: "Profile", exact: true }).click();

      await expect(page).toHaveURL("/user/profile");
      await expect(page.getByRole("heading", { name: "Profile" })).toBeVisible();
    })();

    await step("Navigate to sessions page & verify sessions page renders")(async () => {
      await page.getByRole("link", { name: "Sessions", exact: true }).click();

      await expect(page).toHaveURL("/user/sessions");
      await expect(page.getByRole("heading", { name: "Sessions" })).toBeVisible();
      await expect(page.getByText("This device")).toBeVisible();
    })();

    // === LEGAL PAGES ===
    await step("Navigate to terms page & verify legal terms page renders")(async () => {
      await page.goto("/legal/terms");

      await expect(page).toHaveURL("/legal/terms");
      await expect(page.getByRole("heading", { name: "Terms of Service", level: 1 })).toBeVisible();
    })();

    await step("Navigate to privacy page & verify legal privacy page renders")(async () => {
      await page.goto("/legal/privacy");

      await expect(page).toHaveURL("/legal/privacy");
      await expect(page.getByRole("heading", { name: "Privacy Policy", level: 1 })).toBeVisible();
    })();

    // === BACK TO MAIN SCS ===
    await step("Navigate back to dashboard & verify cross-SCS navigation to main")(async () => {
      await page.goto("/dashboard");

      await expect(page).toHaveURL("/dashboard");
    })();

    // === BROWSER HISTORY NAVIGATION ===
    await step("Use browser back button & verify navigation works across SCS")(async () => {
      await page.goBack();

      await expect(page).toHaveURL("/legal/privacy");
    })();

    await step("Use browser forward button & verify navigation works across SCS")(async () => {
      await page.goForward();

      await expect(page).toHaveURL("/dashboard");
    })();

    // === PERMISSION ENFORCEMENT ===
    await step("Navigate to users page & invite member user")(async () => {
      await page.goto("/account/users");

      await expect(page).toHaveURL("/account/users");
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(member.email);
      await page.getByRole("button", { name: "Send invite" }).click();

      await expectToastMessage(context, "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
      await expect(page.locator("tbody").first()).toContainText(member.email);
    })();

    await step("Log out from owner & verify redirect to login")(async () => {
      context.monitoring.expectedStatusCodes.push(401);

      const triggerButton = page.getByRole("button", { name: "Account menu" });
      await triggerButton.dispatchEvent("click");
      const accountMenu = page.getByRole("menu");
      await expect(accountMenu).toBeVisible();

      const logoutMenuItem = page.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      await expect(page).toHaveURL(/\/login/);
    })();

    await step("Login as member & complete welcome flow (cross-SCS navigation)")(async () => {
      await page.goto("/login");
      await page.getByRole("textbox", { name: "Email" }).fill(member.email);
      await page.getByRole("button", { name: "Log in with email" }).click();

      await expect(page).toHaveURL("/login/verify");
      await typeOneTimeCode(page, getVerificationCode());

      await expect(page).toHaveURL(/\/welcome/);
      await expect(page.getByRole("heading", { name: "Let's set up your profile" })).toBeVisible();
      await page.getByRole("textbox", { name: "First name" }).fill(member.firstName);
      await page.getByRole("textbox", { name: "Last name" }).fill(member.lastName);
      await page.getByRole("button", { name: "Continue" }).click();

      await expect(page).toHaveURL("/dashboard");
    })();

    await step("Navigate to profile as member & verify profile page renders")(async () => {
      await page.goto("/user/profile");

      await expect(page).toHaveURL("/user/profile");
      await expect(page.getByRole("heading", { name: "Profile" })).toBeVisible();
    })();

    await step("Navigate to sessions as member & verify sessions page renders")(async () => {
      await page.getByRole("link", { name: "Sessions", exact: true }).click();

      await expect(page).toHaveURL("/user/sessions");
      await expect(page.getByRole("heading", { name: "Sessions" })).toBeVisible();
    })();

    await step("Navigate to recycle-bin as member & verify access denied page")(async () => {
      await page.goto("/account/users/recycle-bin");

      await expect(page.getByRole("heading", { name: "Access denied" })).toBeVisible();
      await expect(page.getByText("You do not have permission to access this page.")).toBeVisible();
    })();
  });
});

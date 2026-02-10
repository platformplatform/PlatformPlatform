import { faker } from "@faker-js/faker";
import { expect, type Page } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext } from "@shared/e2e/utils/test-assertions";
import { step } from "@shared/e2e/utils/test-step-wrapper";

const MOCK_PROVIDER_COOKIE = "__Test_Use_Mock_Provider";

async function setMockProviderCookie(page: Page, value: string): Promise<void> {
  await page.context().addCookies([
    {
      name: MOCK_PROVIDER_COOKIE,
      value: value,
      url: "https://localhost:9000"
    }
  ]);
}

test.describe("@smoke", () => {
  /**
   * Tests Google OAuth authentication flows including:
   * 1. Signup with Google OAuth to create new tenant and user
   * 2. Logout and verify redirect to login page
   * 3. Login with Google OAuth and verify authentication
   * 4. Verify user profile shows correct email
   * 5. Logout via menu and verify redirect
   * 6. Attempt signup as existing user - verify account already exists error page
   * 7. Navigate to login from error page and complete login
   *
   * Note: Uses mock OAuth provider with unique email per test run to avoid conflicts.
   */
  test("should handle Google OAuth signup, login, and existing user signup redirect flow", async ({ page }) => {
    const context = createTestContext(page);
    const emailPrefix = faker.string.alphanumeric(10);
    const mockUserEmail = `${emailPrefix}@mock.localhost`;

    // === SIGNUP: Create mock user via Google OAuth ===

    await step("Navigate to signup page & sign up with Google OAuth")(async () => {
      await page.goto("/signup");

      await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();
      await setMockProviderCookie(page, emailPrefix);
      await page.getByRole("button", { name: "Sign up with Google" }).click();

      await expect(page).toHaveURL("/");
      await expect(page.getByRole("button", { name: "User profile menu" })).toBeVisible();
    })();

    await step("Open user profile menu & log out")(async () => {
      context.monitoring.expectedStatusCodes.push(401);
      await page.getByRole("button", { name: "User profile menu" }).dispatchEvent("click");
      const menu = page.getByRole("menu");
      await expect(menu).toBeVisible();
      const logoutMenuItem = page.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
    })();

    // === LOGIN: Verify Google OAuth login works ===

    await step("Click Google login button & verify successful authentication")(async () => {
      await setMockProviderCookie(page, emailPrefix);
      await page.getByRole("button", { name: "Log in with Google" }).click();

      await expect(page).toHaveURL("/");
      await expect(page.getByRole("button", { name: "User profile menu" })).toBeVisible();
    })();

    await step("Open user profile menu & verify mock user email displays")(async () => {
      await page.getByRole("button", { name: "User profile menu" }).dispatchEvent("click");
      const menu = page.getByRole("menu");
      await expect(menu).toBeVisible();

      await expect(page.getByText(mockUserEmail)).toBeVisible();
    })();

    await step("Log out via menu & verify redirect to login page")(async () => {
      context.monitoring.expectedStatusCodes.push(401);
      const menu = page.getByRole("menu");
      await expect(menu).toBeVisible();
      const logoutMenuItem = page.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
    })();

    // === EXISTING USER SIGNUP: Verify error page with login redirect ===

    await step("Navigate to signup page & attempt Google signup as existing user")(async () => {
      await page.goto("/signup");

      await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();
      await setMockProviderCookie(page, emailPrefix);
      await page.getByRole("button", { name: "Sign up with Google" }).click();

      await expect(page.getByRole("heading", { name: "Account already exists" })).toBeVisible();
      await expect(page.getByText("An account with this email already exists.")).toBeVisible();
      await expect(page.getByRole("button", { name: "Log in" })).toBeVisible();
    })();

    await step("Click log in button from error page & login with Google")(async () => {
      await page.getByRole("button", { name: "Log in" }).click();

      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await setMockProviderCookie(page, emailPrefix);
      await page.getByRole("button", { name: "Log in with Google" }).click();

      await expect(page).toHaveURL("/");
      await expect(page.getByRole("button", { name: "User profile menu" })).toBeVisible();
    })();
  });
});

test.describe("@comprehensive", () => {
  /**
   * Tests Google OAuth error paths, preferred tenant selection, and error page rendering including:
   * 1. Preferred tenant - signup, extract tenant ID, set localStorage, re-login and verify PreferredTenantId passed
   * 2. Access denied - user cancels OAuth consent, verify error page
   * 3. Token exchange failure - mock provider returns null tokens, verify error page
   * 4. Email not verified - mock provider returns unverified email, verify error page
   * 5. User not found on login - login with unknown email, verify error page with signup action
   * 6. Direct error page rendering for each OAuth error code with reference ID display
   */
  test("should handle preferred tenant selection and OAuth error paths with error page rendering", async ({ page }) => {
    const context = createTestContext(page);
    const emailPrefix = faker.string.alphanumeric(10);
    const mockUserEmail = `${emailPrefix}@mock.localhost`;

    // === PREFERRED TENANT: Verify PreferredTenantId is passed during Google login ===

    await step("Sign up with Google OAuth & create tenant")(async () => {
      await page.goto("/signup");

      await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();
      await setMockProviderCookie(page, emailPrefix);
      await page.getByRole("button", { name: "Sign up with Google" }).click();

      await expect(page).toHaveURL("/");
      await expect(page.getByRole("button", { name: "User profile menu" })).toBeVisible();
    })();

    let tenantId: string;

    await step("Extract tenant ID from user info & log out")(async () => {
      tenantId = await page.evaluate(() => {
        const metaTag = document.head.getElementsByTagName("meta").namedItem("userInfoEnv");
        if (!metaTag) {
          return "";
        }
        const content = JSON.parse(metaTag.content);
        return content.tenantId || "";
      });
      expect(tenantId).toBeTruthy();

      context.monitoring.expectedStatusCodes.push(401);
      await page.getByRole("button", { name: "User profile menu" }).dispatchEvent("click");
      const menu = page.getByRole("menu");
      await expect(menu).toBeVisible();
      const logoutMenuItem = page.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
    })();

    await step("Set preferred tenant, enter email & login with Google OAuth verifying query parameter")(async () => {
      await page.evaluate((tid) => localStorage.setItem("preferred-tenant", tid), tenantId);

      await page.getByLabel("Email").fill(mockUserEmail);

      let capturedUrl = "";
      await page.route("**/authentication/Google/login/start**", async (route) => {
        capturedUrl = route.request().url();
        await route.continue();
      });

      await setMockProviderCookie(page, emailPrefix);
      await page.getByRole("button", { name: "Log in with Google" }).click();

      await expect(page).toHaveURL("/");
      await expect(page.getByRole("button", { name: "User profile menu" })).toBeVisible();

      expect(capturedUrl).toContain(`PreferredTenantId=${tenantId}`);
    })();

    await step("Log out to prepare for error path tests")(async () => {
      context.monitoring.expectedStatusCodes.push(401);
      await page.getByRole("button", { name: "User profile menu" }).dispatchEvent("click");
      const menu = page.getByRole("menu");
      await expect(menu).toBeVisible();
      const logoutMenuItem = page.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
    })();

    // === MOCK PROVIDER ERROR PATHS ===

    await step("Navigate to login & trigger access denied error via mock provider")(async () => {
      await page.goto("/login");

      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await setMockProviderCookie(page, "fail:access_denied");
      await page.getByRole("button", { name: "Log in with Google" }).click();

      await expect(page.getByRole("heading", { name: "Access denied" })).toBeVisible();
      await expect(page.getByText("Authentication was cancelled or denied.")).toBeVisible();
      await expect(page.getByRole("button", { name: "Log in" })).toBeVisible();
    })();

    await step("Navigate to signup & trigger token exchange failure via mock provider")(async () => {
      await page.goto("/signup");

      await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();
      await setMockProviderCookie(page, "fail:token_exchange");
      await page.getByRole("button", { name: "Sign up with Google" }).click();

      await expect(page.getByRole("heading", { name: "Authentication failed" })).toBeVisible();
      await expect(page.getByText("We detected a security issue with your login attempt.")).toBeVisible();
      await expect(page.getByRole("button", { name: "Log in" })).toBeVisible();
    })();

    await step("Navigate to signup & trigger email not verified error via mock provider")(async () => {
      await page.goto("/signup");

      await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();
      await setMockProviderCookie(page, "fail:email_not_verified");
      await page.getByRole("button", { name: "Sign up with Google" }).click();

      await expect(page.getByRole("heading", { name: "Authentication failed" })).toBeVisible();
      await expect(page.getByText("We detected a security issue with your login attempt.")).toBeVisible();
    })();

    await step("Navigate to login & trigger user not found error with unknown email")(async () => {
      const unknownPrefix = faker.string.alphanumeric(10);
      await page.goto("/login");

      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await setMockProviderCookie(page, unknownPrefix);
      await page.getByRole("button", { name: "Log in with Google" }).click();

      await expect(page.getByRole("heading", { name: "Account not found" })).toBeVisible();
      await expect(page.getByText("No account found for this email address.")).toBeVisible();
      await expect(page.getByRole("button", { name: "Sign up" })).toBeVisible();
    })();

    // === DIRECT ERROR PAGE RENDERING ===

    await step("Navigate to user_not_found error page & verify content and reference ID")(async () => {
      await page.goto("/error?error=user_not_found&id=test-ref-001");

      await expect(page.getByRole("heading", { name: "Account not found" })).toBeVisible();
      await expect(page.getByText("No account found for this email address.")).toBeVisible();
      await expect(page.getByRole("button", { name: "Sign up" })).toBeVisible();
      await expect(page.getByText("Reference ID: test-ref-001")).toBeVisible();
    })();

    await step("Navigate to authentication_failed error page & verify content and reference ID")(async () => {
      await page.goto("/error?error=authentication_failed&id=test-ref-002");

      await expect(page.getByRole("heading", { name: "Authentication failed" })).toBeVisible();
      await expect(page.getByText("We detected a security issue with your login attempt.")).toBeVisible();
      await expect(page.getByRole("button", { name: "Log in" })).toBeVisible();
      await expect(page.getByText("Reference ID: test-ref-002")).toBeVisible();
    })();

    await step("Navigate to access_denied error page & verify content and reference ID")(async () => {
      await page.goto("/error?error=access_denied&id=test-ref-003");

      await expect(page.getByRole("heading", { name: "Access denied" })).toBeVisible();
      await expect(page.getByText("Authentication was cancelled or denied.")).toBeVisible();
      await expect(page.getByRole("button", { name: "Log in" })).toBeVisible();
      await expect(page.getByText("Reference ID: test-ref-003")).toBeVisible();
    })();

    await step("Navigate to invalid_request error page & verify content")(async () => {
      await page.goto("/error?error=invalid_request&id=test-ref-004");

      await expect(page.getByRole("heading", { name: "Invalid request" })).toBeVisible();
      await expect(page.getByText("The authentication request was invalid.")).toBeVisible();
      await expect(page.getByRole("button", { name: "Log in" })).toBeVisible();
      await expect(page.getByText("Reference ID: test-ref-004")).toBeVisible();
    })();

    await step("Navigate to identity_mismatch error page & verify content with back to login button")(async () => {
      await page.goto("/error?error=identity_mismatch&id=test-ref-005");

      await expect(page.getByRole("heading", { name: "Identity mismatch" })).toBeVisible();
      await expect(page.getByText("This account is linked to a different Google identity.")).toBeVisible();
      await expect(page.getByRole("button", { name: "Log in" })).toBeVisible();
      await expect(page.getByText("Reference ID: test-ref-005")).toBeVisible();
    })();

    await step("Navigate to session_expired error page & verify content")(async () => {
      await page.goto("/error?error=session_expired&id=test-ref-006");

      await expect(page.getByRole("heading", { name: "Session expired" })).toBeVisible();
      await expect(page.getByText("Your session has expired.")).toBeVisible();
      await expect(page.getByRole("button", { name: "Log in" })).toBeVisible();
      await expect(page.getByText("Reference ID: test-ref-006")).toBeVisible();
    })();

    await step("Navigate to unknown error code & verify fallback error page")(async () => {
      await page.goto("/error?error=some_unknown_error&id=test-ref-007");

      await expect(page.getByRole("heading", { name: "Something went wrong" })).toBeVisible();
      await expect(page.getByText("An unexpected error occurred.")).toBeVisible();
      await expect(page.getByRole("button", { name: "Log in" })).toBeVisible();
      await expect(page.getByText("Reference ID: test-ref-007")).toBeVisible();
    })();

    await step("Navigate to error page without error param & verify redirect to login")(async () => {
      await page.goto("/error");

      await expect(page).toHaveURL("/login");
    })();
  });
});

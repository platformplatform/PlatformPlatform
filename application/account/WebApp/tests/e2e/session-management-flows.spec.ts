import type { Browser, BrowserContext, Page } from "@playwright/test";
import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext, expectToastMessage, typeOneTimeCode } from "@shared/e2e/utils/test-assertions";
import { completeSignupFlow, getVerificationCode, testUser } from "@shared/e2e/utils/test-data";
import { step } from "@shared/e2e/utils/test-step-wrapper";

const accessTokenCookieName = "__Host-access-token";
const refreshTokenCookieName = "__Host-refresh-token";

async function deleteAccessTokenCookie(page: Page): Promise<void> {
  const cookies = await page.context().cookies();
  const accessToken = cookies.find((cookie) => cookie.name === accessTokenCookieName);
  if (accessToken) {
    await page.context().clearCookies({ name: accessTokenCookieName });
  }
}

async function getRefreshTokenCookie(context: BrowserContext): Promise<string | undefined> {
  const cookies = await context.cookies();
  return cookies.find((cookie) => cookie.name === refreshTokenCookieName)?.value;
}

async function setRefreshTokenCookie(context: BrowserContext, value: string, domain: string): Promise<void> {
  await context.addCookies([
    {
      name: refreshTokenCookieName,
      value,
      domain,
      path: "/",
      secure: true,
      httpOnly: true,
      sameSite: "Strict"
    }
  ]);
}

function getDomainFromPage(page: Page): string {
  const url = new URL(page.url());
  return url.hostname;
}

test.describe("@smoke", () => {
  /**
   * SESSION MANAGEMENT WORKFLOW
   *
   * Tests the session management functionality including:
   * - Opening Sessions modal via user profile menu
   * - Viewing active sessions list with current session indicator
   * - Current session card displays device info, IP, timestamps
   * - Current session does not show Revoke button
   * - Revoke individual session with confirmation dialog
   */
  test("should display sessions page with current session and handle session revocation", async ({ page }) => {
    const context = createTestContext(page);
    const owner = testUser();

    await step("Complete owner signup & verify home page")(async () => {
      await completeSignupFlow(page, expect, owner, context);
      await expect(page.getByRole("heading", { name: "Your dashboard is empty" })).toBeVisible();
    })();

    await step("Navigate to Sessions page & verify current session with badge and no Revoke button")(async () => {
      // Navigate to sessions page via URL
      await page.goto("/user/sessions");

      await expect(page.getByRole("heading", { name: "Sessions" })).toBeVisible();
      await expect(page.getByText("This device")).toBeVisible();
      await expect(page.getByText("IP address")).toBeVisible();
      await expect(page.getByText("Last active")).toBeVisible();
      await expect(page.getByText("Created")).toBeVisible();

      const currentSessionCard = page.locator("div.rounded-xl").filter({ hasText: "This device" }).first();
      await expect(currentSessionCard.getByRole("button", { name: "Revoke" })).not.toBeVisible();
    })();

    await step("Create second session from new browser context")(async () => {
      const browser = page.context().browser() as Browser;
      const secondContext = await browser.newContext();
      const secondPage = await secondContext.newPage();
      createTestContext(secondPage);

      await secondPage.goto("/login");
      await expect(secondPage.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      await secondPage.getByRole("textbox", { name: "Email" }).fill(owner.email);
      await secondPage.getByRole("button", { name: "Log in with email" }).click();
      await expect(secondPage).toHaveURL("/login/verify");
      await typeOneTimeCode(secondPage, getVerificationCode());

      await expect(secondPage).toHaveURL("/dashboard");
      await expect(secondPage.getByRole("heading", { name: "Your dashboard is empty" })).toBeVisible();

      await secondContext.close();
    })();

    await step("Refresh Sessions page & verify multiple sessions with Revoke button on non-current")(async () => {
      // Refresh page to see the new session
      await page.reload();

      await expect(page.getByRole("heading", { name: "Sessions" })).toBeVisible();

      const sessionCards = page.locator("div.rounded-xl").filter({ hasText: "IP address" });
      await expect(sessionCards).toHaveCount(2);

      const otherSessionCard = sessionCards.filter({ hasNotText: "This device" }).first();
      await expect(otherSessionCard.getByRole("button", { name: "Revoke" })).toBeVisible();
    })();

    await step("Click Revoke button on other session & verify confirmation dialog")(async () => {
      const sessionCards = page.locator("div.rounded-xl").filter({ hasText: "IP address" });
      const otherSessionCard = sessionCards.filter({ hasNotText: "This device" }).first();
      await otherSessionCard.getByRole("button", { name: "Revoke" }).click();

      await expect(page.getByRole("alertdialog", { name: "Revoke session" })).toBeVisible();
      await expect(page.getByText("Are you sure you want to revoke this session?")).toBeVisible();
    })();

    await step("Cancel revoke dialog & verify session remains")(async () => {
      await page.getByRole("button", { name: "Cancel" }).click();
      await expect(page.getByRole("alertdialog", { name: "Revoke session" })).not.toBeVisible();

      const sessionCards = page.locator("div.rounded-xl").filter({ hasText: "IP address" });
      await expect(sessionCards).toHaveCount(2);
    })();

    await step("Revoke other session & verify success toast and only current session remains")(async () => {
      const sessionCards = page.locator("div.rounded-xl").filter({ hasText: "IP address" });
      const otherSessionCard = sessionCards.filter({ hasNotText: "This device" }).first();
      await otherSessionCard.getByRole("button", { name: "Revoke" }).click();

      const revokeDialog = page.getByRole("alertdialog", { name: "Revoke session" });
      await expect(revokeDialog).toBeVisible();
      await revokeDialog.getByRole("button", { name: "Revoke", exact: true }).click();

      await expectToastMessage(context, "Session revoked successfully");

      const remainingSessionCards = page.locator("div.rounded-xl").filter({ hasText: "IP address" });
      await expect(remainingSessionCards).toHaveCount(1);
      await expect(page.getByText("This device")).toBeVisible();
    })();
  });
});

test.describe("@comprehensive", () => {
  /**
   * SESSION REVOKED ERROR PAGE
   *
   * Tests that when a session is revoked from another browser, the revoked browser
   * is redirected to the session_revoked error page with the correct message.
   *
   * Flow:
   * 1. User logs in to browser A
   * 2. User logs in to browser B (same account, different session)
   * 3. Browser B revokes the session from browser A
   * 4. Browser A deletes its access token and navigates (triggering refresh)
   * 5. Browser A is redirected to /error?error=session_revoked
   */
  test("should redirect to session-revoked error page when session is revoked from another browser", async ({
    page
  }, testInfo) => {
    test.skip(
      testInfo.project.name === "webkit",
      "WebKit __Host- cookie handling prevents refresh token from being sent after access token manipulation"
    );

    const context = createTestContext(page);
    const owner = testUser();
    const browser = page.context().browser() as Browser;

    await step("Sign up user in primary browser & verify home page")(async () => {
      await completeSignupFlow(page, expect, owner, context);
      await expect(page.getByRole("heading", { name: "Your dashboard is empty" })).toBeVisible();
    })();

    const secondContext = await browser.newContext();
    const secondPage = await secondContext.newPage();
    createTestContext(secondPage);

    await step("Login same user in secondary browser & verify home page")(async () => {
      await secondPage.goto("/login");
      await expect(secondPage.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      await secondPage.getByRole("textbox", { name: "Email" }).fill(owner.email);
      await secondPage.getByRole("button", { name: "Log in with email" }).click();
      await expect(secondPage).toHaveURL("/login/verify");
      await typeOneTimeCode(secondPage, getVerificationCode());

      await expect(secondPage).toHaveURL("/dashboard");
      await expect(secondPage.getByRole("heading", { name: "Your dashboard is empty" })).toBeVisible();
    })();

    await step("Revoke primary session from secondary browser & verify success")(async () => {
      const secondPageContext = createTestContext(secondPage);

      // Navigate to sessions page
      await secondPage.goto("/user/sessions");
      await expect(secondPage.getByRole("heading", { name: "Sessions" })).toBeVisible();

      const sessionCards = secondPage.locator("div.rounded-xl").filter({ hasText: "IP address" });
      await expect(sessionCards).toHaveCount(2);

      const otherSessionCard = sessionCards.filter({ hasNotText: "This device" }).first();
      await otherSessionCard.getByRole("button", { name: "Revoke" }).click();

      const revokeDialog = secondPage.getByRole("alertdialog", { name: "Revoke session" });
      await expect(revokeDialog).toBeVisible();
      await revokeDialog.getByRole("button", { name: "Revoke", exact: true }).click();

      await expectToastMessage(secondPageContext, "Session revoked successfully");
    })();

    await step("Navigate in revoked session & verify session_revoked error page")(async () => {
      await deleteAccessTokenCookie(page);
      context.monitoring.expectedStatusCodes.push(401);

      // Trigger offline/online event to force the app to make an API call
      await page.evaluate(() => {
        window.dispatchEvent(new Event("offline"));
        window.dispatchEvent(new Event("online"));
      });

      await expect(page).toHaveURL(/\/error\?.*error=session_revoked/);
      await expect(page.getByRole("heading", { name: "Session ended" })).toBeVisible();
      await expect(page.getByText("Your session was ended from another device.")).toBeVisible();
      await expect(page.getByRole("button", { name: "Log in" })).toBeVisible();
    })();

    await step("Click login on session_revoked page & verify login page")(async () => {
      await page.getByRole("button", { name: "Log in" }).click();

      await expect(page).toHaveURL(/\/login/);
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
    })();

    await secondContext.close();
  });

  /**
   * REPLAY ATTACK DETECTION ERROR PAGE
   *
   * Tests that when a refresh token is "stolen" and used from another browser,
   * both browsers are eventually redirected to the replay_attack error page.
   *
   * Flow:
   * 1. User logs in to browser A (refresh token version 1)
   * 2. Copy refresh token from browser A to browser B
   * 3. Browser B uses stolen token twice (version becomes 3, grace period ends)
   * 4. Browser A tries to refresh (replay detected, session revoked)
   * 5. Browser B tries to refresh (session already revoked)
   * 6. Both browsers see the replay_attack error page
   */
  test("should redirect to replay_attack error page when refresh token replay is detected", async ({
    page
  }, testInfo) => {
    test.skip(
      testInfo.project.name === "webkit",
      "WebKit __Host- cookie handling prevents programmatic cookie manipulation required for replay attack simulation"
    );

    const context = createTestContext(page);
    const owner = testUser();
    const browser = page.context().browser() as Browser;

    await step("Sign up user & verify home page")(async () => {
      await completeSignupFlow(page, expect, owner, context);
      await expect(page.getByRole("heading", { name: "Your dashboard is empty" })).toBeVisible();
    })();

    const stolenRefreshToken = await getRefreshTokenCookie(page.context());
    expect(stolenRefreshToken).toBeDefined();

    const secondContext = await browser.newContext();
    const secondPage = await secondContext.newPage();
    createTestContext(secondPage);

    await step("Inject stolen refresh token into attacker browser & verify token set")(async () => {
      const domain = getDomainFromPage(page);
      await setRefreshTokenCookie(secondContext, stolenRefreshToken as string, domain);
    })();

    await step("Use stolen token twice in attacker browser & verify access granted")(async () => {
      const secondPageContext = createTestContext(secondPage);
      secondPageContext.monitoring.expectedStatusCodes.push(401);

      await secondPage.goto("/dashboard");
      await expect(secondPage).toHaveURL("/dashboard");
      await expect(secondPage.getByRole("button", { name: "User menu" })).toBeVisible();

      await deleteAccessTokenCookie(secondPage);
      await secondPage.goto("/account/users");
      await expect(secondPage).toHaveURL("/account/users");
      await expect(secondPage.getByRole("heading", { name: "Users" })).toBeVisible();
    })();

    await step("Navigate in victim browser after replay & verify replay_attack error page")(async () => {
      await secondPage.route("**/api/**", (route) => route.abort());
      await deleteAccessTokenCookie(page);
      context.monitoring.expectedStatusCodes.push(401);

      // Trigger offline/online event to force the app to make an API call
      await page.evaluate(() => {
        window.dispatchEvent(new Event("offline"));
        window.dispatchEvent(new Event("online"));
      });

      await expect(page).toHaveURL(/\/error\?.*error=replay_attack/);
      await expect(page.getByRole("heading", { name: "Security alert" })).toBeVisible();
      await expect(page.getByText("We detected suspicious activity on your account.")).toBeVisible();
      await expect(page.getByRole("button", { name: "Log in" })).toBeVisible();
    })();

    await step("Attacker browser detects replay & shows replay_attack error page")(async () => {
      const secondPageContext = createTestContext(secondPage);
      secondPageContext.monitoring.expectedStatusCodes.push(401);
      await deleteAccessTokenCookie(secondPage);
      await secondPage.unroute("**/api/**");

      await secondPage.evaluate(() => {
        window.dispatchEvent(new Event("offline"));
        window.dispatchEvent(new Event("online"));
      });

      await expect(secondPage).toHaveURL(/\/error\?.*error=replay_attack/);
      await expect(secondPage.getByRole("heading", { name: "Security alert" })).toBeVisible();
      await expect(secondPage.getByText("We detected suspicious activity on your account.")).toBeVisible();
    })();

    await step("Click login on replay_attack page & verify login page")(async () => {
      await page.getByRole("button", { name: "Log in" }).click();

      await expect(page).toHaveURL(/\/login/);
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
    })();

    await secondContext.close();
  });
});

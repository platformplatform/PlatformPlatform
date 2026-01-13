import type { Browser } from "@playwright/test";
import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext, expectToastMessage } from "@shared/e2e/utils/test-assertions";
import { completeSignupFlow, getVerificationCode, testUser } from "@shared/e2e/utils/test-data";
import { step } from "@shared/e2e/utils/test-step-wrapper";

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
  test("should display sessions modal with current session and handle session revocation", async ({ page }) => {
    const context = createTestContext(page);
    const owner = testUser();
    const sessionsDialog = page.getByRole("dialog", { name: "Sessions" });

    await step("Complete owner signup & verify welcome page")(async () => {
      await completeSignupFlow(page, expect, owner, context);
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
    })();

    await step("Open Sessions modal & verify current session with badge and no Revoke button")(async () => {
      await page.getByRole("button", { name: "User profile menu" }).click();
      await expect(page.getByRole("menu")).toBeVisible();
      await page.getByRole("menuitem", { name: "Sessions" }).click();

      await expect(sessionsDialog).toBeVisible();
      await expect(sessionsDialog.getByRole("heading", { name: "Sessions" })).toBeVisible();
      await expect(sessionsDialog.getByText("Current session")).toBeVisible();
      await expect(sessionsDialog.getByText("IP:")).toBeVisible();
      await expect(sessionsDialog.getByText("Last active:")).toBeVisible();
      await expect(sessionsDialog.getByText("Created:")).toBeVisible();

      const currentSessionCard = sessionsDialog
        .locator("div.rounded-lg.border")
        .filter({ hasText: "Current session" })
        .first();
      await expect(currentSessionCard.getByRole("button", { name: "Revoke" })).not.toBeVisible();
    })();

    await step("Close Sessions modal & create second session from new browser context")(async () => {
      await sessionsDialog.locator("svg.cursor-pointer").click();
      await expect(sessionsDialog).not.toBeVisible();

      const browser = page.context().browser() as Browser;
      const secondContext = await browser.newContext();
      const secondPage = await secondContext.newPage();
      createTestContext(secondPage);

      await secondPage.goto("/login");
      await expect(secondPage.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      await secondPage.getByRole("textbox", { name: "Email" }).fill(owner.email);
      await secondPage.getByRole("button", { name: "Continue" }).click();
      await expect(secondPage).toHaveURL("/login/verify");
      await secondPage.keyboard.type(getVerificationCode());

      await expect(secondPage).toHaveURL("/admin");
      await expect(secondPage.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      await secondContext.close();
    })();

    await step("Re-open Sessions modal & verify multiple sessions with Revoke button on non-current")(async () => {
      await page.getByRole("button", { name: "User profile menu" }).click();
      await expect(page.getByRole("menu")).toBeVisible();
      await page.getByRole("menuitem", { name: "Sessions" }).click();

      await expect(sessionsDialog).toBeVisible();
      await expect(sessionsDialog.getByRole("heading", { name: "Sessions" })).toBeVisible();

      const sessionCards = sessionsDialog.locator("div.rounded-lg.border").filter({ hasText: "IP:" });
      await expect(sessionCards).toHaveCount(2);

      const otherSessionCard = sessionCards.filter({ hasNotText: "Current session" }).first();
      await expect(otherSessionCard.getByRole("button", { name: "Revoke" })).toBeVisible();
    })();

    await step("Click Revoke button on other session & verify confirmation dialog")(async () => {
      const sessionCards = sessionsDialog.locator("div.rounded-lg.border").filter({ hasText: "IP:" });
      const otherSessionCard = sessionCards.filter({ hasNotText: "Current session" }).first();
      await otherSessionCard.getByRole("button", { name: "Revoke" }).click();

      await expect(page.getByRole("alertdialog", { name: "Revoke session" })).toBeVisible();
      await expect(page.getByText("Are you sure you want to revoke this session?")).toBeVisible();
    })();

    await step("Cancel revoke dialog & verify session remains")(async () => {
      await page.getByRole("button", { name: "Cancel" }).click();
      await expect(page.getByRole("alertdialog", { name: "Revoke session" })).not.toBeVisible();

      const sessionCards = sessionsDialog.locator("div.rounded-lg.border").filter({ hasText: "IP:" });
      await expect(sessionCards).toHaveCount(2);
    })();

    await step("Revoke other session & verify success toast and only current session remains")(async () => {
      const sessionCards = sessionsDialog.locator("div.rounded-lg.border").filter({ hasText: "IP:" });
      const otherSessionCard = sessionCards.filter({ hasNotText: "Current session" }).first();
      await otherSessionCard.getByRole("button", { name: "Revoke" }).click();

      const revokeDialog = page.getByRole("alertdialog", { name: "Revoke session" });
      await expect(revokeDialog).toBeVisible();
      await revokeDialog.getByRole("button", { name: "Revoke", exact: true }).click();

      await expectToastMessage(context, "Session revoked successfully");

      const remainingSessionCards = sessionsDialog.locator("div.rounded-lg.border").filter({ hasText: "IP:" });
      await expect(remainingSessionCards).toHaveCount(1);
      await expect(sessionsDialog.getByText("Current session")).toBeVisible();
    })();
  });
});

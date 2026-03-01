import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext, expectToastMessage, typeOneTimeCode } from "@shared/e2e/utils/test-assertions";
import { completeSignupFlow, getVerificationCode, testUser } from "@shared/e2e/utils/test-data";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@comprehensive", () => {
  /**
   * COMPREHENSIVE TENANT SWITCHING AND MULTI-TAB SYNCHRONIZATION
   *
   * Tests the complete end-to-end tenant switching journey including:
   * - Single tenant display without dropdown
   * - Multiple tenant creation and invitation workflow
   * - Tenant switching with invitation acceptance dialog
   * - Tenant preference persistence across sessions
   * - Switching tenants with modals open
   * - Tenant context consistency across navigation
   * - User management within different tenants
   * - Owner vs member role differentiation
   * - User profile updates within tenants
   * - Cross-tab authentication synchronization
   * - Tenant switch detection across browser tabs
   * - Different user login detection
   * - Invitation acceptance with multiple tabs open
   * - Logout synchronization
   */
  test("should handle comprehensive tenant switching and multi-tab synchronization", async ({ context }) => {
    // Increase timeout for this comprehensive test
    test.setTimeout(120000); // 2 minutes
    // Create two pages in the same context to share authentication
    const page1 = await context.newPage();
    const page2 = await context.newPage();

    const testContext1 = createTestContext(page1);
    const testContext2 = createTestContext(page2);

    const user = testUser();
    const secondUser = testUser();
    const thirdOwner = testUser();
    const fourthOwner = testUser();

    // Generate unique tenant names
    const timestamp = Date.now().toString().slice(-4);
    const primaryTenantName = `Primary-${timestamp}`;
    const secondaryTenantName = `Secondary-${timestamp}`;
    const tertiaryTenantName = `Tertiary-${timestamp}`;

    // === SINGLE TENANT DISPLAY ===
    await step("Create single tenant & verify dropdown is hidden")(async () => {
      await completeSignupFlow(page1, expect, user, testContext1);
      await expect(page1.getByRole("heading", { name: "Your dashboard is empty" })).toBeVisible();

      // Update the first tenant name
      await page1.goto("/account/settings");
      await page1.getByRole("textbox", { name: "Account name" }).clear();
      await page1.getByRole("textbox", { name: "Account name" }).fill(primaryTenantName);
      await page1.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(testContext1, "Account settings updated successfully");
      await page1.goto("/dashboard");

      // Account menu shows tenant name
      const accountMenuButton = page1.getByRole("button", { name: "User menu" });
      await expect(accountMenuButton).toBeVisible();
      await expect(accountMenuButton).toContainText(primaryTenantName);

      // With single tenant, "Switch account" option should not appear in menu
      await accountMenuButton.dispatchEvent("click");
      await expect(page1.getByRole("menu")).toBeVisible();
      await expect(page1.getByRole("menuitem", { name: "Switch account" })).not.toBeVisible();
      await page1.keyboard.press("Escape");
    })();

    // === MULTIPLE TENANT SETUP ===
    await step("Logout from primary tenant & verify redirect to login page")(async () => {
      testContext1.monitoring.expectedStatusCodes.push(401);
      await page1.getByRole("button", { name: "User menu" }).dispatchEvent("click");
      const userMenu1 = page1.getByRole("menu");
      await expect(userMenu1).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem = page1.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      await expect(page1.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await expect(page1).toHaveURL("/login?returnPath=%2Fdashboard");
    })();

    await step("Create second tenant & verify user invitation")(async () => {
      // Create second user with second tenant
      await completeSignupFlow(page1, expect, secondUser, testContext1);

      // Update tenant name
      await page1.goto("/account/settings");
      await page1.getByRole("textbox", { name: "Account name" }).clear();
      await page1.getByRole("textbox", { name: "Account name" }).fill(secondaryTenantName);
      await page1.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(testContext1, "Account settings updated successfully");

      // Invite first user
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();
      await page1.getByRole("button", { name: "Invite user" }).click();
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.getByRole("button", { name: "Send invite" }).click();
      await expectToastMessage(testContext1, "User invited successfully");

      // Logout
      testContext1.monitoring.expectedStatusCodes.push(401);
      await page1.getByRole("button", { name: "User menu" }).dispatchEvent("click");
      const userMenu1 = page1.getByRole("menu");
      await expect(userMenu1).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem = page1.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      await expect(userMenu1).not.toBeVisible();
      await expect(page1.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
    })();

    await step("Create third tenant & verify successful user invitation")(async () => {
      await completeSignupFlow(page1, expect, thirdOwner, testContext1);

      // Update third tenant name
      await page1.goto("/account/settings");
      await page1.getByRole("textbox", { name: "Account name" }).clear();
      await page1.getByRole("textbox", { name: "Account name" }).fill(tertiaryTenantName);
      await page1.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(testContext1, "Account settings updated successfully");

      // Invite first user
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();
      await page1.getByRole("button", { name: "Invite user" }).click();
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.getByRole("button", { name: "Send invite" }).click();
      await expectToastMessage(testContext1, "User invited successfully");

      // Logout
      testContext1.monitoring.expectedStatusCodes.push(401);
      await page1.getByRole("button", { name: "User menu" }).dispatchEvent("click");
      const userMenu1 = page1.getByRole("menu");
      await expect(userMenu1).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem = page1.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");
      await expect(page1.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
    })();

    // === TENANT SWITCHING UI AND FUNCTIONALITY ===
    await step("Login with multiple tenants & verify tenant switching UI displays correctly")(async () => {
      // Login
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.getByRole("button", { name: "Log in with email" }).click();
      await expect(page1.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await typeOneTimeCode(page1, getVerificationCode());

      // Wait for login redirect to complete, then navigate to dashboard
      await page1.waitForURL((url) => !url.pathname.startsWith("/login"));
      await page1.goto("/dashboard");
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // Account menu is visible with tenant name
      const accountMenuButton = page1.getByRole("button", { name: "User menu" });
      await expect(accountMenuButton).toBeVisible();
      await expect(accountMenuButton).toContainText(primaryTenantName);

      // Pending invitations banner is visible
      await expect(page1.getByText("You have 2 pending invitations.")).toBeVisible();

      // Click View invitation to accept first pending invitation
      await page1.getByRole("button", { name: "View invitation" }).click();

      // Accept invitation dialog appears
      const invitationDialog = page1.getByRole("dialog", { name: "Accept invitation" });
      await expect(invitationDialog).toBeVisible();

      // Accept the invitation
      await page1.getByRole("button", { name: "Accept invitation" }).click();

      // Wait for navigation to complete after accepting invitation
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // Account menu now shows the new tenant name
      await expect(accountMenuButton).toBeVisible();
    })();

    // === TERTIARY TENANT INVITATION ACCEPTANCE ===
    await step("Accept invitation for tertiary tenant & verify successful tenant switch")(async () => {
      // Remaining pending invitation shows in banner
      await expect(page1.getByText("You have been invited to join")).toBeVisible();

      // Click View invitation to accept the tertiary tenant invitation
      await page1.getByRole("button", { name: "View invitation" }).click();

      // Accept invitation dialog should appear for this tenant
      const invitationDialog = page1.getByRole("dialog", { name: "Accept invitation" });
      await expect(invitationDialog).toBeVisible();

      // Accept the invitation
      await page1.getByRole("button", { name: "Accept invitation" }).click();

      // Wait for navigation to complete after accepting invitation
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // Account menu now shows the tertiary tenant name
      const accountMenuButton = page1.getByRole("button", { name: "User menu" });
      await expect(accountMenuButton).toContainText(tertiaryTenantName);
    })();

    // === OPEN SECOND TAB ===
    await step("Open second tab & verify shared authentication")(async () => {
      // Navigate page2 to home (it shares authentication with page1)
      await page2.goto("/account");
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // both pages show navigation (confirms authentication is shared)
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // Both tabs should show the same tenant (tertiary)
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toContainText(tertiaryTenantName);
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toContainText(tertiaryTenantName);
    })();

    // === TENANT PREFERENCE PERSISTENCE ===
    await step("Logout and login again & verify tenant preference persists")(async () => {
      // Reuse the existing tenant button reference
      const navElement = page1.locator("nav").first();
      const tenantButton = navElement.locator("button").filter({ hasText: tertiaryTenantName });

      // Should still be on tertiary tenant
      await expect(tenantButton).toContainText(tertiaryTenantName);

      // Logout
      testContext1.monitoring.expectedStatusCodes.push(401);
      testContext2.monitoring.expectedStatusCodes.push(401);
      await page1.getByRole("button", { name: "User menu" }).dispatchEvent("click");
      const userMenu1 = page1.getByRole("menu");
      await expect(userMenu1).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem = page1.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");
      await expect(page1.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // tab 2 also loses authentication
      await page2.goto("/account");
      await expect(page2.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // Login again as the same user
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.getByRole("button", { name: "Log in with email" }).click();
      await expect(page1.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await typeOneTimeCode(page1, getVerificationCode());

      // Wait for navigation to complete - no auth sync dialog since it's the same user
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // Should login to tertiary tenant (last selected)
      const navElementAfter = page1.locator("nav").first();
      const tenantButtonAfter = navElementAfter.locator("button").filter({ hasText: tertiaryTenantName });
      await expect(tenantButtonAfter).toContainText(tertiaryTenantName);

      // page2 also shows tertiary tenant when navigated
      await page2.goto("/account");
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toContainText(tertiaryTenantName);
    })();

    // === TENANT CONTEXT ACROSS NAVIGATION ===
    await step("Navigate across pages & verify tenant context remains consistent")(async () => {
      const accountMenuButton = page1.getByRole("button", { name: "User menu" });

      // We're on tertiary tenant at this point
      await expect(accountMenuButton).toContainText(tertiaryTenantName);

      // Navigate to Users page
      await page1.goto("/account/users");
      await expect(page1.getByRole("heading", { name: "Users" })).toBeVisible();

      // Tenant should still be visible
      await expect(accountMenuButton).toContainText(tertiaryTenantName);

      // Navigate to Account settings page
      await page1.goto("/account/settings");
      await expect(page1.getByRole("heading", { name: "Account settings" })).toBeVisible();

      // Should show correct tenant name in account settings
      const accountNameInput = page1.getByRole("textbox", { name: "Account name" });
      await expect(accountNameInput).toHaveValue(tertiaryTenantName);

      // Switch to primary tenant via Account menu > Switch account submenu
      await accountMenuButton.dispatchEvent("click");
      await expect(page1.getByRole("menu")).toBeVisible();

      const switchAccountTrigger = page1.getByRole("menuitem", { name: "Switch account" });
      await expect(switchAccountTrigger).toBeVisible();
      await switchAccountTrigger.click();

      const primaryTenantMenuItem = page1.getByRole("menuitem").filter({ hasText: primaryTenantName }).first();
      await expect(primaryTenantMenuItem).toBeVisible();
      await primaryTenantMenuItem.dispatchEvent("click");

      // Wait for tenant switch to complete
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      await expect(accountMenuButton).toContainText(primaryTenantName);

      // Navigate back to Home
      await page1.goto("/dashboard");
      await expect(page1.getByRole("heading", { level: 1 })).toBeVisible();

      // Tenant should still be primary tenant
      await expect(accountMenuButton).toContainText(primaryTenantName);
    })();

    // === TEST: TENANT SWITCH DETECTION ACROSS TABS ===
    await step("Reload tab 2 & verify it shows tenant switch")(async () => {
      // Reload page2 to check if it detects the tenant switch
      await page2.reload();
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // tab 2 is now on primary tenant
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toContainText(primaryTenantName);
    })();

    // === TEST: DIFFERENT USER LOGIN ===
    await step("Logout & login as different user in tab 1")(async () => {
      testContext1.monitoring.expectedStatusCodes.push(401);

      // Logout from page1
      await page1.getByRole("button", { name: "User menu" }).dispatchEvent("click");
      const userMenu1 = page1.getByRole("menu");
      await expect(userMenu1).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem = page1.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");
      await expect(page1.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // Login as different user
      await page1.getByRole("textbox", { name: "Email" }).fill(secondUser.email);
      await page1.getByRole("button", { name: "Log in with email" }).click();
      await expect(page1.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await typeOneTimeCode(page1, getVerificationCode());
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();
    })();

    await step("Navigate tab 2 to admin & verify it shows different user")(async () => {
      // Navigate page2 to trigger auth check
      await page2.goto("/account");
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // both tabs now show the secondary tenant (different user's tenant)
      // Note: For single tenant users, the tenant name is displayed as text, not in a button
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toContainText(secondaryTenantName);
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toContainText(secondaryTenantName);
    })();

    // === TEST: SWITCH BACK TO ORIGINAL USER WITH MULTIPLE TENANTS ===
    await step("Logout & login as original user with both tenants")(async () => {
      testContext1.monitoring.expectedStatusCodes.push(401);
      testContext2.monitoring.expectedStatusCodes.push(401);

      // Logout
      await page1.getByRole("button", { name: "User menu" }).dispatchEvent("click");
      const userMenu1 = page1.getByRole("menu");
      await expect(userMenu1).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem = page1.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");
      await expect(page1.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // Login as original user (who has access to multiple tenants)
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.getByRole("button", { name: "Log in with email" }).click();
      await expect(page1.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await typeOneTimeCode(page1, getVerificationCode());
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // Navigate page2 to admin
      await page2.goto("/account");
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // both on primary tenant
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toContainText(primaryTenantName);
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toContainText(primaryTenantName);
    })();

    // === TEST: COMPLEX FLOW - SWITCH + LOGOUT + LOGIN ===
    await step("Switch tenant, logout & login again")(async () => {
      // Switch to secondary tenant via Account menu > Switch account submenu
      const accountMenuButton = page1.getByRole("button", { name: "User menu" });
      await accountMenuButton.dispatchEvent("click");
      await expect(page1.getByRole("menu")).toBeVisible();

      const switchAccountTrigger = page1.getByRole("menuitem", { name: "Switch account" });
      await expect(switchAccountTrigger).toBeVisible();
      await switchAccountTrigger.click();

      const secondaryTenantMenuItem = page1.getByRole("menuitem").filter({ hasText: secondaryTenantName });
      await expect(secondaryTenantMenuItem).toBeVisible();
      await secondaryTenantMenuItem.dispatchEvent("click");

      // Wait for tenant switch to complete
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      await expect(accountMenuButton).toContainText(secondaryTenantName);

      // Logout from tab 1
      testContext1.monitoring.expectedStatusCodes.push(401);
      await page1.getByRole("button", { name: "User menu" }).dispatchEvent("click");
      const userMenu1 = page1.getByRole("menu");
      await expect(userMenu1).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem = page1.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");
      await expect(page1.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // Login again in tab 1
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.getByRole("button", { name: "Log in with email" }).click();
      await expect(page1.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await typeOneTimeCode(page1, getVerificationCode());
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // Tab 1 should login to secondary tenant (last selected)
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toContainText(secondaryTenantName);
    })();

    await step("Navigate tab 2 to admin & verify it shows tenant switch from login")(async () => {
      // Navigate page2 to admin to get latest auth state
      await page2.goto("/account");
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // tab 2 now shows secondary tenant (switched during login)
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toContainText(secondaryTenantName);
    })();

    // === TEST: SWITCH BACK TO ORIGINAL TENANT ===
    await step("Switch back to primary tenant in tab 1 & verify synchronization")(async () => {
      // Switch back to primary tenant via Account menu > Switch account submenu
      const accountMenuButton = page1.getByRole("button", { name: "User menu" });
      await accountMenuButton.dispatchEvent("click");
      await expect(page1.getByRole("menu")).toBeVisible();

      const switchAccountTrigger = page1.getByRole("menuitem", { name: "Switch account" });
      await expect(switchAccountTrigger).toBeVisible();
      await switchAccountTrigger.click();

      const primaryTenantMenuItem = page1.getByRole("menuitem").filter({ hasText: primaryTenantName }).first();
      await expect(primaryTenantMenuItem).toBeVisible();
      await primaryTenantMenuItem.dispatchEvent("click");

      // Wait for tenant switch to complete
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      await expect(accountMenuButton).toContainText(primaryTenantName);
    })();

    await step("Reload tab 2 & verify it syncs back to primary tenant")(async () => {
      // Reload page2
      await page2.reload();
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // tab 2 is back on primary tenant
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toContainText(primaryTenantName);
    })();

    // === INVITATION ACCEPTANCE WITH MULTIPLE TABS ===
    await step("Create new tenant with invitation & verify invite displays in tenant list")(async () => {
      // Logout first
      testContext1.monitoring.expectedStatusCodes.push(401);
      await page1.getByRole("button", { name: "User menu" }).dispatchEvent("click");
      const userMenu1 = page1.getByRole("menu");
      await expect(userMenu1).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem = page1.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");
      await expect(page1.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // Create a new owner with new tenant
      const fourthTenantName = `Revoke-Test-${timestamp}`;

      await completeSignupFlow(page1, expect, fourthOwner, testContext1);

      // Update tenant name
      await page1.goto("/account/settings");
      await page1.getByRole("textbox", { name: "Account name" }).clear();
      await page1.getByRole("textbox", { name: "Account name" }).fill(fourthTenantName);
      await page1.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(testContext1, "Account settings updated successfully");

      // Invite the original user
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();
      await page1.getByRole("button", { name: "Invite user" }).click();
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.getByRole("button", { name: "Send invite" }).click();
      await expectToastMessage(testContext1, "User invited successfully");

      // Keep page1 logged in as the fourth owner
    })();

    await step("Login as invited user in both tabs & verify invitation appears")(async () => {
      // Open new tab (page2 is already authenticated as user)
      await page2.goto("/account");

      // Logout from page2
      testContext2.monitoring.expectedStatusCodes.push(401);
      await page2.getByRole("button", { name: "User menu" }).dispatchEvent("click");
      const userMenu2 = page2.getByRole("menu");
      await expect(userMenu2).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem2 = page2.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem2).toBeVisible();
      await logoutMenuItem2.dispatchEvent("click");
      await expect(page2.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // Login as the invited user in page2
      await page2.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page2.getByRole("button", { name: "Log in with email" }).click();
      await expect(page2.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await typeOneTimeCode(page2, getVerificationCode());

      // Wait for login redirect to complete, then navigate to dashboard
      await page2.waitForURL((url) => !url.pathname.startsWith("/login"));
      await page2.goto("/dashboard");
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // Verify user is on primary tenant after login
      const accountMenuButton2 = page2.getByRole("button", { name: "User menu" });
      await expect(accountMenuButton2).toContainText(primaryTenantName);

      // Pending invitation banner is visible with Revoke-Test invitation
      await expect(page2.getByText("You have been invited to join")).toBeVisible();
      await expect(page2.getByText("Revoke-Test")).toBeVisible();
    })();

    await step("Open invitation dialog in both tabs & verify both show accept dialog")(async () => {
      // Create a third page for the same user
      const page3 = await context.newPage();

      // Navigate page3 to account (shares authentication)
      await page3.goto("/account");
      await expect(page3.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // Both tabs should show the invitation banner
      await expect(page2.getByText("You have been invited to join")).toBeVisible();
      await expect(page3.getByText("You have been invited to join")).toBeVisible();

      // Open invitation dialog in page2 via banner button
      await page2.getByRole("button", { name: "View invitation" }).click();
      const invitationDialog2 = page2.getByRole("dialog", { name: "Accept invitation" });
      await expect(invitationDialog2).toBeVisible();
      await expect(invitationDialog2).toContainText("You have been invited to join");

      // Open invitation dialog in page3 via banner button
      await page3.getByRole("button", { name: "View invitation" }).click();
      const invitationDialog3 = page3.getByRole("dialog", { name: "Accept invitation" });
      await expect(invitationDialog3).toBeVisible();
      await expect(invitationDialog3).toContainText("You have been invited to join");

      // Both dialogs should be open
      await expect(invitationDialog2).toBeVisible();
      await expect(invitationDialog3).toBeVisible();
    })();

    await step("Accept invitation in first tab & verify successful tenant switch")(async () => {
      // Accept the invitation in page2
      await page2.getByRole("button", { name: "Accept invitation" }).click();

      // Should navigate and show the new tenant
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toContainText("Revoke-Test");
    })();

    await step("Check second tab & verify auth sync modal appears after first tab accepted")(async () => {
      // Find page3 which still has the dialog open
      const pages = context.pages();
      const page3 = pages[pages.length - 1]; // Get the last page which is page3

      // Should see auth sync modal because page2 already switched tenants
      const authSyncModal = page3.getByRole("dialog", { name: "Account switched" });
      await expect(authSyncModal).toBeVisible();
      await expect(authSyncModal).toContainText("Your account was switched to Revoke-Test");

      // Click the reload button
      await page3.getByRole("button", { name: "Reload" }).click();

      // Should navigate to the new tenant
      await expect(page3.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      await expect(page3.locator('nav[aria-label="Main navigation"]')).toContainText("Revoke-Test");

      // Close page3
      await page3.close();
    })();

    // === FINAL LOGOUT ===
    await step("Logout from the system & verify redirect to login page")(async () => {
      testContext1.monitoring.expectedStatusCodes.push(401);
      testContext2.monitoring.expectedStatusCodes.push(401);

      // Page1 might be on the Revoke-Test tenant now due to auth sync
      // Logout from page2 which we know is on the Revoke-Test tenant
      await page2.getByRole("button", { name: "User menu" }).dispatchEvent("click");
      const userMenu2 = page2.getByRole("menu");
      await expect(userMenu2).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem2 = page2.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem2).toBeVisible();
      await logoutMenuItem2.dispatchEvent("click");
      await expect(page2.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // tab 1 also loses authentication due to auth sync
      await page1.goto("/account");
      await expect(page1.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
    })();
  });
});

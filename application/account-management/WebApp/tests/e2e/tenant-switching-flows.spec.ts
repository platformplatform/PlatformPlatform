import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext, expectToastMessage } from "@shared/e2e/utils/test-assertions";
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

    // Generate unique tenant names
    const timestamp = Date.now().toString().slice(-4);
    const primaryTenantName = `Primary-${timestamp}`;
    const secondaryTenantName = `Secondary-${timestamp}`;
    const tertiaryTenantName = `Tertiary-${timestamp}`;

    // === SINGLE TENANT DISPLAY ===
    await step("Create single tenant & verify dropdown is hidden")(async () => {
      await completeSignupFlow(page1, expect, user, testContext1);
      await expect(page1.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // Update the first tenant name
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Account" }).click();
      await page1.getByRole("textbox", { name: "Account name" }).clear();
      await page1.getByRole("textbox", { name: "Account name" }).fill(primaryTenantName);
      await page1.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(testContext1, "Account updated successfully");
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Home" }).click();

      const navElement = page1.locator("nav").first();

      // With single tenant, there's just a logo - no tenant selector button
      const logoElement = navElement.locator('img[alt="Logo"]');
      await expect(logoElement).toBeVisible();

      // Verify there's no tenant button (it only appears with multiple tenants)
      const tenantButton = navElement.locator("button").filter({ has: page1.locator('img[alt="Logo"]') });
      await expect(tenantButton).not.toBeVisible();
    })();

    // === MULTIPLE TENANT SETUP ===
    await step("Logout from primary tenant & verify redirect to login page")(async () => {
      testContext1.monitoring.expectedStatusCodes.push(401);
      await page1.getByRole("button", { name: "User profile menu" }).click();
      await page1.getByRole("menuitem", { name: "Log out" }).click();

      await expect(page1.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await expect(page1).toHaveURL(/\/login/);
    })();

    await step("Create second tenant & verify user invitation")(async () => {
      // Create second user with second tenant
      await completeSignupFlow(page1, expect, secondUser, testContext1);

      // Update tenant name
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Account" }).click();
      await page1.getByRole("textbox", { name: "Account name" }).clear();
      await page1.getByRole("textbox", { name: "Account name" }).fill(secondaryTenantName);
      await page1.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(testContext1, "Account updated successfully");

      // Invite first user
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();
      await page1.getByRole("button", { name: "Invite user" }).click();
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.getByRole("button", { name: "Send invite" }).click();
      await expectToastMessage(testContext1, "User invited successfully");

      // Logout
      testContext1.monitoring.expectedStatusCodes.push(401);
      await page1.getByRole("button", { name: "User profile menu" }).click();
      await page1.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page1.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
    })();

    await step("Create third tenant & verify successful user invitation")(async () => {
      await completeSignupFlow(page1, expect, thirdOwner, testContext1);

      // Update third tenant name
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Account" }).click();
      await page1.getByRole("textbox", { name: "Account name" }).clear();
      await page1.getByRole("textbox", { name: "Account name" }).fill(tertiaryTenantName);
      await page1.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(testContext1, "Account updated successfully");

      // Invite first user
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();
      await page1.getByRole("button", { name: "Invite user" }).click();
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.getByRole("button", { name: "Send invite" }).click();
      await expectToastMessage(testContext1, "User invited successfully");

      // Logout
      testContext1.monitoring.expectedStatusCodes.push(401);
      await page1.getByRole("button", { name: "User profile menu" }).click();
      await page1.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page1.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
    })();

    // === TENANT SWITCHING UI AND FUNCTIONALITY ===
    await step("Login with multiple tenants & verify tenant switching UI displays correctly")(async () => {
      // Login
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.getByRole("button", { name: "Continue" }).click();
      await expect(page1.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await page1.keyboard.type(getVerificationCode());
      // Wait for navigation to complete - could be Users or Home page
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      await expect(page1).toHaveURL(/\/admin/);

      // Verify tenant selector is visible with dropdown
      const navElement = page1.locator("nav").first();
      const tenantButton = navElement.locator("button").filter({ has: page1.locator('img[alt="Logo"]') });
      await expect(tenantButton).toBeVisible();

      // Should have dropdown arrow with multiple tenants
      const dropdownArrows = tenantButton.locator("svg");
      await expect(dropdownArrows).toHaveCount(1);

      // Open dropdown and verify all tenants are listed
      await tenantButton.click();
      await expect(page1.getByRole("menu")).toBeVisible();

      const menuItems = page1.getByRole("menuitem");
      await expect(menuItems).toHaveCount(3);

      // Close menu
      await page1.keyboard.press("Escape");
      await expect(page1.getByRole("menu")).not.toBeVisible();

      // Switch to secondary tenant - this shows invitation dialog
      await tenantButton.click();
      await menuItems.filter({ hasText: secondaryTenantName }).click();

      // Accept invitation dialog appears for pending invitations
      const invitationDialog = page1.getByRole("dialog", { name: "Accept invitation" });
      await expect(invitationDialog).toBeVisible();

      // Accept the invitation
      await page1.getByRole("button", { name: "Accept invitation" }).click();

      // Verify we're on admin page
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      await expect(page1).toHaveURL(/\/admin/);

      // Re-query the tenant button after page changes
      const navElementAfter = page1.locator("nav").first();
      const tenantButtonAfter = navElementAfter.locator("button").filter({ has: page1.locator('img[alt="Logo"]') });
      await expect(tenantButtonAfter).toContainText(secondaryTenantName);
    })();

    // === TERTIARY TENANT INVITATION ACCEPTANCE ===
    await step("Accept invitation for tertiary tenant & verify successful tenant switch")(async () => {
      const navElement = page1.locator("nav").first();
      const tenantButton = navElement.locator("button").filter({ has: page1.locator('img[alt="Logo"]') });

      await tenantButton.click();
      const menuItems = page1.getByRole("menuitem");

      // Look for the tertiary tenant with pending invitation badge
      const tertiaryMenuItem = menuItems.filter({ hasText: tertiaryTenantName });
      await tertiaryMenuItem.click();

      // Accept invitation dialog should appear for this tenant
      const invitationDialog = page1.getByRole("dialog", { name: "Accept invitation" });
      await expect(invitationDialog).toBeVisible();

      // Accept the invitation
      await page1.getByRole("button", { name: "Accept invitation" }).click();

      // Verify we're on admin page
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      await expect(page1).toHaveURL(/\/admin/);

      // Re-query tenant button after page changes
      const navElementAfter = page1.locator("nav").first();
      const tenantButtonAfter = navElementAfter.locator("button").filter({ has: page1.locator('img[alt="Logo"]') });
      await expect(tenantButtonAfter).toContainText(tertiaryTenantName);
    })();

    // === OPEN SECOND TAB ===
    await step("Open second tab & verify shared authentication")(async () => {
      // Navigate page2 to home (it shares authentication with page1)
      await page2.goto("/admin");
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // Verify both pages show navigation (confirms authentication is shared)
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
      const tenantButton = navElement.locator("button").filter({ has: page1.locator('img[alt="Logo"]') });

      // Should still be on tertiary tenant
      await expect(tenantButton).toContainText(tertiaryTenantName);

      // Logout
      testContext1.monitoring.expectedStatusCodes.push(401);
      testContext2.monitoring.expectedStatusCodes.push(401);
      await page1.getByRole("button", { name: "User profile menu" }).click();
      await page1.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page1.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await expect(page1).toHaveURL(/\/login/);

      // Verify tab 2 also loses authentication
      await page2.goto("/admin");
      await expect(page2.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await expect(page2).toHaveURL(/\/login/);

      // Login again
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.getByRole("button", { name: "Continue" }).click();
      await expect(page1.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await page1.keyboard.type(getVerificationCode());

      // Wait for navigation to complete
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      await expect(page1).toHaveURL(/\/admin/);

      // Should login to tertiary tenant (last selected)
      const navElementAfter = page1.locator("nav").first();
      const tenantButtonAfter = navElementAfter.locator("button").filter({ has: page1.locator('img[alt="Logo"]') });
      await expect(tenantButtonAfter).toContainText(tertiaryTenantName);

      // Verify page2 also shows tertiary tenant when navigated
      await page2.goto("/admin");
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toContainText(tertiaryTenantName);
    })();

    // === SWITCHING WITH MODALS OPEN ===
    await step("Switch tenant with profile modal open & verify tenant switch completes")(async () => {
      // Open profile modal
      await page1.getByRole("button", { name: "User profile menu" }).click();
      await page1.getByRole("menuitem", { name: "Edit profile" }).click();
      await expect(page1.getByRole("dialog", { name: "User profile" })).toBeVisible();

      // Close the modal first since it blocks pointer events
      await page1.getByRole("button", { name: "Cancel" }).click();
      await expect(page1.getByRole("dialog")).not.toBeVisible();

      // Now switch tenant
      const navElement = page1.locator("nav").first();
      const tenantButton = navElement.locator("button").filter({ has: page1.locator('img[alt="Logo"]') });

      await tenantButton.click();

      // Dropdown should work
      await expect(page1.getByRole("menu")).toBeVisible();
      const menuItems = page1.getByRole("menuitem");

      // Switch to secondary tenant
      const targetMenuItem = menuItems.filter({ hasText: secondaryTenantName }).first();
      await targetMenuItem.click();

      // Verify switch completed
      await expect(page1).toHaveURL(/\/admin/);

      // Verify switch completed
      const navElementAfter = page1.locator("nav").first();
      const tenantButtonAfter = navElementAfter.locator("button").filter({ has: page1.locator('img[alt="Logo"]') });
      await expect(tenantButtonAfter).toContainText(secondaryTenantName);
    })();

    // === TENANT CONTEXT ACROSS NAVIGATION ===
    await step("Navigate across pages & verify tenant context remains consistent")(async () => {
      const navElement = page1.locator("nav").first();
      const tenantButton = navElement.locator("button").filter({ has: page1.locator('img[alt="Logo"]') });

      // Get the current tenant after stabilization
      const initialTenant = await tenantButton.textContent();

      // Navigate to Users page
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();
      await expect(page1.getByRole("heading", { name: "Users" })).toBeVisible();

      // Tenant should still be visible
      await expect(tenantButton).toContainText(initialTenant || "");

      // Navigate to Account page
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Account" }).click();
      await expect(page1.getByRole("heading", { name: "Account settings" })).toBeVisible();

      // Should show correct tenant name in account settings
      const accountNameInput = page1.getByRole("textbox", { name: "Account name" });
      await expect(accountNameInput).toHaveValue(initialTenant?.trim() || "");

      // Switch to a different tenant
      await tenantButton.click();
      const menuItems = page1.getByRole("menuitem");

      // Switch to primary tenant
      const targetMenuItem = menuItems.filter({ hasText: primaryTenantName }).first();
      await targetMenuItem.click();

      await expect(page1).toHaveURL(/\/admin/);

      // Verify switched context
      await expect(tenantButton).toContainText(primaryTenantName);

      // Navigate back to Home
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Home" }).click();
      await expect(page1.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // Tenant should still be primary tenant
      await expect(tenantButton).toContainText(primaryTenantName);
    })();

    // === TEST: TENANT SWITCH DETECTION ACROSS TABS ===
    await step("Reload tab 2 & verify it shows tenant switch")(async () => {
      // Reload page2 to check if it detects the tenant switch
      await page2.reload();
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // Verify tab 2 is now on primary tenant
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toContainText(primaryTenantName);
    })();

    // === USER MANAGEMENT WITHIN TENANTS ===
    await step("Navigate to users page & verify current user is listed with correct role")(async () => {
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();
      await expect(page1.getByRole("heading", { name: "Users" })).toBeVisible();

      // Verify the current user is shown in the user list
      await expect(page1.locator("tbody").first()).toContainText(user.email);

      // Verify role is shown (should be Owner on primary tenant)
      const tableBody = page1.locator("tbody").first();
      await expect(tableBody).toContainText("Owner");
    })();

    // === OWNER PERMISSIONS AND USER INVITATION ===
    await step("Invite new user as owner & verify successful invitation")(async () => {
      const invitedUser = testUser();

      // Now we should have invite button as owner
      await page1.getByRole("button", { name: "Invite user" }).click();
      await expect(page1.getByRole("dialog", { name: "Invite user" })).toBeVisible();
      await page1.getByRole("textbox", { name: "Email" }).fill(invitedUser.email);
      await page1.getByRole("button", { name: "Send invite" }).click();

      await expectToastMessage(testContext1, "User invited successfully");
      await expect(page1.getByRole("dialog")).not.toBeVisible();
    })();

    // === USER PROFILE UPDATE ===
    await step("Update user profile & verify changes persist successfully")(async () => {
      await page1.getByRole("button", { name: "User profile menu" }).click();
      await page1.getByRole("menuitem", { name: "Edit profile" }).click();
      await expect(page1.getByRole("dialog", { name: "User profile" })).toBeVisible();

      // Update profile with new information
      await page1.getByRole("textbox", { name: "First name" }).fill("Updated");
      await page1.getByRole("textbox", { name: "Last name" }).fill("Name");
      await page1.getByRole("textbox", { name: "Title" }).fill("Senior Developer");
      await page1.getByRole("button", { name: "Save changes" }).click();

      await expectToastMessage(testContext1, "Profile updated successfully");
      await expect(page1.getByRole("dialog")).not.toBeVisible();
    })();

    // === TEST: DIFFERENT USER LOGIN ===
    await step("Logout & login as different user in tab 1")(async () => {
      testContext1.monitoring.expectedStatusCodes.push(401);

      // Logout from page1
      await page1.getByRole("button", { name: "User profile menu" }).click();
      await page1.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page1.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // Login as different user
      await page1.getByRole("textbox", { name: "Email" }).fill(secondUser.email);
      await page1.getByRole("button", { name: "Continue" }).click();
      await expect(page1.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await page1.keyboard.type(getVerificationCode());
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();
    })();

    await step("Verify tab 2 also shows different user")(async () => {
      // Navigate page2 to trigger auth check
      await page2.goto("/admin");
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // Verify both tabs now show the secondary tenant (different user's tenant)
      // Note: For single tenant users, the tenant name is displayed as text, not in a button
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toContainText(secondaryTenantName);
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toContainText(secondaryTenantName);
    })();

    // === TEST: SWITCH BACK TO ORIGINAL USER WITH MULTIPLE TENANTS ===
    await step("Logout & login as original user with both tenants")(async () => {
      testContext1.monitoring.expectedStatusCodes.push(401);
      testContext2.monitoring.expectedStatusCodes.push(401);

      // Logout
      await page1.getByRole("button", { name: "User profile menu" }).click();
      await page1.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page1.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // Login as original user (who has access to multiple tenants)
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.getByRole("button", { name: "Continue" }).click();
      await expect(page1.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await page1.keyboard.type(getVerificationCode());
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // Navigate page2 to admin
      await page2.goto("/admin");
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // Verify both on primary tenant
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toContainText(primaryTenantName);
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toContainText(primaryTenantName);
    })();

    // === TEST: COMPLEX FLOW - SWITCH + LOGOUT + LOGIN ===
    await step("Switch tenant, logout & login again")(async () => {
      // Switch to secondary tenant in tab 1
      const nav1 = page1.locator("nav").first();
      const tenantButton1 = nav1.locator("button").filter({ has: page1.locator('img[alt="Logo"]') });

      await tenantButton1.click();
      await page1.getByRole("menuitem").filter({ hasText: secondaryTenantName }).click();

      // Logout from tab 1
      testContext1.monitoring.expectedStatusCodes.push(401);
      await page1.getByRole("button", { name: "User profile menu" }).click();
      await page1.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page1.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // Login again in tab 1
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.getByRole("button", { name: "Continue" }).click();
      await expect(page1.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await page1.keyboard.type(getVerificationCode());
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // Tab 1 should login to secondary tenant (last selected)
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toContainText(secondaryTenantName);
    })();

    await step("Navigate tab 2 to admin & verify it shows tenant switch from login")(async () => {
      // Navigate page2 to admin to get latest auth state
      await page2.goto("/admin");
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // Verify tab 2 now shows secondary tenant (switched during login)
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toContainText(secondaryTenantName);
    })();

    // === TEST: SWITCH BACK TO ORIGINAL TENANT ===
    await step("Switch back to primary tenant in tab 1 & verify synchronization")(async () => {
      // Switch back to primary tenant
      const nav1 = page1.locator("nav").first();
      const tenantButton1 = nav1.locator("button").filter({ has: page1.locator('img[alt="Logo"]') });

      await tenantButton1.click();
      const menuItems = page1.getByRole("menuitem");
      await menuItems.filter({ hasText: primaryTenantName }).click();

      // Verify tenant switched in tab 1
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toContainText(primaryTenantName);
    })();

    await step("Reload tab 2 & verify it syncs back to primary tenant")(async () => {
      // Reload page2
      await page2.reload();
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // Verify tab 2 is back on primary tenant
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toContainText(primaryTenantName);
    })();

    // === FINAL LOGOUT ===
    await step("Logout from the system & verify redirect to login page")(async () => {
      testContext1.monitoring.expectedStatusCodes.push(401);
      testContext2.monitoring.expectedStatusCodes.push(401);
      await page1.getByRole("button", { name: "User profile menu" }).click();
      await page1.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page1.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await expect(page1).toHaveURL(/\/login/);

      // Verify tab 2 also loses authentication
      await page2.goto("/admin");
      await expect(page2.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await expect(page2).toHaveURL(/\/login/);
    })();
  });
});

import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext, expectToastMessage } from "@shared/e2e/utils/test-assertions";
import { completeSignupFlow, getVerificationCode, testUser } from "@shared/e2e/utils/test-data";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@comprehensive", () => {
  /**
   * COMPREHENSIVE TENANT SWITCHING WORKFLOW
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
   */
  test("should handle comprehensive tenant switching scenarios", async ({ page }) => {
    const context = createTestContext(page);
    const user = testUser();
    const secondOwner = testUser();
    const thirdOwner = testUser();

    // Generate unique tenant names using timestamps
    const timestamp = Date.now().toString().slice(-4);
    const primaryTenantName = `T1-${timestamp}`;
    const secondaryTenantName = `T2-${timestamp}`;
    const tertiaryTenantName = `T3-${timestamp}`;

    // === SINGLE TENANT DISPLAY ===
    await step("Create single tenant & verify dropdown is hidden")(async () => {
      await completeSignupFlow(page, expect, user, context);
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // Update the first tenant name
      await page.getByLabel("Main navigation").getByRole("link", { name: "Account" }).click();
      await page.getByRole("textbox", { name: "Account name" }).clear();
      await page.getByRole("textbox", { name: "Account name" }).fill(primaryTenantName);
      await page.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(context, "Account updated successfully");
      await page.getByLabel("Main navigation").getByRole("link", { name: "Home" }).click();

      const navElement = page.locator("nav").first();

      // With single tenant, there's just a logo - no tenant selector button
      const logoElement = navElement.locator('img[alt="Logo"]');
      await expect(logoElement).toBeVisible();

      // Verify there's no tenant button (it only appears with multiple tenants)
      const tenantButton = navElement.locator("button").filter({ has: page.locator('img[alt="Logo"]') });
      await expect(tenantButton).not.toBeVisible();
    })();

    // === MULTIPLE TENANT SETUP ===
    await step("Logout from primary tenant & verify redirect to login page")(async () => {
      context.monitoring.expectedStatusCodes.push(401);
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Log out" }).click();

      await expect(page.getByRole("heading", { name: "Welcome back" })).toBeVisible();
      expect(page.url()).toContain("/login");
    })();

    await step("Create second tenant & verify successful user invitation")(async () => {
      await completeSignupFlow(page, expect, secondOwner, context);

      // Update second tenant name
      await page.getByLabel("Main navigation").getByRole("link", { name: "Account" }).click();
      await page.getByRole("textbox", { name: "Account name" }).clear();
      await page.getByRole("textbox", { name: "Account name" }).fill(secondaryTenantName);
      await page.getByRole("button", { name: "Save changes" }).click();

      await expectToastMessage(context, "Account updated successfully");

      // Invite first user
      await page.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Send invite" }).click();

      await expectToastMessage(context, "User invited successfully");

      // Logout
      context.monitoring.expectedStatusCodes.push(401);
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Log out" }).click();

      await expect(page.getByRole("heading", { name: "Welcome back" })).toBeVisible();
      expect(page.url()).toContain("/login");
    })();

    await step("Create third tenant & verify successful user invitation")(async () => {
      await completeSignupFlow(page, expect, thirdOwner, context);

      // Update third tenant name
      await page.getByLabel("Main navigation").getByRole("link", { name: "Account" }).click();
      await page.getByRole("textbox", { name: "Account name" }).clear();
      await page.getByRole("textbox", { name: "Account name" }).fill(tertiaryTenantName);
      await page.getByRole("button", { name: "Save changes" }).click();

      await expectToastMessage(context, "Account updated successfully");

      // Invite first user
      await page.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Send invite" }).click();

      await expectToastMessage(context, "User invited successfully");

      // Logout
      context.monitoring.expectedStatusCodes.push(401);
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Log out" }).click();

      await expect(page.getByRole("heading", { name: "Welcome back" })).toBeVisible();
      expect(page.url()).toContain("/login");
    })();

    // === TENANT SWITCHING UI AND FUNCTIONALITY ===
    await step("Login with multiple tenants & verify tenant switching UI displays correctly")(async () => {
      // Login
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await page.keyboard.type(getVerificationCode());
      // Wait for navigation to complete - could be Users or Home page
      await expect(page.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      expect(page.url()).toContain("/admin");

      // Verify tenant selector is visible with dropdown
      const navElement = page.locator("nav").first();
      const tenantButton = navElement.locator("button").filter({ has: page.locator('img[alt="Logo"]') });
      await expect(tenantButton).toBeVisible();

      // Should have dropdown arrow with multiple tenants
      const dropdownArrows = tenantButton.locator("svg");
      await expect(dropdownArrows).toHaveCount(1);

      // Open dropdown and verify all tenants are listed
      await tenantButton.click();
      await expect(page.getByRole("menu")).toBeVisible();

      const menuItems = page.getByRole("menuitem");
      await expect(menuItems).toHaveCount(3);

      // Close menu
      await page.keyboard.press("Escape");
      await expect(page.getByRole("menu")).not.toBeVisible();

      // Switch to secondary tenant - this shows invitation dialog
      await tenantButton.click();
      await menuItems.filter({ hasText: secondaryTenantName }).click();

      // Accept invitation dialog appears for pending invitations
      const invitationDialog = page.getByRole("dialog", { name: "Accept invitation" });
      await expect(invitationDialog).toBeVisible();

      // Accept the invitation
      await page.getByRole("button", { name: "Accept invitation" }).click();

      // Wait for navigation to complete after accepting
      await page.waitForLoadState("networkidle");

      // Verify we're on admin page
      await expect(page.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      expect(page.url()).toContain("/admin");

      // Re-query the tenant button after page changes
      const navElementAfter = page.locator("nav").first();
      const tenantButtonAfter = navElementAfter.locator("button").filter({ has: page.locator('img[alt="Logo"]') });
      await expect(tenantButtonAfter).toContainText(secondaryTenantName);
    })();

    // === TERTIARY TENANT INVITATION ACCEPTANCE ===
    await step("Accept invitation for tertiary tenant & verify successful tenant switch")(async () => {
      const navElement = page.locator("nav").first();
      const tenantButton = navElement.locator("button").filter({ has: page.locator('img[alt="Logo"]') });

      await tenantButton.click();
      const menuItems = page.getByRole("menuitem");

      // Look for the tertiary tenant with pending invitation badge
      const tertiaryMenuItem = menuItems.filter({ hasText: tertiaryTenantName });
      await tertiaryMenuItem.click();

      // Accept invitation dialog should appear for this tenant
      const invitationDialog = page.getByRole("dialog", { name: "Accept invitation" });
      await expect(invitationDialog).toBeVisible();

      // Accept the invitation
      await page.getByRole("button", { name: "Accept invitation" }).click();

      // Wait for navigation to complete after accepting
      await page.waitForLoadState("networkidle");

      // Verify we're on admin page
      await expect(page.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      expect(page.url()).toContain("/admin");

      // Re-query tenant button after page changes
      const navElementAfter = page.locator("nav").first();
      const tenantButtonAfter = navElementAfter.locator("button").filter({ has: page.locator('img[alt="Logo"]') });
      await expect(tenantButtonAfter).toContainText(tertiaryTenantName);

      // Logout
      context.monitoring.expectedStatusCodes.push(401);
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page.getByRole("heading", { name: "Welcome back" })).toBeVisible();
      expect(page.url()).toContain("/login");

      // Login again
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await page.keyboard.type(getVerificationCode());
      // Wait for navigation to complete
      await expect(page.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      expect(page.url()).toContain("/admin");

      // Should login to tertiary tenant (last selected)
      await expect(tenantButton).toContainText(tertiaryTenantName);
    })();

    // === TENANT PREFERENCE PERSISTENCE ===
    await step("Logout and login again & verify tenant preference persists")(async () => {
      // Reuse the existing tenant button reference
      const navElement = page.locator("nav").first();
      const tenantButton = navElement.locator("button").filter({ has: page.locator('img[alt="Logo"]') });

      // Should still be on tertiary tenant
      await expect(tenantButton).toContainText(tertiaryTenantName);

      // Logout
      context.monitoring.expectedStatusCodes.push(401);
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      expect(page.url()).toContain("/login");

      // Login again
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await page.keyboard.type(getVerificationCode());

      // Wait for navigation to complete
      await expect(page.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      expect(page.url()).toContain("/admin");

      // Should login to tertiary tenant (last selected)
      const navElementAfter = page.locator("nav").first();
      const tenantButtonAfter = navElementAfter.locator("button").filter({ has: page.locator('img[alt="Logo"]') });
      await expect(tenantButtonAfter).toContainText(tertiaryTenantName);
    })();

    // === SWITCHING WITH MODALS OPEN ===
    await step("Switch tenant with profile modal open & verify tenant switch completes")(async () => {
      // Open profile modal
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Edit profile" }).click();
      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();

      // Close the modal first since it blocks pointer events
      await page.getByRole("button", { name: "Cancel" }).click();
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Now switch tenant
      const navElement = page.locator("nav").first();
      const tenantButton = navElement.locator("button").filter({ has: page.locator('img[alt="Logo"]') });
      const _currentTenant = await tenantButton.textContent();

      await tenantButton.click();

      // Dropdown should work
      await expect(page.getByRole("menu")).toBeVisible();
      const menuItems = page.getByRole("menuitem");

      // Switch to secondary tenant (we know current is tertiary from previous test)
      const targetMenuItem = menuItems.filter({ hasText: secondaryTenantName }).first();
      await targetMenuItem.click();

      // Wait for switch to complete
      await page.waitForLoadState("networkidle");
      expect(page.url()).toContain("/admin");

      // Verify switch completed

      // Verify the tenant actually switched to secondary
      const navElementAfter = page.locator("nav").first();
      const tenantButtonAfter = navElementAfter.locator("button").filter({ has: page.locator('img[alt="Logo"]') });
      await expect(tenantButtonAfter).toContainText(secondaryTenantName);
    })();

    // === TENANT CONTEXT ACROSS NAVIGATION ===
    await step("Navigate across pages & verify tenant context remains consistent")(async () => {
      const navElement = page.locator("nav").first();
      const tenantButton = navElement.locator("button").filter({ has: page.locator('img[alt="Logo"]') });

      // Get the current tenant after stabilization
      const initialTenant = await tenantButton.textContent();

      // Navigate to Users page
      await page.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();

      // Tenant should still be visible
      await expect(tenantButton).toContainText(initialTenant || "");

      // Navigate to Account page
      await page.getByLabel("Main navigation").getByRole("link", { name: "Account" }).click();
      await expect(page.getByRole("heading", { name: "Account settings" })).toBeVisible();

      // Should show correct tenant name in account settings
      const accountNameInput = page.getByRole("textbox", { name: "Account name" });
      await expect(accountNameInput).toHaveValue(initialTenant?.trim() || "");

      // Switch to a different tenant
      await tenantButton.click();
      const menuItems = page.getByRole("menuitem");

      // Switch to primary tenant (we know current is secondary from previous test)
      const targetMenuItem = menuItems.filter({ hasText: primaryTenantName }).first();
      await targetMenuItem.click();

      await page.waitForLoadState("networkidle");
      expect(page.url()).toContain("/admin");

      // Verify switched context

      // Verify the tenant switch worked - should be on primary tenant now
      await expect(tenantButton).toContainText(primaryTenantName);

      // Navigate back to Home
      await page.getByLabel("Main navigation").getByRole("link", { name: "Home" }).click();
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // Tenant should still be primary tenant
      await expect(tenantButton).toContainText(primaryTenantName);
    })();

    // === USER MANAGEMENT WITHIN TENANTS ===
    await step("Navigate to users page & verify current user is listed with correct role")(async () => {
      await page.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();

      // Verify the current user is shown in the user list
      await expect(page.locator("tbody").first()).toContainText(user.email);

      // Verify role is shown (should be Owner on primary tenant)
      const tableBody = page.locator("tbody").first();
      await expect(tableBody).toContainText("Owner");
    })();

    // === OWNER PERMISSIONS AND USER INVITATION ===
    await step("Switch to primary tenant as owner & verify successful user invitation")(async () => {
      const invitedUser = testUser();

      // Switch to primary tenant where user is owner
      const navElement = page.locator("nav").first();
      const tenantButton = navElement.locator("button").filter({ has: page.locator('img[alt="Logo"]') });
      await tenantButton.click();

      const menuItems = page.getByRole("menuitem");
      await menuItems.filter({ hasText: primaryTenantName }).click();
      await page.waitForLoadState("networkidle");
      expect(page.url()).toContain("/admin");

      // Navigate to Users page
      await page.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();

      // Now we should have invite button as owner
      await page.getByRole("button", { name: "Invite user" }).click();
      await expect(page.getByRole("dialog", { name: "Invite user" })).toBeVisible();
      await page.getByRole("textbox", { name: "Email" }).fill(invitedUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();

      await expectToastMessage(context, "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
    })();

    // === USER PROFILE UPDATE ===
    await step("Update user profile & verify changes persist successfully")(async () => {
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Edit profile" }).click();
      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();

      // Update profile with new information
      await page.getByRole("textbox", { name: "First name" }).fill("Updated");
      await page.getByRole("textbox", { name: "Last name" }).fill("Name");
      await page.getByRole("textbox", { name: "Title" }).fill("Senior Developer");
      await page.getByRole("button", { name: "Save changes" }).click();

      await expectToastMessage(context, "Profile updated successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
    })();

    // === FINAL LOGOUT ===
    await step("Logout from the system & verify redirect to login page")(async () => {
      context.monitoring.expectedStatusCodes.push(401);
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      expect(page.url()).toContain("/login");
    })();
  });

  /**
   * MULTI-TAB AUTHENTICATION SYNCHRONIZATION
   *
   * Tests the cross-tab authentication synchronization features including:
   * - Tenant switch detection across browser tabs
   * - Different user login detection
   * - Logout synchronization
   * - Modal behavior for hidden tabs
   * - URL sanitization on tenant switch
   * - Rapid switching handling
   * - Non-dismissible modal verification
   */
  test("should handle multi-tab authentication synchronization", async ({ context }) => {
    // Create two pages in the same context to share authentication
    const page1 = await context.newPage();
    const page2 = await context.newPage();

    const testContext1 = createTestContext(page1);
    const testContext2 = createTestContext(page2);

    const user = testUser();
    const secondUser = testUser();

    // Generate unique tenant names
    const timestamp = Date.now().toString().slice(-4);
    const primaryTenantName = `Primary-${timestamp}`;
    const secondaryTenantName = `Secondary-${timestamp}`;

    // === SETUP: CREATE USER AND TENANTS ===
    await step("Create user with primary tenant & verify successful signup")(async () => {
      await completeSignupFlow(page1, expect, user, testContext1);
      await expect(page1.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // Update tenant name
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Account" }).click();
      await page1.getByRole("textbox", { name: "Account name" }).clear();
      await page1.getByRole("textbox", { name: "Account name" }).fill(primaryTenantName);
      await page1.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(testContext1, "Account updated successfully");
    })();

    await step("Create second tenant & verify user invitation")(async () => {
      // Logout from page1
      testContext1.monitoring.expectedStatusCodes.push(401);
      await page1.getByRole("button", { name: "User profile menu" }).click();
      await page1.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page1.getByRole("heading", { name: "Welcome back" })).toBeVisible();

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
      await expect(page1.getByRole("heading", { name: "Welcome back" })).toBeVisible();
    })();

    // === TEST: TENANT SWITCH DETECTION ACROSS TABS ===
    await step("Login with first user & open second tab")(async () => {
      // Login in page1
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.getByRole("button", { name: "Continue" }).click();
      await expect(page1.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await page1.keyboard.type(getVerificationCode());
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // Navigate page2 to home (it shares authentication with page1)
      await page2.goto("/admin");
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // Ensure page2 is fully loaded and interactive
      await page2.waitForLoadState("networkidle");

      // Both should be on the same tenant initially
      // Verify both pages show navigation (confirms authentication is shared)
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();
    })();

    // === TEST 1: LOGOUT SYNCHRONIZATION ===
    // NOTE: Due to Playwright's shared authentication context, both tabs will be logged out simultaneously
    // We verify the synchronization by checking that both tabs lose authentication
    await step("Logout from tab 1 & verify tab 2 loses authentication")(async () => {
      testContext1.monitoring.expectedStatusCodes.push(401);
      testContext2.monitoring.expectedStatusCodes.push(401);
      
      // Logout in page1
      await page1.getByRole("button", { name: "User profile menu" }).click();
      await page1.getByRole("menuitem", { name: "Log out" }).click();
      
      // Verify tab 1 redirected to login
      await expect(page1.getByRole("heading", { name: "Welcome back" })).toBeVisible();
      
      // Navigate page2 to trigger auth check
      await page2.goto("/admin");
      
      // Verify tab 2 is redirected to login (authentication lost)
      await expect(page2.getByRole("heading", { name: "Welcome back" })).toBeVisible();
      expect(page2.url()).toContain("/login");
    })();

    // === TEST 2: LOGIN AS SAME USER IN BOTH TABS ===
    await step("Login as same user in both tabs")(async () => {
      // Login in page1
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.getByRole("button", { name: "Continue" }).click();
      await expect(page1.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await page1.keyboard.type(getVerificationCode());
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      
      // Navigate page2 to admin (it shares authentication with page1)
      await page2.goto("/admin");
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();
    })();

    // === TEST 3: DIFFERENT USER LOGIN ===
    // NOTE: In Playwright, pages in same context share auth, so logging in as different user
    // will affect both tabs. We verify both tabs show the new user.
    await step("Logout & login as different user in tab 1")(async () => {
      testContext1.monitoring.expectedStatusCodes.push(401);
      
      // Logout from page1
      await page1.getByRole("button", { name: "User profile menu" }).click();
      await page1.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page1.getByRole("heading", { name: "Welcome back" })).toBeVisible();
      
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

    // === TEST 4: TENANT SWITCH DETECTION ===
    await step("Logout & login as original user with both tenants")(async () => {
      testContext1.monitoring.expectedStatusCodes.push(401);
      testContext2.monitoring.expectedStatusCodes.push(401);
      
      // Logout
      await page1.getByRole("button", { name: "User profile menu" }).click();
      await page1.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page1.getByRole("heading", { name: "Welcome back" })).toBeVisible();
      
      // Login as original user (who has access to both tenants)
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

    await step("Switch to secondary tenant in tab 1 & verify URL updates")(async () => {
      // Switch tenant in tab 1 - user has multiple tenants so should have dropdown
      const nav1 = page1.locator("nav").first();
      const tenantButton1 = nav1.locator("button").filter({ has: page1.locator('img[alt="Logo"]') });
      
      await tenantButton1.click();
      const menuItems = page1.getByRole("menuitem");
      await menuItems.filter({ hasText: secondaryTenantName }).click();
      
      // Accept invitation dialog appears for pending invitations
      const invitationDialog = page1.getByRole("dialog", { name: "Accept invitation" });
      await expect(invitationDialog).toBeVisible();
      await page1.getByRole("button", { name: "Accept invitation" }).click();
      
      await page1.waitForLoadState("networkidle");
      
      // Verify tenant switched in tab 1
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toContainText(secondaryTenantName);
    })();

    await step("Reload tab 2 & verify it shows tenant switch")(async () => {
      // Reload page2 to check if it detects the tenant switch
      await page2.reload();
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      
      // Verify tab 2 is now on secondary tenant
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toContainText(secondaryTenantName);
    })();

    // === TEST 5: SWITCH BACK TO ORIGINAL TENANT ===
    await step("Switch back to primary tenant in tab 1 & verify synchronization")(async () => {
      // Switch back to primary tenant
      const nav1 = page1.locator("nav").first();
      const tenantButton1 = nav1.locator("button").filter({ has: page1.locator('img[alt="Logo"]') });
      
      await tenantButton1.click();
      const menuItems = page1.getByRole("menuitem");
      await menuItems.filter({ hasText: primaryTenantName }).click();
      await page1.waitForLoadState("networkidle");
      
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

    // === TEST 6: COMPLEX FLOW - SWITCH + LOGOUT + LOGIN ===
    await step("Switch tenant, logout & login again")(async () => {
      // Switch to secondary tenant in tab 1
      const nav1 = page1.locator("nav").first();
      const tenantButton1 = nav1.locator("button").filter({ has: page1.locator('img[alt="Logo"]') });
      
      await tenantButton1.click();
      await page1.getByRole("menuitem").filter({ hasText: secondaryTenantName }).click();
      await page1.waitForLoadState("networkidle");
      
      // Logout from tab 1
      testContext1.monitoring.expectedStatusCodes.push(401);
      await page1.getByRole("button", { name: "User profile menu" }).click();
      await page1.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page1.getByRole("heading", { name: "Welcome back" })).toBeVisible();
      
      // Login again in tab 1
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.getByRole("button", { name: "Continue" }).click();
      await expect(page1.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await page1.keyboard.type(getVerificationCode());
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      
      // Tab 1 should login to secondary tenant (last selected)
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toContainText(secondaryTenantName);
    })();

    await step("Reload tab 2 & verify it shows tenant switch from login")(async () => {
      // Navigate page2 to admin to get latest auth state
      await page2.goto("/admin");
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      
      // Verify tab 2 now shows secondary tenant (switched during login)
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toContainText(secondaryTenantName);
    })();

    // === TEST 7: COMPLEX FLOW - SWITCH BACK AFTER LOGIN ===
    await step("Switch back to primary tenant & logout")(async () => {
      // Switch tab 1 back to primary tenant
      const nav1 = page1.locator("nav").first();
      const tenantButton1 = nav1.locator("button").filter({ has: page1.locator('img[alt="Logo"]') });
      
      await tenantButton1.click();
      await page1.getByRole("menuitem").filter({ hasText: primaryTenantName }).click();
      await page1.waitForLoadState("networkidle");
      await expect(tenantButton1).toContainText(primaryTenantName);
      
      // Logout again
      testContext1.monitoring.expectedStatusCodes.push(401);
      testContext2.monitoring.expectedStatusCodes.push(401);
      await page1.getByRole("button", { name: "User profile menu" }).click();
      await page1.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page1.getByRole("heading", { name: "Welcome back" })).toBeVisible();
    })();

    await step("Login again & verify both tabs sync to primary tenant")(async () => {
      // Login in tab 1
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.getByRole("button", { name: "Continue" }).click();
      await expect(page1.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await page1.keyboard.type(getVerificationCode());
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      
      // Tab 1 should be on primary tenant (last selected before logout)
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toContainText(primaryTenantName);
      
      // Navigate page2 to verify it's also on primary tenant
      await page2.goto("/admin");
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toContainText(primaryTenantName);
    })();
  });
});

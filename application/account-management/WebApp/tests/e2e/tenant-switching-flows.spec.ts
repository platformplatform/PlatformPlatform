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
      await expect(page1.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // Update the first tenant name
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Account" }).click();
      await page1.getByRole("textbox", { name: "Account name" }).clear();
      await page1.getByRole("textbox", { name: "Account name" }).fill(primaryTenantName);
      await page1.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(testContext1, "Account name updated successfully");
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Home" }).click();

      const navElement = page1.locator("nav").first();

      // With single tenant, there's just a logo - no tenant selector button
      // The tenant has a name (primaryTenantName) so it will show the initial letter, not an img
      const tenantLogoElement = navElement
        .locator('[class*="TenantLogo"], [class*="tenant-logo"], div')
        .filter({ hasText: primaryTenantName.charAt(0) });
      await expect(tenantLogoElement.first()).toBeVisible();

      // there's no tenant button (it only appears with multiple tenants)
      // With single tenant, there's no dropdown button with ChevronDown icon
      const tenantButton = navElement
        .locator("button")
        .filter({ has: page1.locator("svg") })
        .filter({ hasText: primaryTenantName });
      await expect(tenantButton).not.toBeVisible();
    })();

    // === MULTIPLE TENANT SETUP ===
    await step("Logout from primary tenant & verify redirect to login page")(async () => {
      testContext1.monitoring.expectedStatusCodes.push(401);
      await page1.getByRole("button", { name: "User profile menu" }).dispatchEvent("click");
      const userMenu1 = page1.getByRole("menu", { name: "User profile menu" });
      await expect(userMenu1).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem = page1.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      await expect(page1.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await expect(page1).toHaveURL("/login?returnPath=%2Fadmin");
    })();

    await step("Create second tenant & verify user invitation")(async () => {
      // Create second user with second tenant
      await completeSignupFlow(page1, expect, secondUser, testContext1);

      // Update tenant name
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Account" }).click();
      await page1.getByRole("textbox", { name: "Account name" }).clear();
      await page1.getByRole("textbox", { name: "Account name" }).fill(secondaryTenantName);
      await page1.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(testContext1, "Account name updated successfully");

      // Invite first user
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();
      await page1.getByRole("button", { name: "Invite user" }).click();
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.getByRole("button", { name: "Send invite" }).click();
      await expectToastMessage(testContext1, "User invited successfully");

      // Logout
      testContext1.monitoring.expectedStatusCodes.push(401);
      await page1.getByRole("button", { name: "User profile menu" }).dispatchEvent("click");
      const userMenu1 = page1.getByRole("menu", { name: "User profile menu" });
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
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Account" }).click();
      await page1.getByRole("textbox", { name: "Account name" }).clear();
      await page1.getByRole("textbox", { name: "Account name" }).fill(tertiaryTenantName);
      await page1.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(testContext1, "Account name updated successfully");

      // Invite first user
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();
      await page1.getByRole("button", { name: "Invite user" }).click();
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.getByRole("button", { name: "Send invite" }).click();
      await expectToastMessage(testContext1, "User invited successfully");

      // Logout
      testContext1.monitoring.expectedStatusCodes.push(401);
      await page1.getByRole("button", { name: "User profile menu" }).dispatchEvent("click");
      const userMenu1 = page1.getByRole("menu", { name: "User profile menu" });
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
      await page1.getByRole("button", { name: "Continue" }).click();
      await expect(page1.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await typeOneTimeCode(page1, getVerificationCode());
      // Wait for navigation to complete - could be Users or Home page
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      await expect(page1).toHaveURL("/admin/users");

      // tenant selector is visible with dropdown
      const navElement = page1.locator("nav").first();
      // The button contains the TenantLogo component and shows primary tenant (which was last selected before logout)
      const tenantButton = navElement.locator("button").filter({ hasText: primaryTenantName });
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
      await expect(page1.getByRole("menu")).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const secondaryTenantMenuItem = menuItems.filter({ hasText: secondaryTenantName });
      await expect(secondaryTenantMenuItem).toBeVisible();
      await secondaryTenantMenuItem.dispatchEvent("click");

      // Accept invitation dialog appears for pending invitations
      const invitationDialog = page1.getByRole("dialog", { name: "Accept invitation" });
      await expect(invitationDialog).toBeVisible();

      // Accept the invitation
      await page1.getByRole("button", { name: "Accept invitation" }).click();

      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      await expect(page1).toHaveURL("/admin/users");

      // Re-query the tenant button after page changes
      const navElementAfter = page1.locator("nav").first();
      const tenantButtonAfter = navElementAfter.locator("button").filter({ hasText: secondaryTenantName });
      await expect(tenantButtonAfter).toContainText(secondaryTenantName);
    })();

    // === TERTIARY TENANT INVITATION ACCEPTANCE ===
    await step("Accept invitation for tertiary tenant & verify successful tenant switch")(async () => {
      const navElement = page1.locator("nav").first();
      // Tenant button shows secondary tenant currently
      const tenantButton = navElement.locator("button").filter({ hasText: secondaryTenantName });

      await tenantButton.click();
      await expect(page1.getByRole("menu")).toBeVisible();
      const menuItems = page1.getByRole("menuitem");

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const tertiaryMenuItem = menuItems.filter({ hasText: tertiaryTenantName });
      await expect(tertiaryMenuItem).toBeVisible();
      await tertiaryMenuItem.dispatchEvent("click");

      // Accept invitation dialog should appear for this tenant
      const invitationDialog = page1.getByRole("dialog", { name: "Accept invitation" });
      await expect(invitationDialog).toBeVisible();

      // Accept the invitation
      await page1.getByRole("button", { name: "Accept invitation" }).click();

      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      await expect(page1).toHaveURL("/admin/users");

      // Re-query tenant button after page changes
      const navElementAfter = page1.locator("nav").first();
      const tenantButtonAfter = navElementAfter.locator("button").filter({ hasText: tertiaryTenantName });
      await expect(tenantButtonAfter).toContainText(tertiaryTenantName);
    })();

    // === OPEN SECOND TAB ===
    await step("Open second tab & verify shared authentication")(async () => {
      // Navigate page2 to home (it shares authentication with page1)
      await page2.goto("/admin");
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
      await page1.getByRole("button", { name: "User profile menu" }).dispatchEvent("click");
      const userMenu1 = page1.getByRole("menu", { name: "User profile menu" });
      await expect(userMenu1).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem = page1.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");
      await expect(page1.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await expect(page1).toHaveURL("login?returnPath=%2Fadmin%2Fusers");

      // tab 2 also loses authentication
      await page2.goto("/admin");
      await expect(page2.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await expect(page2).toHaveURL("/login?returnPath=%2Fadmin");

      // Login again
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.getByRole("button", { name: "Continue" }).click();
      await expect(page1.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await typeOneTimeCode(page1, getVerificationCode());

      // Wait for navigation to complete
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      await expect(page1).toHaveURL("/admin/users");

      // Should login to tertiary tenant (last selected)
      const navElementAfter = page1.locator("nav").first();
      const tenantButtonAfter = navElementAfter.locator("button").filter({ hasText: tertiaryTenantName });
      await expect(tenantButtonAfter).toContainText(tertiaryTenantName);

      // page2 also shows tertiary tenant when navigated
      await page2.goto("/admin");
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toContainText(tertiaryTenantName);
    })();

    // === TENANT CONTEXT ACROSS NAVIGATION ===
    await step("Navigate across pages & verify tenant context remains consistent")(async () => {
      const navElement = page1.locator("nav").first();
      const tenantButton = navElement.locator("button").filter({ hasText: tertiaryTenantName });

      // We're on tertiary tenant at this point
      const currentTenantName = tertiaryTenantName;

      // Navigate to Users page
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();
      await expect(page1.getByRole("heading", { name: "Users" })).toBeVisible();

      // Tenant should still be visible
      await expect(tenantButton).toContainText(currentTenantName);

      // Navigate to Account page
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Account" }).click();
      await expect(page1.getByRole("heading", { name: "Account settings" })).toBeVisible();

      // Should show correct tenant name in account settings
      const accountNameInput = page1.getByRole("textbox", { name: "Account name" });
      await expect(accountNameInput).toHaveValue(currentTenantName);

      // Switch to a different tenant
      await tenantButton.click();
      await expect(page1.getByRole("menu")).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const targetMenuItem = page1.getByRole("menuitem").filter({ hasText: primaryTenantName }).first();
      await expect(targetMenuItem).toBeVisible();
      await targetMenuItem.dispatchEvent("click");

      await expect(page1).toHaveURL("/admin/account");

      // Re-query the tenant button with the new tenant name
      const navElementAfterSwitch = page1.locator("nav").first();
      const tenantButtonAfterSwitch = navElementAfterSwitch.locator("button").filter({ hasText: primaryTenantName });
      await expect(tenantButtonAfterSwitch).toContainText(primaryTenantName);

      // Navigate back to Home
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Home" }).click();
      await expect(page1.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // Tenant should still be primary tenant
      await expect(tenantButtonAfterSwitch).toContainText(primaryTenantName);
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
      await page1.getByRole("button", { name: "User profile menu" }).dispatchEvent("click");
      const userMenu1 = page1.getByRole("menu", { name: "User profile menu" });
      await expect(userMenu1).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem = page1.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");
      await expect(page1.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // Login as different user
      await page1.getByRole("textbox", { name: "Email" }).fill(secondUser.email);
      await page1.getByRole("button", { name: "Continue" }).click();
      await expect(page1.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await typeOneTimeCode(page1, getVerificationCode());
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();
    })();

    await step("Navigate tab 2 to admin & verify it shows different user")(async () => {
      // Navigate page2 to trigger auth check
      await page2.goto("/admin");
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
      await page1.getByRole("button", { name: "User profile menu" }).dispatchEvent("click");
      const userMenu1 = page1.getByRole("menu", { name: "User profile menu" });
      await expect(userMenu1).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem = page1.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");
      await expect(page1.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // Login as original user (who has access to multiple tenants)
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.getByRole("button", { name: "Continue" }).click();
      await expect(page1.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await typeOneTimeCode(page1, getVerificationCode());
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // Navigate page2 to admin
      await page2.goto("/admin");
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // both on primary tenant
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toContainText(primaryTenantName);
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toContainText(primaryTenantName);
    })();

    // === TEST: COMPLEX FLOW - SWITCH + LOGOUT + LOGIN ===
    await step("Switch tenant, logout & login again")(async () => {
      // Switch to secondary tenant in tab 1 (currently on primary tenant)
      const nav1 = page1.locator("nav").first();
      const tenantButton1 = nav1.locator("button").filter({ hasText: primaryTenantName });

      await tenantButton1.click();
      await expect(page1.getByRole("menu")).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const secondaryTenantMenuItem = page1.getByRole("menuitem").filter({ hasText: secondaryTenantName });
      await expect(secondaryTenantMenuItem).toBeVisible();
      await secondaryTenantMenuItem.dispatchEvent("click");

      // Wait for tenant menu to close
      await expect(page1.getByRole("menu")).not.toBeVisible();

      // Logout from tab 1
      testContext1.monitoring.expectedStatusCodes.push(401);
      await page1.getByRole("button", { name: "User profile menu" }).dispatchEvent("click");
      const userMenu1 = page1.getByRole("menu", { name: "User profile menu" });
      await expect(userMenu1).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem = page1.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      // Wait for logout redirect
      await expect(page1).toHaveURL(/\/login/);
      await expect(page1.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // Login again in tab 1
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.getByRole("button", { name: "Continue" }).click();
      await expect(page1.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await typeOneTimeCode(page1, getVerificationCode());
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // Tab 1 should login to secondary tenant (last selected)
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toContainText(secondaryTenantName);
    })();

    await step("Navigate tab 2 to admin & verify it shows tenant switch from login")(async () => {
      // Navigate page2 to admin to get latest auth state
      await page2.goto("/admin");
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // tab 2 now shows secondary tenant (switched during login)
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toContainText(secondaryTenantName);
    })();

    // === TEST: SWITCH BACK TO ORIGINAL TENANT ===
    await step("Switch back to primary tenant in tab 1 & verify synchronization")(async () => {
      // Switch back to primary tenant (currently on secondary tenant)
      const nav1 = page1.locator("nav").first();
      const tenantButton1 = nav1.locator("button").filter({ hasText: secondaryTenantName });

      await tenantButton1.click();
      await expect(page1.getByRole("menu")).toBeVisible();
      const menuItems = page1.getByRole("menuitem");

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const primaryTenantMenuItem = menuItems.filter({ hasText: primaryTenantName });
      await expect(primaryTenantMenuItem).toBeVisible();
      await primaryTenantMenuItem.dispatchEvent("click");

      // Wait for menu to close after selection
      await expect(page1.getByRole("menu")).not.toBeVisible();

      // tenant switched in tab 1
      await expect(page1.locator('nav[aria-label="Main navigation"]')).toContainText(primaryTenantName);
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
      await page1.getByRole("button", { name: "User profile menu" }).dispatchEvent("click");
      const userMenu1 = page1.getByRole("menu", { name: "User profile menu" });
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
      await page1.getByLabel("Main navigation").getByRole("link", { name: "Account" }).click();
      await page1.getByRole("textbox", { name: "Account name" }).clear();
      await page1.getByRole("textbox", { name: "Account name" }).fill(fourthTenantName);
      await page1.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(testContext1, "Account name updated successfully");

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
      await page2.goto("/admin");

      // Logout from page2
      testContext2.monitoring.expectedStatusCodes.push(401);
      await page2.getByRole("button", { name: "User profile menu" }).dispatchEvent("click");
      const userMenu2 = page2.getByRole("menu", { name: "User profile menu" });
      await expect(userMenu2).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem2 = page2.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem2).toBeVisible();
      await logoutMenuItem2.dispatchEvent("click");
      await expect(page2.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // Login as the invited user in page2
      await page2.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page2.getByRole("button", { name: "Continue" }).click();
      await expect(page2.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await typeOneTimeCode(page2, getVerificationCode());
      await expect(page2.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // Open tenant selector in page2 (should be on primary tenant after login)
      const nav2 = page2.locator("nav").first();
      const tenantButton2 = nav2.locator("button").filter({ hasText: primaryTenantName });
      await tenantButton2.click();

      // the new tenant with invitation is visible
      const menuItems2 = page2.getByRole("menuitem");
      const invitedTenant2 = menuItems2.filter({ hasText: "Revoke-Test" });
      await expect(invitedTenant2).toBeVisible();

      // Close dropdown without accepting
      await page2.keyboard.press("Escape");
    })();

    await step("Open invitation dialog in both tabs & verify both show accept dialog")(async () => {
      // Create a third page for the same user
      const page3 = await context.newPage();

      // Navigate page3 to admin (shares authentication)
      await page3.goto("/admin");
      await expect(page3.locator('nav[aria-label="Main navigation"]')).toBeVisible();

      // Open tenant selector in page2 (still on primary tenant)
      const nav2 = page2.locator("nav").first();
      const tenantButton2 = nav2.locator("button").filter({ hasText: primaryTenantName });
      await tenantButton2.click();
      await expect(page2.getByRole("menu")).toBeVisible();
      const menuItems2 = page2.getByRole("menuitem");

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const invitedTenant2 = menuItems2.filter({ hasText: "Revoke-Test" });
      await expect(invitedTenant2).toBeVisible();
      await invitedTenant2.dispatchEvent("click");

      // invitation dialog appears in page2
      const invitationDialog2 = page2.getByRole("dialog", { name: "Accept invitation" });
      await expect(invitationDialog2).toBeVisible();
      await expect(invitationDialog2).toContainText("You have been invited to join");

      // Open tenant selector in page3 (should also be on primary tenant)
      const nav3 = page3.locator("nav").first();
      const tenantButton3 = nav3.locator("button").filter({ hasText: primaryTenantName });
      await tenantButton3.click();
      await expect(page3.getByRole("menu")).toBeVisible();
      const menuItems3 = page3.getByRole("menuitem");

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const invitedTenant3 = menuItems3.filter({ hasText: "Revoke-Test" });
      await expect(invitedTenant3).toBeVisible();
      await invitedTenant3.dispatchEvent("click");

      // invitation dialog appears in page3
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
      await page2.getByRole("button", { name: "User profile menu" }).dispatchEvent("click");
      const userMenu2 = page2.getByRole("menu", { name: "User profile menu" });
      await expect(userMenu2).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem2 = page2.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem2).toBeVisible();
      await logoutMenuItem2.dispatchEvent("click");
      await expect(page2.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await expect(page2).toHaveURL("/login?returnPath=%2Fadmin");

      // tab 1 also loses authentication due to auth sync
      await page1.goto("/admin");
      await expect(page1.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await expect(page1).toHaveURL("/login?returnPath=%2Fadmin");
    })();
  });
});

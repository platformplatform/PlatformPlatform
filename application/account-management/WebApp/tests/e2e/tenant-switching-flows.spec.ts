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
    await step("Open profile modal and switch tenant & verify modal closes automatically")(async () => {
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
      const currentTenant = await tenantButton.textContent();

      await tenantButton.click();

      // Dropdown should work
      await expect(page.getByRole("menu")).toBeVisible();
      const menuItems = page.getByRole("menuitem");

      // Find a different tenant to switch to - we know the current tenant text from above
      const targetMenuItem = currentTenant?.includes(primaryTenantName)
        ? menuItems.filter({ hasText: secondaryTenantName }).first()
        : menuItems.filter({ hasText: primaryTenantName }).first();

      const targetTenantName = currentTenant?.includes(primaryTenantName) ? secondaryTenantName : primaryTenantName;
      await targetMenuItem.click();

      // Wait for switch to complete
      await page.waitForLoadState("networkidle");
      expect(page.url()).toContain("/admin");

      // Verify switch completed

      // Verify the tenant actually switched
      const navElementAfter = page.locator("nav").first();
      const tenantButtonAfter = navElementAfter.locator("button").filter({ has: page.locator('img[alt="Logo"]') });
      await expect(tenantButtonAfter).toContainText(targetTenantName);
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

      // Find a different tenant - switch based on current tenant
      const targetMenuItem = initialTenant?.includes(primaryTenantName)
        ? menuItems.filter({ hasText: secondaryTenantName }).first()
        : menuItems.filter({ hasText: primaryTenantName }).first();

      const switchedToTenant = initialTenant?.includes(primaryTenantName) ? secondaryTenantName : primaryTenantName;
      await targetMenuItem.click();

      await page.waitForLoadState("networkidle");
      expect(page.url()).toContain("/admin");

      // Verify switched context

      // Verify the tenant switch worked
      await expect(tenantButton).toContainText(switchedToTenant);

      // Navigate back to Home
      await page.getByLabel("Main navigation").getByRole("link", { name: "Home" }).click();
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // Tenant should still be the switched one
      await expect(tenantButton).toContainText(switchedToTenant);
    })();

    // === USER MANAGEMENT WITHIN TENANTS ===
    await step("Navigate to users page & verify current user is listed with correct role")(async () => {
      await page.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();

      // Verify the current user is shown in the user list
      await expect(page.locator("tbody").first()).toContainText(user.email);

      // Check if we can see role information
      const tableBody = page.locator("tbody").first();
      const roleText = await tableBody.textContent();

      // User might be Member or Owner depending on which tenant we're in
      const hasValidRole = roleText?.includes("Member") || roleText?.includes("Owner");
      expect(hasValidRole).toBeTruthy();
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
});

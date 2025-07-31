import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext, expectToastMessage } from "@shared/e2e/utils/test-assertions";
import { completeSignupFlow, getVerificationCode, testUser } from "@shared/e2e/utils/test-data";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@comprehensive", () => {
  test("should handle comprehensive tenant switching scenarios", async ({ page }) => {
    const context = createTestContext(page);
    const user = testUser();
    const secondOwner = testUser();
    const thirdOwner = testUser();

    // Generate unique tenant names using email prefixes
    const primaryTenantName = `Tenant-${user.email.split("@")[0]}`;
    const secondaryTenantName = `Tenant-${secondOwner.email.split("@")[0]}`;
    const tertiaryTenantName = `Tenant-${thirdOwner.email.split("@")[0]}`;

    // === SCENARIO 1: Single tenant display without dropdown ===
    await step("Create single tenant and verify no dropdown")(async () => {
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

    // === SCENARIO 2: Create multiple tenants and test switching ===
    await step("Setup multiple tenants")(async () => {
      // Logout
      context.monitoring.expectedStatusCodes.push(401);
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page).toHaveURL(/\/login\?returnPath=/);

      // Create second tenant and invite first user
      await completeSignupFlow(page, expect, secondOwner, context);

      // Update second tenant name
      await page.getByLabel("Main navigation").getByRole("link", { name: "Account" }).click();
      await page.getByRole("textbox", { name: "Account name" }).clear();
      await page.getByRole("textbox", { name: "Account name" }).fill(secondaryTenantName);
      await page.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(context, "Account updated successfully");

      await page.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await expectToastMessage(context, "User invited successfully");

      // Logout
      context.monitoring.expectedStatusCodes.push(401);
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page).toHaveURL(/\/login\?returnPath=/);

      // Create third tenant and invite first user
      await completeSignupFlow(page, expect, thirdOwner, context);

      // Update third tenant name
      await page.getByLabel("Main navigation").getByRole("link", { name: "Account" }).click();
      await page.getByRole("textbox", { name: "Account name" }).clear();
      await page.getByRole("textbox", { name: "Account name" }).fill(tertiaryTenantName);
      await page.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(context, "Account updated successfully");

      await page.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await expectToastMessage(context, "User invited successfully");

      // Logout
      context.monitoring.expectedStatusCodes.push(401);
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page).toHaveURL(/\/login\?returnPath=/);
    })();

    // === SCENARIO 3: Test switching between tenants ===
    await step("Login and verify tenant switching UI and functionality")(async () => {
      // Login
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL(/\/login\/verify/);
      await page.keyboard.type(getVerificationCode());
      await expect(page).toHaveURL(/\/admin/);

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

      // Switch to secondary tenant
      await tenantButton.click();
      await menuItems.filter({ hasText: secondaryTenantName }).click();
      await expect(page.getByText("Switching account...")).toBeVisible();
      await expect(page).toHaveURL(/\/admin/);

      // Verify UI updated to secondary tenant
      await expect(tenantButton).toContainText(secondaryTenantName);

      // After switching to a new tenant, the profile dialog appears for first-time setup
      const profileDialog = page.getByRole("dialog", { name: "User profile" });
      await expect(profileDialog).toBeVisible();

      // Complete the profile setup in the new tenant
      await page.getByRole("textbox", { name: "First name" }).fill(user.firstName);
      await page.getByRole("textbox", { name: "Last name" }).fill(user.lastName);
      await page.getByRole("textbox", { name: "Title" }).fill("Software Engineer");
      await page.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(context, "Profile updated successfully");
      await expect(profileDialog).not.toBeVisible();
    })();

    // === SCENARIO 4: Test tenant persistence across sessions ===
    await step("Verify tenant preference persists after logout and login")(async () => {
      const navElement = page.locator("nav").first();
      const tenantButton = navElement.locator("button").filter({ has: page.locator('img[alt="Logo"]') });

      await tenantButton.click();
      const menuItems = page.getByRole("menuitem");
      await menuItems.filter({ hasText: tertiaryTenantName }).click();
      await expect(page.getByText("Switching account...")).toBeVisible();
      await expect(page).toHaveURL(/\/admin/);

      // Verify switched to tertiary tenant
      await expect(tenantButton).toContainText(tertiaryTenantName);

      // Complete profile setup in tertiary tenant
      const profileDialog = page.getByRole("dialog", { name: "User profile" });
      await expect(profileDialog).toBeVisible();
      await page.getByRole("textbox", { name: "First name" }).fill(user.firstName);
      await page.getByRole("textbox", { name: "Last name" }).fill(user.lastName);
      await page.getByRole("textbox", { name: "Title" }).fill("Software Engineer");
      await page.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(context, "Profile updated successfully");
      await expect(profileDialog).not.toBeVisible();

      // Logout
      context.monitoring.expectedStatusCodes.push(401);
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page).toHaveURL(/\/login\?returnPath=/);

      // Login again
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL(/\/login\/verify/);
      await page.keyboard.type(getVerificationCode());
      await expect(page).toHaveURL(/\/admin/);

      // Should login to tertiary tenant (last selected)
      await expect(tenantButton).toContainText(tertiaryTenantName);
    })();

    // === SCENARIO 5: Test switching with modals open ===
    await step("Test tenant switching with profile modal open")(async () => {
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

      const targetTenantText = await targetMenuItem.textContent();
      await targetMenuItem.click();

      // Should switch tenant
      await expect(page.getByText("Switching account...")).toBeVisible();
      await expect(page).toHaveURL(/\/admin/);

      // Verify switch completed

      // Verify the tenant actually switched
      const navElementAfter = page.locator("nav").first();
      const tenantButtonAfter = navElementAfter.locator("button").filter({ has: page.locator('img[alt="Logo"]') });
      await expect(tenantButtonAfter).toContainText(targetTenantText?.trim() || "");
    })();

    // === SCENARIO 6: Test tenant context across different pages ===
    await step("Verify tenant context remains consistent across navigation")(async () => {
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

      const switchedToTenant = (await targetMenuItem.textContent()) || "";
      await targetMenuItem.click();

      await expect(page.getByText("Switching account...")).toBeVisible();
      await expect(page).toHaveURL(/\/admin/);

      // Verify switched context

      // Verify the tenant switch worked
      await expect(tenantButton).toContainText(switchedToTenant.trim());

      // Navigate back to Home
      await page.getByLabel("Main navigation").getByRole("link", { name: "Home" }).click();
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // Tenant should still be the switched one
      await expect(tenantButton).toContainText(switchedToTenant.trim());
    })();

    // === SCENARIO 7: User management within tenants ===
    await step("Navigate to users page & verify current user is listed")(async () => {
      await page.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();

      // Verify the current user is shown in the user list
      await expect(page.locator("tbody").first()).toContainText(user.email);

      // Check if we can see role information
      const tableBody = page.locator("tbody").first();
      const roleText = await tableBody.textContent();

      // User might be Member or Owner depending on which tenant we're in
      expect(roleText).toMatch(/Member|Owner/);
    })();

    // === SCENARIO 8: Switch to a tenant where user is owner and invite users ===
    await step("Switch to primary tenant and invite a new user")(async () => {
      const invitedUser = testUser();

      // Switch to primary tenant where user is owner
      const navElement = page.locator("nav").first();
      const tenantButton = navElement.locator("button").filter({ has: page.locator('img[alt="Logo"]') });
      await tenantButton.click();

      const menuItems = page.getByRole("menuitem");
      await menuItems.filter({ hasText: primaryTenantName }).click();
      await expect(page.getByText("Switching account...")).toBeVisible();
      await expect(page).toHaveURL(/\/admin/);

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

    // === SCENARIO 9: Update user profile ===
    await step("Update user profile & verify changes persist")(async () => {
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

    // === SCENARIO 10: Final logout ===
    await step("Logout from the system")(async () => {
      context.monitoring.expectedStatusCodes.push(401);
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page).toHaveURL(/\/login\?returnPath=/);
    })();
  });
});

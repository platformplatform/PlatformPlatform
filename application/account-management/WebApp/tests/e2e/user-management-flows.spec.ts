import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext, expectToastMessage, typeOneTimeCode } from "@shared/e2e/utils/test-assertions";
import { completeSignupFlow, getVerificationCode, testUser } from "@shared/e2e/utils/test-data";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@smoke", () => {
  /**
   * COMPREHENSIVE USER MANAGEMENT WORKFLOW
   *
   * Tests the complete end-to-end user management journey including:
   * - User invitation process with validation (invalid email, duplicate email)
   * - Role management (changing user roles from Member to Admin)
   * - Permission system (testing what owners vs admins can/cannot do)
   * - Search and filtering functionality (email search, role filtering)
   * - User permission restrictions (what owners vs admins can/cannot do)
   * - Soft delete workflow (delete user via actions menu)
   * - Recycle bin tab visibility (Owner/Admin only, not Members)
   * - Unsaved changes warning (account settings page, dialogs)
   *
   * Note: Restore and permanent delete workflows are tested in @comprehensive
   */
  test("should handle user invitation, role management & permissions workflow", async ({ page }) => {
    const context = createTestContext(page);
    const owner = testUser();
    const adminUser = testUser();
    const memberUser = testUser();
    const deletableUser = testUser();

    await step("Complete owner signup & verify welcome page")(async () => {
      await completeSignupFlow(page, expect, owner, context);
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
    })();

    await step("Navigate to users page & verify owner is listed")(async () => {
      await page.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();

      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
      // Wait for table to load and verify content exists - use first() due to mobile rendering with duplicate tables
      await expect(page.locator("tbody").first().first()).toContainText(owner.email);
      await expect(page.locator("tbody").first().first()).toContainText("Owner");
    })();

    await step("Attempt invitation without account name & verify requirement dialog")(async () => {
      await page.getByRole("button", { name: "Invite user" }).click();

      // Verify account name required dialog appears
      await expect(page.getByRole("dialog", { name: "Add your account name" })).toBeVisible();
      await expect(page.getByText("Your team needs to know who's inviting them")).toBeVisible();

      // Navigate to account settings via dialog
      await page.getByRole("button", { name: "Go to account settings" }).click();
      await expect(page).toHaveURL("/admin/account");
    })();

    await step("Set account name & verify successful save")(async () => {
      await expect(page.getByRole("heading", { name: "Account settings" })).toBeVisible();
      await page.getByRole("textbox", { name: "Account name" }).fill("Test Company");
      await page.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(context, "Account name updated successfully");
    })();

    await step("Modify account name, navigate away & verify unsaved changes warning")(async () => {
      await page.getByRole("textbox", { name: "Account name" }).fill("Modified Company");
      await page.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();

      await expect(page.getByRole("alertdialog", { name: "Unsaved changes" })).toBeVisible();
      await expect(
        page.getByText("You have unsaved changes. If you leave now, your changes will be lost.")
      ).toBeVisible();

      await page.getByRole("button", { name: "Stay" }).click();
      await expect(page.getByRole("alertdialog", { name: "Unsaved changes" })).not.toBeVisible();
      await expect(page).toHaveURL("/admin/account");
      await expect(page.getByRole("textbox", { name: "Account name" })).toHaveValue("Modified Company");

      await page.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();
      await expect(page.getByRole("alertdialog", { name: "Unsaved changes" })).toBeVisible();
      await page.getByRole("button", { name: "Leave" }).click();

      await expect(page).toHaveURL("/admin/users");
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
    })();

    // Email validation is comprehensively tested in signup-flows.spec.ts

    await step("Open Invite dialog, enter email & verify unsaved changes warning on Escape")(async () => {
      await page.getByRole("button", { name: "Invite user" }).first().click();
      await expect(page.getByRole("dialog", { name: "Invite user" })).toBeVisible();
      await page.getByRole("textbox", { name: "Email" }).click();
      await page.keyboard.type("test@example.com");

      await page.keyboard.press("Escape");

      await expect(page.getByRole("alertdialog", { name: "Unsaved changes" })).toBeVisible();
      await page.getByRole("button", { name: "Stay" }).click();
      await expect(page.getByRole("alertdialog", { name: "Unsaved changes" })).not.toBeVisible();
      await expect(page.getByRole("dialog", { name: "Invite user" })).toBeVisible();
      await expect(page.getByRole("textbox", { name: "Email" })).toHaveValue("test@example.com");

      await page.keyboard.press("Escape");
      await expect(page.getByRole("alertdialog", { name: "Unsaved changes" })).toBeVisible();
      await page.getByRole("button", { name: "Leave" }).click();

      await expect(page.getByRole("alertdialog", { name: "Unsaved changes" })).not.toBeVisible();
      await expect(page.getByRole("dialog", { name: "Invite user" })).not.toBeVisible();
    })();

    await step("Invite member user & verify successful invitation")(async () => {
      await page.getByRole("button", { name: "Invite user" }).first().click();
      await expect(page.getByRole("dialog", { name: "Invite user" })).toBeVisible();
      await page.getByRole("textbox", { name: "Email" }).fill(memberUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();

      await expectToastMessage(context, "User invited successfully");
      await expect(page.getByRole("dialog", { name: "Invite user" })).not.toBeVisible();
      await expect(page.locator("tbody").first().first()).toContainText(memberUser.email);
      await expect(page.locator("tbody").first().first()).toContainText(owner.email);
    })();

    await step("Invite admin user & verify successful invitation")(async () => {
      await page.getByRole("button", { name: "Invite user" }).first().click();
      await expect(page.getByRole("dialog", { name: "Invite user" })).toBeVisible();
      await page.getByRole("textbox", { name: "Email" }).fill(adminUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();

      await expectToastMessage(context, "User invited successfully");
      await expect(page.getByRole("dialog", { name: "Invite user" })).not.toBeVisible();
      await expect(page.locator("tbody").first().first()).toContainText(adminUser.email);
      await expect(page.locator("tbody").first().first()).toContainText(memberUser.email);
      await expect(page.locator("tbody").first().first()).toContainText(owner.email);
    })();

    await step("Invite deletable user for soft delete testing")(async () => {
      await page.getByRole("button", { name: "Invite user" }).first().click();
      await expect(page.getByRole("dialog", { name: "Invite user" })).toBeVisible();
      await page.getByRole("textbox", { name: "Email" }).fill(deletableUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();

      await expectToastMessage(context, "User invited successfully");
      await expect(page.getByRole("dialog", { name: "Invite user" })).not.toBeVisible();
      await expect(page.locator("tbody").first().first()).toContainText(deletableUser.email);
    })();

    await step("Open Change Role dialog, select role & verify unsaved changes warning on Escape")(async () => {
      const adminUserRow = page.locator("tbody").first().locator("tr").filter({ hasText: adminUser.email });
      const actionsButton = adminUserRow.locator("button[aria-label='User actions']").first();
      await actionsButton.dispatchEvent("click");

      await expect(page.getByRole("menu")).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const changeRoleMenuItem = page.getByRole("menuitem", { name: "Change role" });
      await expect(changeRoleMenuItem).toBeVisible();
      await changeRoleMenuItem.dispatchEvent("click");

      await expect(page.getByRole("dialog", { name: "Change user role" })).toBeVisible();
      await page.getByRole("radio", { name: "Owner" }).check({ force: true });

      await page.keyboard.press("Escape");

      await expect(page.getByRole("alertdialog", { name: "Unsaved changes" })).toBeVisible();
      await page.getByRole("button", { name: "Stay" }).click();
      await expect(page.getByRole("alertdialog", { name: "Unsaved changes" })).not.toBeVisible();
      await expect(page.getByRole("dialog", { name: "Change user role" })).toBeVisible();
      await expect(page.getByRole("radio", { name: "Owner" })).toBeChecked();

      await page.getByRole("radio", { name: "Owner" }).focus();
      await page.keyboard.press("Escape");
      await expect(page.getByRole("alertdialog", { name: "Unsaved changes" })).toBeVisible();
      await page.getByRole("button", { name: "Leave" }).click();

      await expect(page.getByRole("alertdialog", { name: "Unsaved changes" })).not.toBeVisible();
      await expect(page.getByRole("dialog", { name: "Change user role" })).not.toBeVisible();
    })();

    await step("Open actions menu for admin user and change role to Admin & verify role updates")(async () => {
      const adminUserRow = page.locator("tbody").first().locator("tr").filter({ hasText: adminUser.email });
      const actionsButton = adminUserRow.locator("button[aria-label='User actions']").first();
      await actionsButton.dispatchEvent("click");

      // Wait for menu to be visible before clicking
      await expect(page.getByRole("menu")).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const changeRoleMenuItem = page.getByRole("menuitem", { name: "Change role" });
      await expect(changeRoleMenuItem).toBeVisible();
      await changeRoleMenuItem.dispatchEvent("click");

      await expect(page.getByRole("dialog", { name: "Change user role" })).toBeVisible();
      await page.getByRole("radio", { name: "Admin" }).check({ force: true });
      await page.getByRole("button", { name: "Save changes" }).click();

      await expectToastMessage(context, `User role updated successfully for ${adminUser.email}`);

      // Wait for dialog to close
      await expect(page.getByRole("dialog", { name: "Change user role" })).not.toBeVisible();
      await expect(adminUserRow.first()).toContainText("Admin");
    })();

    await step("Attempt to invite duplicate user email & verify error message appears")(async () => {
      await page.getByRole("button", { name: "Invite user" }).first().click();
      await expect(page.getByRole("dialog", { name: "Invite user" })).toBeVisible();
      await page.getByRole("textbox", { name: "Email" }).fill(memberUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();

      await expectToastMessage(context, 400, `The user '${memberUser.email}' already exists.`);

      await page.getByRole("button", { name: "Cancel" }).click();
      await expect(page.getByRole("dialog", { name: "Invite user" })).not.toBeVisible();
    })();

    await step("Open users table & verify invited users appear with correct roles")(async () => {
      // Set viewport to ensure role badges are visible
      await page.setViewportSize({ width: 1280, height: 720 });

      const userTable = page.locator("tbody").first().first();
      // Verify all users are visible without counting rows due to mobile rendering differences
      await expect(userTable).toContainText(adminUser.email);
      await expect(userTable).toContainText(memberUser.email);
      await expect(userTable).toContainText(owner.email);
    })();

    await step("Open owner's actions menu & verify self-deletion and role change are disabled")(async () => {
      const ownerRowSelf = page.locator("tbody").first().locator("tr").filter({ hasText: owner.email });
      const ownerActionsButton = ownerRowSelf.locator("button[aria-label='User actions']").first();
      await ownerActionsButton.dispatchEvent("click");

      await expect(page.getByRole("menuitem", { name: "Delete" })).toBeDisabled();
      await expect(page.getByRole("menuitem", { name: "Change role" })).toBeDisabled();

      // Click outside the menu to close it
      await page.locator("body").click({ position: { x: 10, y: 10 } });
    })();

    await step("Filter users by email search & verify filtered results display correctly")(async () => {
      // Ensure viewport is desktop size for search to be visible
      await page.setViewportSize({ width: 1280, height: 720 });

      const userTable = page.locator("tbody").first();

      // React Aria SearchField uses a controlled input pattern incompatible with Playwright.
      // Escape key clears the search (real UI interaction); URL sets up filtered state.
      await page.goto(`/admin/users?search=${encodeURIComponent(adminUser.email)}`);

      // Verify only admin user is shown without counting rows
      await expect(userTable).toContainText(adminUser.email);
      await expect(userTable).not.toContainText(owner.email);
      await expect(userTable).not.toContainText(memberUser.email);

      // Clear search and verify all users are shown again
      const searchField = page.getByRole("searchbox", { name: "Search" });
      await searchField.focus();
      await page.keyboard.press("Escape"); // Trigger search immediately to show all results

      await expect(page).not.toHaveURL(/search=/);

      await expect(userTable).toContainText(adminUser.email);
      await expect(userTable).toContainText(memberUser.email);
      await expect(userTable).toContainText(owner.email);
    })();

    await step("Filter users by role & verify role-based filtering works correctly")(async () => {
      const userTable = page.locator("tbody").first().first();
      await page.getByRole("button", { name: "Show filters" }).click();

      const filterDialog = page.getByRole("dialog", { name: "Filters" });
      await expect(filterDialog).toBeVisible();

      await filterDialog.getByLabel("User role").click();
      await page.getByRole("option", { name: "Owner" }).click();

      await filterDialog.getByRole("button", { name: "OK" }).click();

      // Verify only owner is shown without counting rows
      await expect(filterDialog).not.toBeVisible();
      await expect(userTable).toContainText(owner.email);
      await expect(userTable).not.toContainText(adminUser.email);
      await expect(userTable).not.toContainText(memberUser.email);

      await page.getByRole("button", { name: "Show filters" }).click();
      await expect(filterDialog).toBeVisible();

      await filterDialog.getByLabel("User role").click();
      await page.getByRole("option", { name: "Any role" }).click();
      await filterDialog.getByRole("button", { name: "OK" }).click();

      // Verify all users are shown again
      await expect(filterDialog).not.toBeVisible();
      await expect(userTable).toContainText(adminUser.email);
      await expect(userTable).toContainText(memberUser.email);
      await expect(userTable).toContainText(owner.email);
    })();

    // === ACTIVATE DELETABLE USER TO ENABLE SOFT DELETE ===
    await step("Logout from owner to activate deletable user")(async () => {
      context.monitoring.expectedStatusCodes.push(401);
      const triggerButton = page.getByRole("button", { name: "User profile menu" });
      await triggerButton.dispatchEvent("click");
      const userMenu = page.getByRole("menu");
      await expect(userMenu).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem = page.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      await expect(page.getByRole("textbox", { name: "Email" })).toBeVisible();
    })();

    await step("Login as deletable user & verify unsaved changes warning on profile Escape")(async () => {
      await page.getByRole("textbox", { name: "Email" }).fill(deletableUser.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await typeOneTimeCode(page, getVerificationCode());

      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();
      await page.getByRole("textbox", { name: "First name" }).fill(deletableUser.firstName);

      await page.keyboard.press("Escape");

      await expect(page.getByRole("alertdialog", { name: "Unsaved changes" })).toBeVisible();
      await page.getByRole("button", { name: "Stay" }).click();
      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();
      await expect(page.getByRole("textbox", { name: "First name" })).toHaveValue(deletableUser.firstName);

      await page.getByRole("textbox", { name: "Last name" }).fill(deletableUser.lastName);
      await page.getByRole("button", { name: "Save changes" }).click();

      await expectToastMessage(context, "Profile updated successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
    })();

    await step("Logout from deletable user & login as owner")(async () => {
      context.monitoring.expectedStatusCodes.push(401);
      const triggerButton = page.getByRole("button", { name: "User profile menu" });
      await triggerButton.dispatchEvent("click");
      const userMenu = page.getByRole("menu");
      await expect(userMenu).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem = page.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      await expect(page.getByRole("textbox", { name: "Email" })).toBeVisible();

      await page.getByRole("textbox", { name: "Email" }).fill(owner.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await typeOneTimeCode(page, getVerificationCode());

      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
    })();

    // === SOFT DELETE WORKFLOW SECTION ===
    await step("Soft delete user via actions menu & verify removed from All users tab")(async () => {
      const deletableUserRow = page.locator("tbody").first().locator("tr").filter({ hasText: deletableUser.email });
      const deletableActionsButton = deletableUserRow.locator("button[aria-label='User actions']").first();
      await deletableActionsButton.dispatchEvent("click");

      await expect(page.getByRole("menu")).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const deleteMenuItem = page.getByRole("menuitem", { name: "Delete" });
      await expect(deleteMenuItem).toBeVisible();
      await deleteMenuItem.dispatchEvent("click");

      const deleteDialog = page.getByRole("alertdialog", { name: "Delete user" });
      await expect(deleteDialog).toBeVisible();
      const deletableFullName = `${deletableUser.firstName} ${deletableUser.lastName}`;
      await expect(page.getByText(`Are you sure you want to delete ${deletableFullName}?`)).toBeVisible();
      const deleteButton = deleteDialog.getByRole("button", { name: "Delete" });
      await expect(deleteButton).toBeEnabled();
      await deleteButton.click();

      await expectToastMessage(context, `User deleted successfully: ${deletableFullName}`);
      await expect(deleteDialog).not.toBeVisible();
      await expect(page.locator("tbody").first()).not.toContainText(deletableUser.email);
      await expect(page.locator("tbody").first().locator("tr")).toHaveCount(3);
    })();

    await step("Navigate to Recycle bin tab & verify soft-deleted user appears")(async () => {
      await page.getByRole("link", { name: "Recycle bin" }).click();

      await expect(page).toHaveURL("/admin/users/recycle-bin");
      await expect(page.getByRole("table", { name: "Deleted users" })).toContainText(deletableUser.email);
    })();

    await step("Logout from owner account & verify redirect to login")(async () => {
      // Mark 401 as expected during logout transition (React Query may have in-flight requests)
      context.monitoring.expectedStatusCodes.push(401);

      // Navigate to home first
      await page.goto("/admin");
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      const triggerButton = page.getByRole("button", { name: "User profile menu" });
      await triggerButton.dispatchEvent("click");
      const userMenu = page.getByRole("menu");
      await expect(userMenu).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem = page.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      await expect(page).toHaveURL("/login?returnPath=%2Fadmin");
    })();

    await step("Login as admin user & verify successful authentication")(async () => {
      await page.getByRole("textbox", { name: "Email" }).fill(adminUser.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login/verify?returnPath=%2Fadmin");
      await typeOneTimeCode(page, getVerificationCode());

      await expect(page).toHaveURL("/admin");
    })();

    await step("Complete admin user profile setup & verify profile saved")(async () => {
      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();
      await page.getByRole("textbox", { name: "First name" }).fill(adminUser.firstName);
      await page.getByRole("textbox", { name: "Last name" }).fill(adminUser.lastName);
      await page.getByRole("textbox", { name: "Title" }).fill("Administrator");
      await page.getByRole("button", { name: "Save changes" }).click();

      await expectToastMessage(context, "Profile updated successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
    })();

    await step("Navigate to users page as admin & verify admin can see all users")(async () => {
      await page.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();

      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
      // Use first tbody due to mobile rendering with duplicate tables
      await expect(page.locator("tbody").first().first().locator("tr")).toHaveCount(3); // owner + admin + member
    })();

    await step("Navigate to Recycle bin tab as admin & verify deleted user appears")(async () => {
      await expect(page.getByRole("link", { name: "Recycle bin" })).toBeVisible();
      await page.getByRole("link", { name: "Recycle bin" }).click();

      await expect(page).toHaveURL("/admin/users/recycle-bin");
      await expect(page.getByRole("table", { name: "Deleted users" })).toContainText(deletableUser.email);
    })();

    await step("Logout from admin account & verify redirect to login")(async () => {
      context.monitoring.expectedStatusCodes.push(401);
      const profileMenuButton = page.getByRole("button", { name: "User profile menu" });
      await profileMenuButton.focus();
      await page.keyboard.press("Enter");
      await expect(page.getByRole("menu")).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem = page.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      await expect(page).toHaveURL("/login?returnPath=%2Fadmin%2Fusers%2Frecycle-bin");
    })();

    // === MEMBER PERMISSION CHECK SECTION ===
    await step("Login as member user & verify access denied on recycle-bin")(async () => {
      await page.getByRole("textbox", { name: "Email" }).fill(memberUser.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login/verify?returnPath=%2Fadmin%2Fusers%2Frecycle-bin");
      await typeOneTimeCode(page, getVerificationCode());

      // Member lands on recycle-bin but sees access denied (requires Owner/Admin role)
      await expect(page.getByRole("heading", { name: "Access denied" })).toBeVisible();
      await expect(page.getByText("You do not have permission to access this page.")).toBeVisible();
    })();

    await step("Navigate to users page & complete profile setup")(async () => {
      // Navigate to users page where member has access and profile dialog appears
      await page.getByRole("button", { name: "Go to home" }).click();
      await expect(page).toHaveURL("/");

      await page.goto("/admin/users");

      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();
      await expect(page.getByRole("textbox", { name: "First name" })).toBeVisible();
      await page.getByRole("textbox", { name: "First name" }).fill(memberUser.firstName);
      await page.getByRole("textbox", { name: "Last name" }).fill(memberUser.lastName);
      await page.getByRole("textbox", { name: "Title" }).fill("Team Member");
      await page.getByRole("button", { name: "Save changes" }).click();

      await expectToastMessage(context, "Profile updated successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Verify member sees Users page without Recycle bin tab
      await expect(page).toHaveURL("/admin/users");
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(page.getByRole("link", { name: "All users" })).toBeVisible();
      await expect(page.getByRole("link", { name: "Recycle bin" })).not.toBeVisible();
    })();
  });
});

test.describe("@comprehensive", () => {
  /**
   * USER DELETION WORKFLOWS WITH DASHBOARD INTEGRATION AND LASTSEENAT VERIFICATION
   *
   * Tests comprehensive user deletion functionality with dashboard context including:
   * - Dashboard metrics integration (user count displays)
   * - URL-based filtering (active users link)
   * - Advanced filtering (role, status combinations)
   * - Role change with LastSeenAt preservation verification
   * - Single user soft deletion via menu actions
   * - Bulk user selection and bulk deletion
   * - Owner protection mechanisms (deletion restrictions)
   * - Restore user from Recycle bin tab with LastSeenAt preservation verification
   * - Permanent delete user via confirmation dialog
   * - Empty recycle bin functionality
   */
  test("should handle single and bulk user deletion workflows with dashboard integration", async ({ page }) => {
    const context = createTestContext(page);
    const owner = testUser();
    const user1 = testUser();
    const user2 = testUser();

    // === USER SETUP SECTION ===
    await step("Complete owner signup & verify welcome page")(async () => {
      await completeSignupFlow(page, expect, owner, context);
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
    })();

    await step("Set account name for user invitations")(async () => {
      await page.goto("/admin/account");
      await expect(page.getByRole("heading", { name: "Account settings" })).toBeVisible();
      await page.getByRole("textbox", { name: "Account name" }).fill("Test Company");
      await page.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(context, "Account name updated successfully");
    })();

    await step("Navigate to users page & verify owner is listed")(async () => {
      await page.goto("/admin/users");
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
    })();

    await step("Invite multiple users & verify they are added to the list")(async () => {
      const usersToInvite = [user1, user2];

      for (const user of usersToInvite) {
        await page.getByRole("button", { name: "Invite user" }).click();
        await page.getByRole("textbox", { name: "Email" }).fill(user.email);
        await page.getByRole("button", { name: "Send invite" }).click();

        await expectToastMessage(context, "User invited successfully");
        await expect(page.getByRole("dialog")).not.toBeVisible();
      }

      await expect(page.locator("tbody").first().first().locator("tr")).toHaveCount(3); // owner + 2 invited users
    })();

    // === DASHBOARD METRICS SECTION ===
    await step("Navigate to dashboard & verify user count metrics display correctly")(async () => {
      await page.goto("/admin");

      // Verify dashboard shows correct user counts
      await expect(page.getByRole("link", { name: "View users" })).toContainText("3");
      await expect(page.getByRole("link", { name: "View active users" })).toContainText("1");
      await expect(page.getByRole("link", { name: "View invited users" })).toContainText("2");
    })();

    // === URL FILTERING SECTION ===
    await step("Click invited users link & verify URL filtering works correctly")(async () => {
      await page.getByRole("link", { name: "View invited users" }).click();

      // Verify filtering by URL parameter
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(page.locator("tbody").first().first().locator("tr")).toHaveCount(2);
      await expect(page).toHaveURL(/userStatus=Pending/);
    })();

    // === ADVANCED FILTERING SECTION ===
    await step("Show filters & verify all filter options are available")(async () => {
      // Show filters
      await page.getByRole("button", { name: "Show filters" }).click();

      await expect(page.getByLabel("User role").first()).toBeVisible();
      await expect(page.getByLabel("User status").first()).toBeVisible();
      await expect(page.getByText("Modified date").first()).toBeVisible();
    })();

    await step("Filter by Owner role & verify only owner shown")(async () => {
      // Filters are in the dialog that was opened in the previous step
      const filterDialog = page.getByRole("dialog", { name: "Filters" });

      // Clear status filter
      await filterDialog.getByLabel("User status").click();
      await page.getByRole("option", { name: "Any status" }).click();

      // Set role filter to Owner
      await filterDialog.getByLabel("User role").click();
      await page.getByRole("option", { name: "Owner" }).click();

      // Click OK to apply and close dialog
      await filterDialog.getByRole("button", { name: "OK" }).click();
      await expect(filterDialog).not.toBeVisible();

      // Verify only owner is shown
      await expect(page.locator("tbody").first().first().locator("tr")).toHaveCount(1);
      await expect(page.locator("tbody").first()).toContainText(owner.email);
      await expect(page.locator("tbody").first()).not.toContainText(user1.email);
      await expect(page.locator("tbody").first()).not.toContainText(user2.email);
    })();

    await step("Filter by Member role & verify only members shown")(async () => {
      // Reopen dialog
      await page.getByRole("button", { name: "Show filters" }).click();
      const filterDialog = page.getByRole("dialog", { name: "Filters" });
      await expect(filterDialog).toBeVisible();

      // Set role filter to Member
      await filterDialog.getByLabel("User role").click();
      await page.getByRole("option", { name: "Member" }).click();

      // Click OK to apply and close dialog
      await filterDialog.getByRole("button", { name: "OK" }).click();
      await expect(filterDialog).not.toBeVisible();

      // Verify only member users are shown
      await expect(page.locator("tbody").first().first().locator("tr")).toHaveCount(2);
      await expect(page.locator("tbody").first()).toContainText(user1.email);
      await expect(page.locator("tbody").first()).toContainText(user2.email);
      await expect(page.locator("tbody").first()).not.toContainText(owner.email);
    })();

    await step("Clear all filters & verify all users are shown")(async () => {
      // Reopen filter dialog
      await page.getByRole("button", { name: "Show filters" }).click();
      const filterDialog = page.getByRole("dialog", { name: "Filters" });
      await expect(filterDialog).toBeVisible();

      // Click Clear button to reset all filters
      await filterDialog.getByRole("button", { name: "Clear" }).click();

      // Close dialog with Escape (OK button gets detached after Clear click due to re-render)
      await page.keyboard.press("Escape");
      await expect(filterDialog).not.toBeVisible();

      // Verify all 3 users are shown (no filters applied)
      await expect(page.locator("tbody").first().first().locator("tr")).toHaveCount(3);
    })();

    await step("Filter by Pending status & verify only pending users shown")(async () => {
      // Reopen filter dialog
      await page.getByRole("button", { name: "Show filters" }).click();
      const filterDialog = page.getByRole("dialog", { name: "Filters" });
      await expect(filterDialog).toBeVisible();

      // Set status filter to Pending
      await filterDialog.getByLabel("User status").click();
      await page.getByRole("option", { name: "Pending" }).click();

      // Close dialog
      await filterDialog.getByRole("button", { name: "OK" }).click();
      await expect(filterDialog).not.toBeVisible();

      // Verify only pending users are shown (invited users who haven't confirmed)
      await expect(page.locator("tbody").first().first().locator("tr")).toHaveCount(2);
      await expect(page.locator("tbody").first()).toContainText(user1.email);
      await expect(page.locator("tbody").first()).toContainText(user2.email);
      await expect(page.locator("tbody").first()).not.toContainText(owner.email);
    })();

    await step("Filter by Active status & verify only active users shown")(async () => {
      // Reopen dialog
      await page.getByRole("button", { name: "Show filters" }).click();
      const filterDialog = page.getByRole("dialog", { name: "Filters" });
      await expect(filterDialog).toBeVisible();

      // Set status filter to Active
      await filterDialog.getByLabel("User status").click();
      await page.getByRole("option", { name: "Active" }).click();

      // Click OK to apply and close dialog
      await filterDialog.getByRole("button", { name: "OK" }).click();
      await expect(filterDialog).not.toBeVisible();

      // Verify only active users are shown (owner who has confirmed email)
      await expect(page.locator("tbody").first().first().locator("tr")).toHaveCount(1);
      await expect(page.locator("tbody").first()).toContainText(owner.email);
      await expect(page.locator("tbody").first()).not.toContainText(user1.email);
    })();

    // === ACTIVATE USERS TO ENABLE SOFT DELETE ===
    await step("Logout from owner & login as user1 to confirm email")(async () => {
      context.monitoring.expectedStatusCodes.push(401);
      const triggerButton = page.getByRole("button", { name: "User profile menu" });
      await triggerButton.dispatchEvent("click");
      await expect(page.getByRole("menu")).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem = page.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      await expect(page.getByRole("textbox", { name: "Email" })).toBeVisible();
      await page.getByRole("textbox", { name: "Email" }).fill(user1.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await typeOneTimeCode(page, getVerificationCode());

      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();
      await page.getByRole("textbox", { name: "First name" }).fill(user1.firstName);
      await page.getByRole("textbox", { name: "Last name" }).fill(user1.lastName);
      await page.getByRole("button", { name: "Save changes" }).click();

      await expectToastMessage(context, "Profile updated successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
    })();

    await step("Logout from user1 & login as user2 to confirm email")(async () => {
      context.monitoring.expectedStatusCodes.push(401);
      const triggerButton = page.getByRole("button", { name: "User profile menu" });
      await triggerButton.dispatchEvent("click");
      await expect(page.getByRole("menu")).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem = page.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      await expect(page.getByRole("textbox", { name: "Email" })).toBeVisible();
      await page.getByRole("textbox", { name: "Email" }).fill(user2.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await typeOneTimeCode(page, getVerificationCode());

      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();
      await page.getByRole("textbox", { name: "First name" }).fill(user2.firstName);
      await page.getByRole("textbox", { name: "Last name" }).fill(user2.lastName);
      await page.getByRole("button", { name: "Save changes" }).click();

      await expectToastMessage(context, "Profile updated successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
    })();

    await step("Logout from user2 & login back as owner")(async () => {
      context.monitoring.expectedStatusCodes.push(401);
      await expect(page.getByRole("region", { name: /notification/ })).not.toBeVisible();
      const triggerButton = page.getByRole("button", { name: "User profile menu" });
      await triggerButton.dispatchEvent("click");
      await expect(page.getByRole("menu")).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem = page.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      await expect(page.getByRole("textbox", { name: "Email" })).toBeVisible();
      await page.getByRole("textbox", { name: "Email" }).fill(owner.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await typeOneTimeCode(page, getVerificationCode());

      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
    })();

    await step("Navigate to dashboard & verify updated active user counts")(async () => {
      await page.goto("/admin");

      await expect(page.getByRole("link", { name: "View users" })).toContainText("3");
      await expect(page.getByRole("link", { name: "View active users" })).toContainText("3");
      await expect(page.getByRole("link", { name: "View invited users" })).toContainText("0");
    })();

    await step("Navigate to users page & verify all users shown for deletion tests")(async () => {
      await page.goto("/admin/users");

      await expect(page.locator("tbody").first().first().locator("tr")).toHaveCount(3);
      await expect(page.locator("tbody").first()).toContainText(owner.email);
      await expect(page.locator("tbody").first()).toContainText(user1.email);
      await expect(page.locator("tbody").first()).toContainText(user2.email);
    })();

    // === ROLE CHANGE WITH LASTSEENAT PRESERVATION SECTION ===
    await step("Change user1 role to Admin & verify LastSeenAt unchanged")(async () => {
      const user1Row = page.locator("tbody").first().locator("tr").filter({ hasText: user1.email });
      const user1LastSeenAtBefore = await user1Row.locator("td").nth(3).innerText();

      const user1ActionsButton = user1Row.locator("button[aria-label='User actions']").first();
      await user1ActionsButton.dispatchEvent("click");

      await expect(page.getByRole("menu")).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const changeRoleMenuItem = page.getByRole("menuitem", { name: "Change role" });
      await expect(changeRoleMenuItem).toBeVisible();
      await changeRoleMenuItem.dispatchEvent("click");

      await expect(page.getByRole("dialog", { name: "Change user role" })).toBeVisible();
      await page.getByRole("radio", { name: "Admin" }).check({ force: true });
      await page.getByRole("button", { name: "Save changes" }).click();

      const user1FullName = `${user1.firstName} ${user1.lastName}`;
      await expectToastMessage(context, `User role updated successfully for ${user1FullName}`);
      await expect(page.getByRole("dialog", { name: "Change user role" })).not.toBeVisible();

      await expect(user1Row.locator("td").nth(3)).toHaveText(user1LastSeenAtBefore);
      await expect(user1Row).toContainText("Admin");
    })();

    // === SOFT DELETION WITH LASTSEENAT PRESERVATION SECTION ===
    let user1LastSeenAtBeforeDelete: string;

    await step("Capture user1 LastSeenAt & soft delete via menu")(async () => {
      const user1FullName = `${user1.firstName} ${user1.lastName}`;
      const user1Row = page.locator("tbody").first().locator("tr").filter({ hasText: user1.email });

      user1LastSeenAtBeforeDelete = await user1Row.locator("td").nth(3).innerText();

      const user1ActionsButton = user1Row.locator("button[aria-label='User actions']").first();
      await user1ActionsButton.dispatchEvent("click");
      await expect(page.getByRole("menu")).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const deleteMenuItem1 = page.getByRole("menuitem", { name: "Delete" });
      await expect(deleteMenuItem1).toBeVisible();
      await deleteMenuItem1.dispatchEvent("click");

      const deleteDialog = page.getByRole("alertdialog", { name: "Delete user" });
      await expect(deleteDialog).toBeVisible();
      await expect(page.getByText(`Are you sure you want to delete ${user1FullName}?`)).toBeVisible();
      const deleteButton = deleteDialog.getByRole("button", { name: "Delete" });
      await expect(deleteButton).toBeEnabled();
      await deleteButton.click();

      await expectToastMessage(context, `User deleted successfully: ${user1FullName}`);
      await expect(deleteDialog).not.toBeVisible();
      await expect(page.locator("tbody").first().first().locator("tr")).toHaveCount(2);
      await expect(page.getByText(user1.email)).not.toBeVisible();
      await expect(page.locator("tbody").first()).toContainText(owner.email);
      await expect(page.locator("tbody").first()).toContainText(user2.email);
    })();

    await step("Soft delete user2 via menu & verify removal from All users")(async () => {
      const user2FullName = `${user2.firstName} ${user2.lastName}`;

      const user2Row = page.locator("tbody").first().locator("tr").filter({ hasText: user2.email });
      const user2ActionsButton = user2Row.locator("button[aria-label='User actions']").first();
      await user2ActionsButton.dispatchEvent("click");
      await expect(page.getByRole("menu")).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const deleteMenuItem2 = page.getByRole("menuitem", { name: "Delete" });
      await expect(deleteMenuItem2).toBeVisible();
      await deleteMenuItem2.dispatchEvent("click");

      const deleteDialog = page.getByRole("alertdialog", { name: "Delete user" });
      await expect(deleteDialog).toBeVisible();
      await expect(page.getByText(`Are you sure you want to delete ${user2FullName}?`)).toBeVisible();
      const deleteButton = deleteDialog.getByRole("button", { name: "Delete" });
      await expect(deleteButton).toBeEnabled();
      await deleteButton.click();

      await expectToastMessage(context, `User deleted successfully: ${user2FullName}`);
      await expect(deleteDialog).not.toBeVisible();
      await expect(page.locator("tbody").first().first().locator("tr")).toHaveCount(1);
      await expect(page.getByText(user2.email)).not.toBeVisible();
      await expect(page.locator("tbody").first()).toContainText(owner.email);
    })();

    // === OWNER PROTECTION SECTION ===
    await step("Open owner actions menu & verify delete option is disabled")(async () => {
      const usersGrid = page.getByRole("table", { name: "Users" });
      const ownerRow = usersGrid.getByRole("row").filter({ hasText: owner.email });
      await ownerRow.getByRole("button", { name: "User actions" }).click();

      await expect(page.getByRole("menuitem", { name: "Delete" })).toBeDisabled();

      await page.keyboard.press("Escape");
    })();

    // === RESTORE AND PERMANENT DELETE SECTION ===
    await step("Navigate to Recycle bin tab & verify soft-deleted users appear")(async () => {
      await page.getByRole("link", { name: "Recycle bin" }).click();

      await expect(page).toHaveURL("/admin/users/recycle-bin");
      const deletedUsersGrid = page.getByRole("table", { name: "Deleted users" });
      await expect(deletedUsersGrid).toContainText(user1.email);
      await expect(deletedUsersGrid).toContainText(user2.email);
    })();

    await step("Select user1 row & restore via toolbar button")(async () => {
      const deletedUsersGrid = page.getByRole("table", { name: "Deleted users" });
      const user1Row = deletedUsersGrid.locator("tr").filter({ hasText: user1.email });
      await user1Row.click();

      const restoreButton = page.getByRole("button", { name: "Restore user" });
      await expect(restoreButton).toBeVisible();
      await restoreButton.click();

      const user1FullName = `${user1.firstName} ${user1.lastName}`;
      await expectToastMessage(context, `User restored successfully: ${user1FullName}`);

      await expect(deletedUsersGrid).not.toContainText(user1.email);
      await expect(deletedUsersGrid).toContainText(user2.email);
    })();

    await step("Select user2 row & permanently delete via toolbar")(async () => {
      const deletedUsersGrid = page.getByRole("table", { name: "Deleted users" });
      const user2Row = deletedUsersGrid.locator("tr").filter({ hasText: user2.email });
      await user2Row.click();

      const deleteButton = page.getByRole("button", { name: "Permanently delete user" });
      await expect(deleteButton).toBeVisible();
      await deleteButton.click();

      const deleteDialog = page.getByRole("alertdialog", { name: "Permanently delete user" });
      await expect(deleteDialog).toBeVisible();
      const confirmDeleteButton = deleteDialog.getByRole("button", { name: "Delete permanently" });
      await expect(confirmDeleteButton).toBeEnabled();
      await confirmDeleteButton.click();

      const user2FullName = `${user2.firstName} ${user2.lastName}`;
      await expectToastMessage(context, `User permanently deleted: ${user2FullName}`);

      await expect(deleteDialog).not.toBeVisible();
      await expect(page).toHaveURL("/admin/users/recycle-bin");
      await expect(page.getByRole("table", { name: "Deleted users" })).not.toBeVisible();
      await expect(page.getByRole("main").getByText("No deleted users").last()).toBeVisible();
    })();

    await step("Navigate to All users & verify restored user LastSeenAt unchanged")(async () => {
      await page.getByRole("link", { name: "All users" }).click();

      await expect(page).toHaveURL("/admin/users");
      await expect(page.locator("tbody").first().first().locator("tr")).toHaveCount(2);
      await expect(page.locator("tbody").first()).toContainText(owner.email);
      await expect(page.locator("tbody").first()).toContainText(user1.email);
      await expect(page.locator("tbody").first()).not.toContainText(user2.email);

      const user1Row = page.locator("tbody").first().locator("tr").filter({ hasText: user1.email });
      await expect(user1Row.locator("td").nth(3)).toHaveText(user1LastSeenAtBeforeDelete);
    })();
  });
});

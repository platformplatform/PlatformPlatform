import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { assertToastMessage, assertValidationError, createTestContext } from "@shared/e2e/utils/test-assertions";
import { completeSignupFlow, getVerificationCode, testUser } from "@shared/e2e/utils/test-data";

test.describe("User Management Flow", () => {
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
     */
    test("should handle complete user invitation, role management & admin permissions workflow", async ({ page }) => {
      const context = createTestContext(page);
      const owner = testUser();
      const adminUser = testUser();
      const memberUser = testUser();

      // Act & Assert: Complete owner signup & verify owner account creation
      await completeSignupFlow(page, expect, owner, context);
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // Act & Assert: Navigate to users page & verify owner is listed
      await page.getByRole("button", { name: "Users" }).click();
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(page.locator("tbody").locator("tr")).toHaveCount(1);
      await expect(page.getByText(`${owner.firstName} ${owner.lastName}`)).toBeVisible();
      await expect(page.getByText(owner.email)).toBeVisible();
      await expect(page.getByText("Owner")).toBeVisible();

      // Act & Assert: Submit invalid email invitation & verify validation error
      await page.getByRole("button", { name: "Invite users" }).click();
      await expect(page.getByRole("dialog", { name: "Invite user" })).toBeVisible();
      await page.getByRole("textbox", { name: "Email" }).fill("invalid-email");
      await page.getByRole("button", { name: "Send invite" }).click();
      await assertValidationError(context, "Email must be in a valid format and no longer than 100 characters.");
      await expect(page.getByRole("dialog")).toBeVisible();

      // Act & Assert: Invite member user & verify successful invitation
      await page.getByRole("textbox", { name: "Email" }).fill(memberUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await assertToastMessage(context, "Success", "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
      await expect(page.locator("tbody").locator("tr")).toHaveCount(2);
      await expect(page.getByText(`${memberUser.email}`)).toBeVisible();

      // Act & Assert: Invite admin user & verify successful invitation
      await page.getByRole("button", { name: "Invite users" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(adminUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await assertToastMessage(context, "Success", "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
      await expect(page.locator("tbody").locator("tr")).toHaveCount(3);
      await expect(page.getByText(`${adminUser.email}`)).toBeVisible();

      // Act & Assert: Change user role to Admin & verify role change is successful
      const adminUserRow = page.locator("tbody tr").filter({ hasText: adminUser.email });
      await adminUserRow.getByLabel("User actions").click();
      await page.getByRole("menuitem", { name: "Change role" }).click();
      await expect(page.getByRole("alertdialog", { name: "Change user role" })).toBeVisible();
      await page.getByRole("button", { name: "Member User role" }).click();
      await page.getByRole("option", { name: "Admin" }).click();
      await assertToastMessage(context, "Success", `User role updated successfully for ${adminUser.email}`);
      await expect(page.getByRole("alertdialog", { name: "Change user role" })).not.toBeVisible();
      await expect(adminUserRow).toContainText("Admin");

      // Act & Assert: Verify row is selected after role change & unselect to show invite button
      await expect(adminUserRow).toHaveAttribute("aria-selected", "true");
      await adminUserRow.click(); // Unselect the row
      await expect(adminUserRow).not.toHaveAttribute("aria-selected", "true");

      // Act & Assert: Attempt to invite duplicate user email & verify error message appears
      await page.getByRole("button", { name: "Invite users" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(memberUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await expect(
        page.getByText(`The email '${memberUser.email}' is already in use by another user on this tenant.`)
      ).toBeVisible();
      await page.getByRole("button", { name: "Cancel" }).click();
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Act & Assert: Check users table & verify invited users appear with correct roles
      const userTable = page.locator("tbody");
      await expect(userTable.locator("tr")).toHaveCount(3); // owner + 2 invited users
      await expect(userTable).toContainText(adminUser.email);
      await expect(userTable).toContainText(memberUser.email);
      await expect(page.getByText("Member").first()).toBeVisible();

      // Act & Assert: Test owner cannot delete or change role on themselves & verify restrictions
      const ownerRowSelf = page.locator("tbody tr").filter({ hasText: owner.email });
      await ownerRowSelf.getByLabel("User actions").click();
      await expect(page.getByRole("menuitem", { name: "Delete user" })).not.toBeVisible();
      await expect(page.getByRole("menuitem", { name: "Change role" })).toBeDisabled();
      await page.keyboard.press("Escape");

      // Act & Assert: Filter users by email search & verify filtered results display correctly
      await page.getByPlaceholder("Search").fill(adminUser.email);
      await page.keyboard.press("Enter"); // Trigger search immediately without debounce
      await expect(userTable.locator("tr")).toHaveCount(1);
      await expect(userTable).toContainText(adminUser.email);
      await expect(userTable).not.toContainText(owner.email);
      await expect(userTable).not.toContainText(memberUser.email);

      await page.getByPlaceholder("Search").clear();
      await page.keyboard.press("Enter"); // Trigger search immediately to show all results
      await expect(userTable.locator("tr")).toHaveCount(3);
      await expect(userTable).toContainText(adminUser.email);
      await expect(userTable).toContainText(memberUser.email);

      // Act & Assert: Filter users by role & verify role-based filtering works correctly
      await page.getByRole("button", { name: "Show filters" }).click();
      await page.getByRole("button", { name: "Any role User role" }).click();
      await page.getByRole("option", { name: "Owner" }).click();
      await expect(userTable.locator("tr")).toHaveCount(1); // After filtering by Owner role, should only have 1 owner (the original)
      await expect(userTable).toContainText(owner.email);
      await expect(userTable).not.toContainText(adminUser.email);
      await page.getByRole("button", { name: "Owner User role" }).click();
      await page.getByRole("option", { name: "Any role" }).click();
      await expect(userTable).toContainText(adminUser.email);

      // Act & Assert: Logout from owner account to test admin permissions
      await page.getByRole("button", { name: "Home" }).click();
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page).toHaveURL("/login?returnPath=%2Fadmin");

      // Act & Assert: Login as admin user & verify successful authentication
      await page.getByRole("textbox", { name: "Email" }).fill(adminUser.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login/verify?returnPath=%2Fadmin");
      await page.keyboard.type(getVerificationCode());
      await expect(page).toHaveURL("/admin");

      // Act & Assert: Complete admin user profile setup & verify profile form completion
      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();
      await page.getByRole("textbox", { name: "First name" }).fill(adminUser.firstName);
      await page.getByRole("textbox", { name: "Last name" }).fill(adminUser.lastName);
      await page.getByRole("textbox", { name: "Title" }).fill("Administrator");
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertToastMessage(context, "Success", "Profile updated successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // Act & Assert: Navigate to users page as admin & verify admin can see all users
      await page.getByRole("button", { name: "Users" }).click();
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(page.locator("tbody tr")).toHaveCount(3); // owner + admin + member

      // Act & Assert: Test admin cannot delete owner or change owner role & verify restrictions
      const ownerRow = page.locator("tbody tr").filter({ hasText: owner.email });
      await ownerRow.getByLabel("User actions").click();
      await expect(page.getByRole("menuitem", { name: "Delete user" })).not.toBeVisible();
      await expect(page.getByRole("menuitem", { name: "Change role" })).toBeDisabled();
      await page.keyboard.press("Escape");

      // Act & Assert: Test admin cannot delete other admin users & verify restrictions
      const currentAdminRow = page.locator("tbody tr").filter({ hasText: adminUser.email });
      await currentAdminRow.getByLabel("User actions").click();
      await expect(page.getByRole("menuitem", { name: "Delete user" })).not.toBeVisible();
      await expect(page.getByRole("menuitem", { name: "Change role" })).toBeDisabled();
      await page.keyboard.press("Escape");

      // Act & Assert: Test admin can access member user menu but cannot delete them
      const memberUserRow = page.locator("tbody tr").filter({ hasText: memberUser.email });
      await memberUserRow.getByLabel("User actions").click();
      await expect(page.getByRole("menuitem", { name: "Change role" })).toBeVisible();
      await expect(page.getByRole("menuitem", { name: "Delete user" })).not.toBeVisible(); // Delete not implemented yet
      await page.keyboard.press("Escape");
    });
  });

  test.describe("@comprehensive", () => {
    /**
     * DASHBOARD METRICS & URL FILTERING
     *
     * Tests the dashboard integration and filtering features including:
     * - Dashboard metrics integration (user count displays)
     * - URL-based filtering (active users link)
     * - Bulk delete readiness (verify UI elements not yet implemented)
     */
    test("should handle dashboard metrics and URL filtering", async ({ page }) => {
      const context = createTestContext(page);
      const owner = testUser();
      const user1 = testUser();
      const user2 = testUser();
      const user3 = testUser();

      // Act & Assert: Complete owner signup & navigate to users page
      await completeSignupFlow(page, expect, owner, context);
      await page.getByRole("button", { name: "Users" }).click();
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();

      // Act & Assert: Invite multiple users & verify they are added to the list
      const usersToInvite = [user1, user2, user3];
      for (const user of usersToInvite) {
        await page.getByRole("button", { name: "Invite users" }).click();
        await page.getByRole("textbox", { name: "Email" }).fill(user.email);
        await page.getByRole("button", { name: "Send invite" }).click();
        await assertToastMessage(context, "User invited successfully");
        await expect(page.getByRole("dialog")).not.toBeVisible();
      }

      const userTable = page.locator("tbody");
      await expect(userTable.locator("tr")).toHaveCount(4); // owner + 3 invited users

      // Act & Assert: Navigate to dashboard & verify user count metrics show correct numbers
      await page.getByRole("button", { name: "Home" }).click();
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await expect(page.getByText("Total users")).toBeVisible();
      await expect(page.getByText("4")).toBeVisible(); // Total: 1 owner + 3 invited users
      await expect(page.getByRole("link", { name: "View active users" })).toContainText("Active users");
      await expect(page.getByRole("link", { name: "View active users" })).toContainText("1"); // Active: Only owner is active
      await expect(page.getByRole("link", { name: "View invited users" })).toContainText("Invited users");
      await expect(page.getByRole("link", { name: "View invited users" })).toContainText("3"); // Invited: 3 invited users

      // Act & Assert: Click active users link & verify URL filtering shows only active users
      await page.getByRole("link", { name: "View active users" }).click();
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(page.locator("tbody tr")).toHaveCount(1); // Only active users (owner)
      expect(page.url()).toContain("userStatus=Active");

      // Act & Assert: Navigate back to all users & verify bulk operations UI elements are not yet implemented
      await page.getByRole("button", { name: "Users" }).first().click();
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(userTable.locator("tr")).toHaveCount(4); // All users visible again
      await expect(page.getByRole("button", { name: "Delete selected users" })).not.toBeVisible(); // Not implemented yet
      await expect(page.getByRole("button", { name: "Bulk actions" })).not.toBeVisible(); // Not implemented yet
    });
  });
});
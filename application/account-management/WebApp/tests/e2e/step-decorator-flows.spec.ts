import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { step } from "@shared/e2e/utils/step-decorator";
import { assertToastMessage, assertValidationError, createTestContext } from "@shared/e2e/utils/test-assertions";
import { completeSignupFlow, getVerificationCode, testUser } from "@shared/e2e/utils/test-data";

test.describe("Step Decorator Flow", () => {
  test.describe("@comprehensive", () => {
    /**
     * STEP DECORATOR WITH REAL USER MANAGEMENT WORKFLOW
     *
     * Tests the step decorator functionality using a complete user management scenario including:
     * - User invitation process with validation (invalid email, duplicate email)
     * - Role management (changing user roles from Member to Admin)
     * - Permission system (testing what owners vs admins can/cannot do)
     * - Search and filtering functionality (email search, role filtering)
     * - All wrapped in step decorators to demonstrate timing and structure
     */
    test("should handle complete user management workflow with step decorators and timing", async ({ page }) => {
      const context = createTestContext(page);
      const owner = testUser();
      const adminUser = testUser();
      const memberUser = testUser();

      await step("Complete owner signup & verify owner account creation")(async () => {
        await completeSignupFlow(page, expect, owner, context);
        await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      })();

      await step("Navigate to users page & verify owner is listed")(async () => {
        await page.getByRole("button", { name: "Users" }).click();
        await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
        await expect(page.locator("tbody").locator("tr")).toHaveCount(1);
        await expect(page.getByText(`${owner.firstName} ${owner.lastName}`)).toBeVisible();
        await expect(page.getByText(owner.email)).toBeVisible();
        await expect(page.getByText("Owner")).toBeVisible();
      })();

      await step("Submit invalid email invitation & verify validation error")(async () => {
        await page.getByRole("button", { name: "Invite users" }).click();
        await expect(page.getByRole("dialog", { name: "Invite user" })).toBeVisible();
        await page.getByRole("textbox", { name: "Email" }).fill("invalid-email");
        await page.getByRole("button", { name: "Send invite" }).click();
        await assertValidationError(context, "Email must be in a valid format and no longer than 100 characters.");
        await expect(page.getByRole("dialog")).toBeVisible();
      })();

      await step("Invite member user & verify successful invitation")(async () => {
        await page.getByRole("textbox", { name: "Email" }).fill(memberUser.email);
        await page.getByRole("button", { name: "Send invite" }).click();
        await assertToastMessage(context, "User invited successfully");
        await expect(page.getByRole("dialog")).not.toBeVisible();
        await expect(page.locator("tbody").locator("tr")).toHaveCount(2);
        await expect(page.getByText(`${memberUser.email}`)).toBeVisible();
      })();

      await step("Invite admin user & verify successful invitation")(async () => {
        await page.getByRole("button", { name: "Invite users" }).click();
        await page.getByRole("textbox", { name: "Email" }).fill(adminUser.email);
        await page.getByRole("button", { name: "Send invite" }).click();
        await assertToastMessage(context, "User invited successfully");
        await expect(page.getByRole("dialog")).not.toBeVisible();
        await expect(page.locator("tbody").locator("tr")).toHaveCount(3);
        await expect(page.getByText(`${adminUser.email}`)).toBeVisible();
      })();

      await step("Change user role to Admin & verify role change is successful")(async () => {
        const adminUserRow = page.locator("tbody tr").filter({ hasText: adminUser.email });
        await adminUserRow.getByLabel("User actions").click();
        await page.getByRole("menuitem", { name: "Change role" }).click();
        await expect(page.getByRole("alertdialog", { name: "Change user role" })).toBeVisible();
        await page.getByRole("button", { name: "Member User role" }).click();
        await page.getByRole("option", { name: "Admin" }).click();
        await assertToastMessage(context, `User role updated successfully for ${adminUser.email}`);
        await expect(page.getByRole("alertdialog", { name: "Change user role" })).not.toBeVisible();
        await expect(adminUserRow).toContainText("Admin");
      })();

      await step("Verify row is selected after role change & unselect to show invite button")(async () => {
        const adminUserRow = page.locator("tbody tr").filter({ hasText: adminUser.email });
        await expect(adminUserRow).toHaveAttribute("aria-selected", "true");
        await adminUserRow.click();
        await expect(adminUserRow).not.toHaveAttribute("aria-selected", "true");
      })();

      await step("Attempt to invite duplicate user email & verify error message appears")(async () => {
        await page.getByRole("button", { name: "Invite users" }).click();
        await page.getByRole("textbox", { name: "Email" }).fill(memberUser.email);
        await page.getByRole("button", { name: "Send invite" }).click();
        await assertToastMessage(context, 400, `The user with '${memberUser.email}' already exists.`);
        await page.getByRole("button", { name: "Cancel" }).click();
        await expect(page.getByRole("dialog")).not.toBeVisible();
      })();

      await step("Check users table & verify invited users appear with correct roles")(async () => {
        const userTable = page.locator("tbody");
        await expect(userTable.locator("tr")).toHaveCount(3);
        await expect(userTable).toContainText(adminUser.email);
        await expect(userTable).toContainText(memberUser.email);
        await expect(page.getByText("Member").first()).toBeVisible();
      })();

      await step("Test owner cannot delete or change role on themselves & verify restrictions")(async () => {
        const ownerRowSelf = page.locator("tbody tr").filter({ hasText: owner.email });
        await ownerRowSelf.getByLabel("User actions").click();
        await expect(page.getByRole("menuitem", { name: "Delete user" })).not.toBeVisible();
        await expect(page.getByRole("menuitem", { name: "Change role" })).toBeDisabled();
        await page.keyboard.press("Escape");
      })();

      await step("Filter users by email search & verify filtered results display correctly")(async () => {
        const userTable = page.locator("tbody");
        await page.getByPlaceholder("Search").fill(adminUser.email);
        await page.keyboard.press("Enter");
        await expect(userTable.locator("tr")).toHaveCount(1);
        await expect(userTable).toContainText(adminUser.email);
        await expect(userTable).not.toContainText(owner.email);
        await expect(userTable).not.toContainText(memberUser.email);

        await page.getByPlaceholder("Search").clear();
        await page.keyboard.press("Enter");
        await expect(userTable.locator("tr")).toHaveCount(3);
        await expect(userTable).toContainText(adminUser.email);
        await expect(userTable).toContainText(memberUser.email);
      })();

      await step("Filter users by role & verify role-based filtering works correctly")(async () => {
        const userTable = page.locator("tbody");
        await page.getByRole("button", { name: "Show filters" }).click();
        await page.getByRole("button", { name: "Any role User role" }).click();
        await page.getByRole("option", { name: "Owner" }).click();
        await expect(userTable.locator("tr")).toHaveCount(1);
        await expect(userTable).toContainText(owner.email);
        await expect(userTable).not.toContainText(adminUser.email);
        await page.getByRole("button", { name: "Owner User role" }).click();
        await page.getByRole("option", { name: "Any role" }).click();
        await expect(userTable).toContainText(adminUser.email);
      })();

      await step("Logout from owner account to test admin permissions")(async () => {
        await page.getByRole("button", { name: "Home" }).click();
        await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
        await page.getByRole("button", { name: "User profile menu" }).click();
        await page.getByRole("menuitem", { name: "Log out" }).click();
        await expect(page).toHaveURL("/login?returnPath=%2Fadmin");
      })();

      await step("Login as admin user & verify successful authentication")(async () => {
        await page.getByRole("textbox", { name: "Email" }).fill(adminUser.email);
        await page.getByRole("button", { name: "Continue" }).click();
        await expect(page).toHaveURL("/login/verify?returnPath=%2Fadmin");
        await page.keyboard.type(getVerificationCode());
        await expect(page).toHaveURL("/admin");
      })();

      await step("Complete admin user profile setup & verify profile form completion")(async () => {
        await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();
        await page.getByRole("textbox", { name: "First name" }).fill(adminUser.firstName);
        await page.getByRole("textbox", { name: "Last name" }).fill(adminUser.lastName);
        await page.getByRole("textbox", { name: "Title" }).fill("Administrator");
        await page.getByRole("button", { name: "Save changes" }).click();
        await assertToastMessage(context, "Profile updated successfully");
        await expect(page.getByRole("dialog")).not.toBeVisible();
        await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      })();

      await step("Navigate to users page as admin & verify admin can see all users")(async () => {
        await page.getByRole("button", { name: "Users" }).click();
        await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
        await expect(page.locator("tbody tr")).toHaveCount(3);
      })();

      await step("Test admin cannot delete owner or change owner role & verify restrictions")(async () => {
        const ownerRow = page.locator("tbody tr").filter({ hasText: owner.email });
        await ownerRow.getByLabel("User actions").click();
        await expect(page.getByRole("menuitem", { name: "Delete user" })).not.toBeVisible();
        await expect(page.getByRole("menuitem", { name: "Change role" })).toBeDisabled();
        await page.keyboard.press("Escape");
      })();

      await step("Test admin cannot delete other admin users & verify restrictions")(async () => {
        const currentAdminRow = page.locator("tbody tr").filter({ hasText: adminUser.email });
        await currentAdminRow.getByLabel("User actions").click();
        await expect(page.getByRole("menuitem", { name: "Delete user" })).not.toBeVisible();
        await expect(page.getByRole("menuitem", { name: "Change role" })).toBeDisabled();
        await page.keyboard.press("Escape");
      })();

      await step("Test admin can access member user menu but cannot delete them")(async () => {
        const memberUserRow = page.locator("tbody tr").filter({ hasText: memberUser.email });
        await memberUserRow.getByLabel("User actions").click();
        await expect(page.getByRole("menuitem", { name: "Change role" })).toBeVisible();
        await expect(page.getByRole("menuitem", { name: "Delete user" })).not.toBeVisible();
        await page.keyboard.press("Escape");
      })();
    });
  });
});

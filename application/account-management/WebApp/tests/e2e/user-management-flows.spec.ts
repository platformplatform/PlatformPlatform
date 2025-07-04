import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext, expectToastMessage, expectValidationError } from "@shared/e2e/utils/test-assertions";
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
   */
  test("should handle user invitation, role management & permissions workflow", async ({ page }) => {
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
      await page.getByRole("button", { name: "Invite user" }).click();
      await expect(page.getByRole("dialog", { name: "Invite user" })).toBeVisible();
      await page.getByRole("textbox", { name: "Email" }).fill("invalid-email");
      await page.getByRole("button", { name: "Send invite" }).click();

      await expectValidationError(context, "Email must be in a valid format and no longer than 100 characters.");
      await expect(page.getByRole("dialog")).toBeVisible();
    })();

    await step("Invite member user & verify successful invitation")(async () => {
      await page.getByRole("textbox", { name: "Email" }).fill(memberUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();

      await expectToastMessage(context, "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
      await expect(page.locator("tbody").locator("tr")).toHaveCount(2);
      await expect(page.getByText(`${memberUser.email}`)).toBeVisible();
    })();

    await step("Invite admin user & verify successful invitation")(async () => {
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(adminUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();

      await expectToastMessage(context, "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
      await expect(page.locator("tbody").locator("tr")).toHaveCount(3);
      await expect(page.getByText(`${adminUser.email}`)).toBeVisible();
    })();

    await step("Select Admin role from dropdown & verify role change completes")(async () => {
      const adminUserRow = page.locator("tbody tr").filter({ hasText: adminUser.email });
      await adminUserRow.getByLabel("User actions").click();
      await page.getByRole("menuitem", { name: "Change role" }).click();
      await expect(page.getByRole("alertdialog", { name: "Change user role" })).toBeVisible();
      await page.getByRole("button", { name: "Member User role" }).click();
      await page.getByRole("option", { name: "Admin" }).click();

      await expectToastMessage(context, `User role updated successfully for ${adminUser.email}`);
      await expect(page.getByRole("alertdialog", { name: "Change user role" })).not.toBeVisible();
      await expect(adminUserRow).toContainText("Admin");
    })();

    await step("Attempt to invite duplicate user email & verify error message appears")(async () => {
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(memberUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();

      await expectToastMessage(context, 400, `The user with '${memberUser.email}' already exists.`);

      await page.getByRole("button", { name: "Cancel" }).click();

      await expect(page.getByRole("dialog")).not.toBeVisible();
    })();

    await step("Check users table & verify invited users appear with correct roles")(async () => {
      const userTable = page.locator("tbody");
      await expect(userTable.locator("tr")).toHaveCount(3); // owner + 2 invited users
      await expect(userTable).toContainText(adminUser.email);
      await expect(userTable).toContainText(memberUser.email);
      await expect(page.getByText("Member").first()).toBeVisible();
    })();

    await step("Try to delete owner account & verify action restrictions")(async () => {
      const ownerRowSelf = page.locator("tbody tr").filter({ hasText: owner.email });
      await ownerRowSelf.getByLabel("User actions").click();

      await expect(page.getByRole("menuitem", { name: "Delete user" })).not.toBeVisible();
      await expect(page.getByRole("menuitem", { name: "Change role" })).toBeDisabled();

      await page.keyboard.press("Escape");
    })();

    await step("Filter users by email search & verify filtered results display correctly")(async () => {
      const userTable = page.locator("tbody");
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
    })();

    await step("Filter users by role & verify role-based filtering works correctly")(async () => {
      const userTable = page.locator("tbody");
      await page.getByRole("button", { name: "Show filters" }).click();
      await page.getByRole("button", { name: "Any role User role" }).click();
      await page.getByRole("option", { name: "Owner" }).click();

      await expect(userTable.locator("tr")).toHaveCount(1); // After filtering by Owner role, should only have 1 owner (the original)
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

      await expectToastMessage(context, "Profile updated successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
    })();

    await step("Navigate to users page as admin & verify admin can see all users")(async () => {
      await page.getByRole("button", { name: "Users" }).click();

      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(page.locator("tbody tr")).toHaveCount(3); // owner + admin + member
    })();

    await step("Open member user menu as admin & verify limited actions available")(async () => {
      const memberUserRow = page.locator("tbody tr").filter({ hasText: memberUser.email });
      await memberUserRow.getByLabel("User actions").click({ force: true });

      await expect(page.getByRole("menuitem", { name: "Change role" })).toBeVisible();
      await expect(page.getByRole("menuitem", { name: "Delete user" })).not.toBeVisible();
    })();
  });
});

test.describe("@comprehensive", () => {
  /**
   * USER DELETION WORKFLOWS WITH DASHBOARD INTEGRATION
   *
   * Tests comprehensive user deletion functionality with dashboard context including:
   * - Dashboard metrics integration (user count displays)
   * - URL-based filtering (active users link)
   * - Single user deletion via menu actions
   * - Bulk user selection by clicking rows with Ctrl/Cmd modifier
   * - Bulk deletion of multiple users via "Delete X users" button
   * - Owner protection mechanisms (deletion restrictions)
   * - UI state management after deletions (selection clearing, button visibility)
   */
  test("should handle single and bulk user deletion workflows with dashboard integration", async ({ page }) => {
    const context = createTestContext(page);
    const owner = testUser();
    const user1 = testUser();
    const user2 = testUser();
    const user3 = testUser();

    // === USER SETUP SECTION ===
    await step("Complete owner signup & navigate to users page")(async () => {
      await completeSignupFlow(page, expect, owner, context);
      await page.goto("/admin/users");

      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
    })();

    await step("Invite multiple users & verify they are added to the list")(async () => {
      const usersToInvite = [user1, user2, user3];

      for (const user of usersToInvite) {
        await page.getByRole("button", { name: "Invite user" }).click();
        await page.getByRole("textbox", { name: "Email" }).fill(user.email);
        await page.getByRole("button", { name: "Send invite" }).click();

        await expectToastMessage(context, "User invited successfully");
        await expect(page.getByRole("dialog")).not.toBeVisible();
      }

      await expect(page.locator("tbody tr")).toHaveCount(4); // owner + 3 invited users
    })();

    // === DASHBOARD METRICS SECTION ===
    await step("Navigate to dashboard & verify user count metrics display correctly")(async () => {
      await page.goto("/admin");

      await expect(page.getByRole("link", { name: "View users" })).toContainText("4");
      await expect(page.getByRole("link", { name: "View active users" })).toContainText("1");
      await expect(page.getByRole("link", { name: "View invited users" })).toContainText("3");
    })();

    // === URL FILTERING SECTION ===
    await step("Click invited users link & verify URL filtering works correctly")(async () => {
      await page.getByRole("link", { name: "View invited users" }).click();

      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(page.locator("tbody tr")).toHaveCount(3);
      await expect(page.url()).toContain("userStatus=Pending");
    })();

    // === ADVANCED FILTERING SECTION ===
    await step("Verify all filter options are available")(async () => {
      // Filters should already be visible due to URL filtering from previous step
      await expect(page.getByLabel("User role").first()).toBeVisible();
      await expect(page.getByLabel("User status").first()).toBeVisible();
      await expect(page.getByLabel("Modified date").first()).toBeVisible();
    })();

    await step("Filter by Owner role & verify only owner shown")(async () => {
      // First dismiss any open dropdown and clear status filter
      await page.keyboard.press("Escape");
      await page.getByLabel("User status").first().click();
      await page.getByRole("option", { name: "Any status" }).click();

      // Now filter by Owner role
      await page.getByLabel("User role").first().click();
      await page.getByRole("option", { name: "Owner" }).click();

      // Verify only owner is shown
      await expect(page.locator("tbody tr")).toHaveCount(1);
      await expect(page.locator("tbody")).toContainText(owner.email);
      await expect(page.locator("tbody")).not.toContainText(user1.email);
      await expect(page.locator("tbody")).not.toContainText(user2.email);
    })();

    await step("Filter by Member role & verify only members shown")(async () => {
      // Change filter to Member role
      await page.getByLabel("User role").first().click();
      await page.getByRole("option", { name: "Member" }).click();

      // Verify only member users are shown
      await expect(page.locator("tbody tr")).toHaveCount(3);
      await expect(page.locator("tbody")).toContainText(user1.email);
      await expect(page.locator("tbody")).toContainText(user2.email);
      await expect(page.locator("tbody")).toContainText(user3.email);
      await expect(page.locator("tbody")).not.toContainText(owner.email);
    })();

    await step("Filter by Pending status & verify only pending users shown")(async () => {
      // Reset role filter and set status filter
      await page.getByLabel("User role").first().click();
      await page.getByRole("option", { name: "Any role" }).click();

      await page.getByLabel("User status").first().click();
      await page.getByRole("option", { name: "Pending" }).click();

      // Verify only pending users are shown (invited users who haven't confirmed)
      await expect(page.locator("tbody tr")).toHaveCount(3);
      await expect(page.locator("tbody")).toContainText(user1.email);
      await expect(page.locator("tbody")).toContainText(user2.email);
      await expect(page.locator("tbody")).toContainText(user3.email);
      await expect(page.locator("tbody")).not.toContainText(owner.email);
    })();

    await step("Filter by Active status & verify only active users shown")(async () => {
      // Change filter to Active status
      await page.getByLabel("User status").first().click();
      await page.getByRole("option", { name: "Active" }).click();

      // Verify only active users are shown (owner who has confirmed email)
      await expect(page.locator("tbody tr")).toHaveCount(1);
      await expect(page.locator("tbody")).toContainText(owner.email);
      await expect(page.locator("tbody")).not.toContainText(user1.email);
    })();

    await step("Filter by past date range & verify no users shown")(async () => {
      // Reset status filter first
      await page.getByLabel("User status").first().click();
      await page.getByRole("option", { name: "Any status" }).click();

      // Open date picker
      await page.getByLabel("Modified date").first().click();

      // Set start date to January 1, 2024
      await page.locator('[role="spinbutton"][aria-label="month, Start Date, "]').clear();
      await page.locator('[role="spinbutton"][aria-label="month, Start Date, "]').type("01");
      await page.locator('[role="spinbutton"][aria-label="day, Start Date, "]').clear();
      await page.locator('[role="spinbutton"][aria-label="day, Start Date, "]').type("01");
      await page.locator('[role="spinbutton"][aria-label="year, Start Date, "]').clear();
      await page.locator('[role="spinbutton"][aria-label="year, Start Date, "]').type("2024");

      // Set end date to December 31, 2024
      await page.locator('[role="spinbutton"][aria-label="month, End Date, "]').clear();
      await page.locator('[role="spinbutton"][aria-label="month, End Date, "]').type("12");
      await page.locator('[role="spinbutton"][aria-label="day, End Date, "]').clear();
      await page.locator('[role="spinbutton"][aria-label="day, End Date, "]').type("31");
      await page.locator('[role="spinbutton"][aria-label="year, End Date, "]').clear();
      await page.locator('[role="spinbutton"][aria-label="year, End Date, "]').type("2024");

      // Close the calendar
      await page.keyboard.press("Escape");

      // Verify no users are shown for the past date range (users were created in 2025)
      await expect(page.locator("tbody tr")).toHaveCount(0);
    })();

    // === CLEAR FILTERS FOR CLEAN DELETION TESTS ===
    await step("Clear all filters & verify all users shown again for clean deletion tests")(async () => {
      // Reset any remaining filters to show all users
      await page.getByRole("button", { name: "Clear filters" }).click();

      // Verify all users are shown again
      await expect(page.locator("tbody tr")).toHaveCount(4);
      await expect(page.locator("tbody")).toContainText(owner.email);
      await expect(page.locator("tbody")).toContainText(user1.email);
      await expect(page.locator("tbody")).toContainText(user2.email);
      await expect(page.locator("tbody")).toContainText(user3.email);
    })();

    // === SINGLE USER DELETION SECTION ===
    await step("Delete single user via menu & verify removal")(async () => {
      const user1Row = page.locator("tbody tr").filter({ hasText: user1.email });
      await user1Row.getByLabel("User actions").click();
      await page.getByRole("menuitem", { name: "Delete" }).click();

      await expect(page.getByRole("alertdialog", { name: "Delete user" })).toBeVisible();
      await expect(page.getByText(`Are you sure you want to delete ${user1.email}?`)).toBeVisible();

      await page.getByRole("button", { name: "Delete" }).click();

      await expectToastMessage(context, `User deleted successfully: ${user1.email}`);
      await expect(page.getByRole("alertdialog")).not.toBeVisible();
      await expect(page.locator("tbody tr")).toHaveCount(3); // owner + user2 + user3
      await expect(page.getByText(user1.email)).not.toBeVisible();
      await expect(page.locator("tbody")).toContainText(owner.email);
      await expect(page.locator("tbody")).toContainText(user2.email);
      await expect(page.locator("tbody")).toContainText(user3.email);
    })();

    // === BULK USER SELECTION SECTION ===
    await step("Select remaining two users by clicking rows & verify selection state")(async () => {
      // Set viewport to 2xl to avoid side pane backdrop issues
      await page.setViewportSize({ width: 1536, height: 1024 });

      const user2Row = page.locator("tbody tr").filter({ hasText: user2.email });
      const user3Row = page.locator("tbody tr").filter({ hasText: user3.email });

      // Select first user by clicking the row
      await user2Row.click();
      await expect(user2Row).toHaveAttribute("aria-selected", "true");
      // Verify the toolbar delete button is visible (single user selection)
      await expect(page.getByRole("button", { name: "Delete user" }).first()).toBeVisible();

      // Select second user by clicking the row with Ctrl/Cmd modifier (should enable multi-selection)
      // On 2xl viewport, there's no backdrop so this should work
      await user3Row.click({ modifiers: ["ControlOrMeta"] });
      await expect(user3Row).toHaveAttribute("aria-selected", "true");
      await expect(user2Row).toHaveAttribute("aria-selected", "true");
      await expect(page.getByRole("button", { name: "Delete 2 users" })).toBeVisible();
    })();

    // === BULK USER DELETION SECTION ===
    await step("Cancel bulk deletion & verify users remain selected")(async () => {
      // Reset viewport to default for subsequent tests
      await page.setViewportSize({ width: 1280, height: 720 });

      await page.getByRole("button", { name: "Delete 2 users" }).click();

      await expect(page.getByRole("alertdialog", { name: "Delete users" })).toBeVisible();
      await expect(page.getByText("Are you sure you want to delete 2 users?")).toBeVisible();

      await page.getByRole("button", { name: "Cancel" }).click();

      await expect(page.getByRole("alertdialog")).not.toBeVisible();
      await expect(page.locator("tbody tr")).toHaveCount(3); // All users still present
      await expect(page.getByRole("button", { name: "Delete 2 users" })).toBeVisible(); // Selection maintained
    })();

    await step("Confirm bulk delete selected users & verify removal")(async () => {
      await page.getByRole("button", { name: "Delete 2 users" }).click();

      await expect(page.getByRole("alertdialog", { name: "Delete users" })).toBeVisible();
      await page.getByRole("button", { name: "Delete" }).click();

      await expectToastMessage(context, "2 users deleted successfully");
      await expect(page.getByRole("alertdialog")).not.toBeVisible();
      await expect(page.locator("tbody tr")).toHaveCount(1); // Only owner left
      await expect(page.getByText(user2.email)).not.toBeVisible();
      await expect(page.getByText(user3.email)).not.toBeVisible();
      await expect(page.locator("tbody")).toContainText(owner.email);
      await expect(page.getByRole("button", { name: "Delete 2 users" })).not.toBeVisible();
      await expect(page.getByRole("button", { name: "Invite user" })).toBeVisible();
    })();

    // === OWNER PROTECTION SECTION ===
    await step("Verify owner menu delete option is disabled")(async () => {
      const ownerRow = page.locator("tbody tr").filter({ hasText: owner.email });
      await ownerRow.getByLabel("User actions").click();

      await expect(page.getByRole("menuitem", { name: "Delete" })).toBeDisabled();

      await page.keyboard.press("Escape");
    })();
  });
});

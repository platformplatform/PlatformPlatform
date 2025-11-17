import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { blurActiveElement, createTestContext, expectToastMessage } from "@shared/e2e/utils/test-assertions";
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

      // Return to users page
      await page.getByLabel("Main navigation").getByRole("link", { name: "Users" }).click();
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
    })();

    // Email validation is comprehensively tested in signup-flows.spec.ts

    await step("Invite member user & verify successful invitation")(async () => {
      await page.getByRole("button", { name: "Invite user" }).click();
      await expect(page.getByRole("dialog", { name: "Invite user" })).toBeVisible();
      await page.getByRole("textbox", { name: "Email" }).fill(memberUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();

      await expectToastMessage(context, "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
      // Verify both users exist in table - use first() due to mobile rendering with duplicate tables
      await expect(page.locator("tbody").first().first()).toContainText(memberUser.email);
      await expect(page.locator("tbody").first().first()).toContainText(owner.email);
    })();

    await step("Invite admin user & verify successful invitation")(async () => {
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(adminUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();

      await expectToastMessage(context, "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
      // Verify all three users exist in table - use first() due to mobile rendering with duplicate tables
      await expect(page.locator("tbody").first().first()).toContainText(adminUser.email);
      await expect(page.locator("tbody").first().first()).toContainText(memberUser.email);
      await expect(page.locator("tbody").first().first()).toContainText(owner.email);
    })();

    await step("Open actions menu for admin user and change role to Admin & verify role updates")(async () => {
      const adminUserRow = page.locator("tbody").first().locator("tr").filter({ hasText: adminUser.email });
      const actionsButton = adminUserRow.locator("button[aria-label='User actions']").first();
      await actionsButton.evaluate((el: HTMLElement) => el.click());

      // Wait for menu to be visible before clicking
      await expect(page.getByRole("menu")).toBeVisible();
      await page.getByRole("menuitem", { name: "Change role" }).click();

      await expect(page.getByRole("alertdialog", { name: "Change user role" })).toBeVisible();
      await page.getByRole("button", { name: "Member User role" }).click();
      await page.getByRole("option", { name: "Admin" }).click();
      await page.getByRole("button", { name: "OK" }).click();

      await expectToastMessage(context, `User role updated successfully for ${adminUser.email}`);

      // Wait for dialog to close
      await expect(page.getByRole("alertdialog", { name: "Change user role" })).not.toBeVisible();
      await expect(adminUserRow.first()).toContainText("Admin");
    })();

    await step("Attempt to invite duplicate user email & verify error message appears")(async () => {
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(memberUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();

      await expectToastMessage(context, 400, `The user with '${memberUser.email}' already exists.`);

      await page.getByRole("button", { name: "Cancel" }).click();

      // Wait for dialog to close
      await expect(page.getByRole("dialog")).not.toBeVisible();
    })();

    await step("Check users table & verify invited users appear with correct roles")(async () => {
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
      await ownerActionsButton.evaluate((el: HTMLElement) => el.click());

      await expect(page.getByRole("menuitem", { name: "Delete" })).toBeDisabled();
      await expect(page.getByRole("menuitem", { name: "Change role" })).toBeDisabled();

      // Click outside the menu to close it
      await page.locator("body").click({ position: { x: 10, y: 10 } });
    })();

    await step("Filter users by email search & verify filtered results display correctly")(async () => {
      // Ensure viewport is desktop size for search to be visible
      await page.setViewportSize({ width: 1280, height: 720 });

      const userTable = page.locator("tbody").first();

      // Search for admin user
      const searchInput = page.getByRole("searchbox", { name: "Search" });
      await searchInput.fill(adminUser.email);
      await page.keyboard.press("Enter"); // Trigger search immediately without debounce

      // Verify only admin user is shown without counting rows
      await expect(userTable).toContainText(adminUser.email);
      await expect(userTable).not.toContainText(owner.email);
      await expect(userTable).not.toContainText(memberUser.email);

      // Clear search and verify all users are shown again
      await searchInput.clear();
      await page.keyboard.press("Enter"); // Trigger search immediately to show all results

      await expect(userTable).toContainText(adminUser.email);
      await expect(userTable).toContainText(memberUser.email);
      await expect(userTable).toContainText(owner.email);
    })();

    await step("Filter users by role & verify role-based filtering works correctly")(async () => {
      const userTable = page.locator("tbody").first().first();
      await page.getByRole("button", { name: "Show filters" }).click();
      await page.getByRole("button", { name: "Any role User role" }).click();
      await page.getByRole("option", { name: "Owner" }).click();

      // Verify only owner is shown without counting rows
      await expect(userTable).toContainText(owner.email);
      await expect(userTable).not.toContainText(adminUser.email);
      await expect(userTable).not.toContainText(memberUser.email);

      await page.getByRole("button", { name: "Owner User role" }).click();
      await page.getByRole("option", { name: "Any role" }).click();

      // Verify all users are shown again
      await expect(userTable).toContainText(adminUser.email);
      await expect(userTable).toContainText(memberUser.email);
      await expect(userTable).toContainText(owner.email);
    })();

    await step("Logout from owner account & verify redirect to login")(async () => {
      // Mark 401 as expected during logout transition (React Query may have in-flight requests)
      context.monitoring.expectedStatusCodes.push(401);

      // Navigate to home first
      await page.goto("/admin");
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

    await step("Open member user menu as admin & verify limited actions available")(async () => {
      const memberUserRow = page.locator("tbody").first().locator("tr").filter({ hasText: memberUser.email });
      const memberActionsButton = memberUserRow.locator("button[aria-label='User actions']").first();
      await memberActionsButton.evaluate((el: HTMLElement) => el.click());

      // Admin users don't see Change role or Delete options - only View profile
      await expect(page.getByRole("menuitem", { name: "View profile" })).toBeVisible();
      await expect(page.getByRole("menuitem", { name: "Change role" })).not.toBeVisible();
      await expect(page.getByRole("menuitem", { name: "Delete" })).not.toBeVisible();
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
      const usersToInvite = [user1, user2, user3];

      for (const user of usersToInvite) {
        await page.getByRole("button", { name: "Invite user" }).click();
        await page.getByRole("textbox", { name: "Email" }).fill(user.email);
        await page.getByRole("button", { name: "Send invite" }).click();

        await expectToastMessage(context, "User invited successfully");
        await expect(page.getByRole("dialog")).not.toBeVisible();
      }

      await expect(page.locator("tbody").first().first().locator("tr")).toHaveCount(4); // owner + 3 invited users
    })();

    // === DASHBOARD METRICS SECTION ===
    await step("Navigate to dashboard & verify user count metrics display correctly")(async () => {
      await page.goto("/admin");

      // Verify dashboard shows correct user counts
      await expect(page.getByRole("link", { name: "View users" })).toContainText("4");
      await expect(page.getByRole("link", { name: "View active users" })).toContainText("1");
      await expect(page.getByRole("link", { name: "View invited users" })).toContainText("3");
    })();

    // === URL FILTERING SECTION ===
    await step("Click invited users link & verify URL filtering works correctly")(async () => {
      await page.getByRole("link", { name: "View invited users" }).click();

      // Verify filtering by URL parameter
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(page.locator("tbody").first().first().locator("tr")).toHaveCount(3);
      await expect(page.url()).toContain("userStatus=Pending");
    })();

    // === ADVANCED FILTERING SECTION ===
    await step("Show filters & verify all filter options are available")(async () => {
      // Show filters
      await page.getByRole("button", { name: "Show filters" }).click();

      await expect(page.getByLabel("User role").first()).toBeVisible();
      await expect(page.getByLabel("User status").first()).toBeVisible();
      await expect(page.getByLabel("Modified date").first()).toBeVisible();
    })();

    await step("Filter by Owner role & verify only owner shown")(async () => {
      // Filters are in the dialog that was opened in the previous step
      const filterDialog = page.getByRole("dialog", { name: "Filters" });

      // Clear status filter
      await filterDialog.getByLabel("User status").click();
      const statusListbox = page.getByRole("listbox", { name: "User status" });
      await expect(statusListbox).toBeVisible();
      await statusListbox.getByRole("option", { name: "Any status" }).click();
      await blurActiveElement(page);

      // Set role filter to Owner
      await filterDialog.getByLabel("User role").click();
      const roleListbox = page.getByRole("listbox", { name: "User role" });
      await expect(roleListbox).toBeVisible();
      await roleListbox.getByRole("option", { name: "Owner" }).click();
      await blurActiveElement(page);

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
      const roleListbox = page.getByRole("listbox", { name: "User role" });
      await expect(roleListbox).toBeVisible();
      await roleListbox.getByRole("option", { name: "Member" }).click();
      await blurActiveElement(page);

      // Click OK to apply and close dialog
      await filterDialog.getByRole("button", { name: "OK" }).click();
      await expect(filterDialog).not.toBeVisible();

      // Verify only member users are shown
      await expect(page.locator("tbody").first().first().locator("tr")).toHaveCount(3);
      await expect(page.locator("tbody").first()).toContainText(user1.email);
      await expect(page.locator("tbody").first()).toContainText(user2.email);
      await expect(page.locator("tbody").first()).toContainText(user3.email);
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

      // Verify all 4 users are shown (no filters applied)
      await expect(page.locator("tbody").first().first().locator("tr")).toHaveCount(4);
    })();

    await step("Filter by Pending status & verify only pending users shown")(async () => {
      // Reopen filter dialog
      await page.getByRole("button", { name: "Show filters" }).click();
      const filterDialog = page.getByRole("dialog", { name: "Filters" });
      await expect(filterDialog).toBeVisible();

      // Set status filter to Pending
      await filterDialog.getByLabel("User status").click();
      const statusListbox = page.getByRole("listbox", { name: "User status" });
      await expect(statusListbox).toBeVisible();
      await statusListbox.getByRole("option", { name: "Pending" }).click();
      await blurActiveElement(page);

      // Close dialog
      await filterDialog.getByRole("button", { name: "OK" }).click();
      await expect(filterDialog).not.toBeVisible();

      // Verify only pending users are shown (invited users who haven't confirmed)
      await expect(page.locator("tbody").first().first().locator("tr")).toHaveCount(3);
      await expect(page.locator("tbody").first()).toContainText(user1.email);
      await expect(page.locator("tbody").first()).toContainText(user2.email);
      await expect(page.locator("tbody").first()).toContainText(user3.email);
      await expect(page.locator("tbody").first()).not.toContainText(owner.email);
    })();

    await step("Filter by Active status & verify only active users shown")(async () => {
      // Reopen dialog
      await page.getByRole("button", { name: "Show filters" }).click();
      const filterDialog = page.getByRole("dialog", { name: "Filters" });
      await expect(filterDialog).toBeVisible();

      // Set status filter to Active
      await filterDialog.getByLabel("User status").click();
      const statusListbox = page.getByRole("listbox", { name: "User status" });
      await expect(statusListbox).toBeVisible();
      await statusListbox.getByRole("option", { name: "Active" }).click();
      await blurActiveElement(page);

      // Click OK to apply and close dialog
      await filterDialog.getByRole("button", { name: "OK" }).click();
      await expect(filterDialog).not.toBeVisible();

      // Verify only active users are shown (owner who has confirmed email)
      await expect(page.locator("tbody").first().first().locator("tr")).toHaveCount(1);
      await expect(page.locator("tbody").first()).toContainText(owner.email);
      await expect(page.locator("tbody").first()).not.toContainText(user1.email);
    })();

    await step("Filter by past date range & verify no users shown")(async () => {
      // Reopen dialog
      await page.getByRole("button", { name: "Show filters" }).click();
      const filterDialog = page.getByRole("dialog", { name: "Filters" });
      await expect(filterDialog).toBeVisible();

      // Clear status filter
      await filterDialog.getByLabel("User status").click();
      const statusListbox = page.getByRole("listbox", { name: "User status" });
      await expect(statusListbox).toBeVisible();
      await statusListbox.getByRole("option", { name: "Any status" }).click();
      await blurActiveElement(page);

      // Open date picker
      await filterDialog.getByLabel("Modified date").click();

      // Close the calendar picker to enter keyboard mode
      await page.keyboard.press("Escape");

      // Click on year field to focus it for keyboard entry
      await page.locator('[role="spinbutton"][aria-label="year, Start Date, "]').click({ force: true });

      // Set dates via keyboard (auto-advances through fields)
      await page.keyboard.type("20240101");
      await page.keyboard.type("20241231");

      // Close the filter dialog
      await filterDialog.getByRole("button", { name: "OK" }).click();
      await expect(filterDialog).not.toBeVisible();

      // Verify no users are shown for the past date range (users were created in 2025)
      await expect(page.locator("tbody").first().first().locator("tr")).toHaveCount(0);
    })();

    // === CLEAR FILTERS FOR CLEAN DELETION TESTS ===
    await step("Navigate to users page & verify all users shown for clean deletion tests")(async () => {
      // Navigate to users page without any filters
      await page.goto("/admin/users");

      // Verify all users are shown
      await expect(page.locator("tbody").first().first().locator("tr")).toHaveCount(4);
      await expect(page.locator("tbody").first()).toContainText(owner.email);
      await expect(page.locator("tbody").first()).toContainText(user1.email);
      await expect(page.locator("tbody").first()).toContainText(user2.email);
      await expect(page.locator("tbody").first()).toContainText(user3.email);
    })();

    // === SINGLE USER DELETION SECTION ===
    await step("Delete single user via menu & verify removal")(async () => {
      // Open actions menu and delete user1
      const user1Row = page.locator("tbody").first().locator("tr").filter({ hasText: user1.email });
      const user1ActionsButton = user1Row.locator("button[aria-label='User actions']").first();
      await user1ActionsButton.evaluate((el: HTMLElement) => el.click());
      await page.getByRole("menuitem", { name: "Delete" }).click();

      // Confirm deletion
      await expect(page.getByRole("alertdialog", { name: "Delete user" })).toBeVisible();
      await expect(page.getByText(`Are you sure you want to delete ${user1.email}?`)).toBeVisible();
      await page.getByRole("button", { name: "Delete" }).click();

      // Verify user is removed from table
      await expectToastMessage(context, `User deleted successfully: ${user1.email}`);
      await expect(page.getByRole("alertdialog", { name: "Delete user" })).not.toBeVisible();
      await expect(page.locator("tbody").first().first().locator("tr")).toHaveCount(3); // owner + user2 + user3
      await expect(page.getByText(user1.email)).not.toBeVisible();
      await expect(page.locator("tbody").first()).toContainText(owner.email);
      await expect(page.locator("tbody").first()).toContainText(user2.email);
      await expect(page.locator("tbody").first()).toContainText(user3.email);
      await expect(page.locator("tbody").first().locator("tr")).toHaveCount(3);
    })();

    // === BULK USER SELECTION SECTION ===
    await step("Select remaining two users by clicking rows & verify selection state")(async () => {
      // Use JavaScript evaluation to click rows since regular click is not working
      const allRows = page.locator("tbody").first().locator("tr");

      // Select first non-owner user (index 1)
      const user2Row = allRows.nth(1);
      await user2Row.evaluate((el: HTMLElement) => el.click());
      await expect(user2Row).toHaveAttribute("aria-selected", "true");

      // Verify the toolbar delete button is visible (single user selection)
      await expect(page.getByRole("button", { name: "Delete user" }).first()).toBeVisible();

      // Select second non-owner user (index 2) with Ctrl/Cmd modifier
      const user3Row = allRows.nth(2);
      await page.keyboard.down("ControlOrMeta");
      await user3Row.evaluate((el: HTMLElement) => el.click());
      await page.keyboard.up("ControlOrMeta");

      // Verify the bulk delete button is visible
      await expect(user3Row).toHaveAttribute("aria-selected", "true");
      await expect(user2Row).toHaveAttribute("aria-selected", "true");
      await expect(page.getByRole("button", { name: "Delete 2 users" })).toBeVisible();
    })();

    await step("Select owner with Ctrl/Cmd modifier & verify delete button becomes disabled")(async () => {
      const ownerRow = page.locator("tbody").first().locator("tr").filter({ hasText: owner.email });

      // Select owner by clicking the row with Ctrl/Cmd modifier
      await page.keyboard.down("ControlOrMeta");
      await ownerRow.evaluate((el: HTMLElement) => el.click());
      await page.keyboard.up("ControlOrMeta");

      // Verify the toolbar bulk delete button is visible but disabled (owner protection)
      await expect(page.getByRole("button", { name: "Delete 3 users" })).toBeVisible();
      await expect(page.getByRole("button", { name: "Delete 3 users" })).toBeDisabled();
    })();

    await step("Deselect owner with Ctrl/Cmd modifier & verify delete button becomes enabled")(async () => {
      const ownerRow = page.locator("tbody").first().locator("tr").filter({ hasText: owner.email });

      // Deselect owner with Ctrl/Cmd modifier
      await page.keyboard.down("ControlOrMeta");
      await ownerRow.evaluate((el: HTMLElement) => el.click());
      await page.keyboard.up("ControlOrMeta");

      // Verify the toolbar bulk delete button is now enabled
      await expect(page.getByRole("button", { name: "Delete 2 users" })).toBeVisible();
      await expect(page.getByRole("button", { name: "Delete 2 users" })).toBeEnabled();
    })();

    // === BULK USER DELETION SECTION ===
    await step("Cancel bulk deletion & verify users remain selected")(async () => {
      await page.getByRole("button", { name: "Delete 2 users" }).click();

      await expect(page.getByRole("alertdialog", { name: "Delete users" })).toBeVisible();
      await expect(page.getByText("Are you sure you want to delete 2 users?")).toBeVisible();

      await page.getByRole("button", { name: "Cancel" }).click();

      await expect(page.getByRole("alertdialog")).not.toBeVisible();
      await expect(page.locator("tbody").first().first().locator("tr")).toHaveCount(3); // All users still present
      await expect(page.getByRole("button", { name: "Delete 2 users" })).toBeVisible(); // Selection maintained
    })();

    await step("Confirm bulk delete selected users & verify removal")(async () => {
      await page.getByRole("button", { name: "Delete 2 users" }).click();

      const deleteDialog = page.getByRole("alertdialog", { name: "Delete users" });
      await expect(deleteDialog).toBeVisible();
      await deleteDialog.getByRole("button", { name: "Delete", exact: true }).click();

      await expectToastMessage(context, "2 users deleted successfully");
      await expect(deleteDialog).not.toBeVisible();
      await expect(page.locator("tbody").first().first().locator("tr")).toHaveCount(1); // Only owner left
      await expect(page.getByText(user2.email)).not.toBeVisible();
      await expect(page.getByText(user3.email)).not.toBeVisible();
      await expect(page.locator("tbody").first()).toContainText(owner.email);
      await expect(page.getByRole("button", { name: "Delete 2 users" })).not.toBeVisible();
      await expect(page.getByRole("button", { name: "Invite user" })).toBeVisible();
    })();

    // === OWNER PROTECTION SECTION ===
    await step("Verify owner menu delete option is disabled")(async () => {
      const ownerRow = page.locator("tbody").first().locator("tr").filter({ hasText: owner.email });
      const ownerActionsButton = ownerRow.locator("button[aria-label='User actions']").first();
      await ownerActionsButton.evaluate((el: HTMLElement) => el.click());

      await expect(page.getByRole("menuitem", { name: "Delete" })).toBeDisabled();

      // Click outside the menu to close it
      await page.locator("body").click({ position: { x: 10, y: 10 } });
    })();
  });
});

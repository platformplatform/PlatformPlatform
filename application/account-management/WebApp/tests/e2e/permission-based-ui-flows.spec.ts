import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext, expectToastMessage, typeOneTimeCode } from "@shared/e2e/utils/test-assertions";
import { completeSignupFlow, getVerificationCode, testUser } from "@shared/e2e/utils/test-data";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@smoke", () => {
  /**
   * PERMISSION-BASED UI ACCESS CONTROL TESTS
   *
   * Tests the permission-based UI behavior ensuring UI elements accurately reflect
   * what actions users can perform based on backend authorization rules by creating
   * users with different roles and testing the UI behavior in the same session.
   *
   * Covers:
   * - Owner vs Member UI visibility (invite button, danger zone, bulk actions)
   * - Self-action restrictions (cannot delete or change own role)
   * - Access denied page for role-restricted routes (recycle-bin requires Owner/Admin)
   * - Access denied page for internal-user-restricted routes (back-office requires internal user)
   *
   * Note: Current test fixtures infrastructure creates only Owner users, so we test
   * by creating users with different roles and switching between them in a single session.
   */
  test("should enforce permission-based UI visibility, self-action restrictions, and access denied pages", async ({
    page
  }) => {
    const context = createTestContext(page);
    const owner = testUser();
    const member = testUser();

    // Create owner and member users
    await step("Create owner account with signup flow & verify welcome page")(async () => {
      await completeSignupFlow(page, expect, owner, context);
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
    })();

    await step("Set account name & verify save confirmation")(async () => {
      await page.goto("/admin/account");
      await expect(page.getByRole("heading", { name: "Account settings" })).toBeVisible();
      await page.getByRole("textbox", { name: "Account name" }).fill("Test Organization");
      await page.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(context, "Account name updated successfully");
    })();

    await step("Navigate to users page as Owner & verify invite button is visible")(async () => {
      await page.goto("/admin/users");

      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(page.getByRole("button", { name: "Invite user" })).toBeVisible();
    })();

    await step("Navigate to account settings as Owner & verify tenant name field is editable")(async () => {
      await page.goto("/admin/account");

      await expect(page.getByRole("heading", { name: "Account settings" })).toBeVisible();
      await expect(page.getByRole("textbox", { name: "Account name" })).toBeEnabled();
      await expect(page.getByRole("textbox", { name: "Account name" })).not.toHaveAttribute("readonly");
      await expect(page.getByRole("button", { name: "Save changes" })).toBeVisible();
    })();

    await step("Verify danger zone is visible for Owner")(async () => {
      // Danger zone should be visible to Owners
      await expect(page.getByRole("heading", { name: "Danger zone" })).toBeVisible();
      await expect(page.getByRole("button", { name: "Delete account" })).toBeVisible();
      await expect(
        page.getByText("Delete your account and all data. This action is irreversible—proceed with caution.")
      ).toBeVisible();
    })();

    await step("Open owner's actions menu & verify self-action restrictions")(async () => {
      await page.goto("/admin/users");

      // Wait for page to load
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();

      // Wait for table to be present
      await page.waitForSelector("tbody tr", { state: "attached" });

      // Find the owner's own row by looking for the email
      const ownerRow = page.locator("tbody tr").filter({ hasText: owner.email }).first();

      // Click the actions button using JavaScript to bypass visibility checks
      const actionsButton = ownerRow.locator("button[aria-label='User actions']").first();
      await actionsButton.dispatchEvent("click");

      // Verify delete menu item is disabled (self-protection)
      await expect(page.getByRole("menuitem", { name: "Delete" })).toBeDisabled();
      // Verify change role menu item is disabled (self-protection)
      await expect(page.getByRole("menuitem", { name: "Change role" })).toBeDisabled();

      // Click outside the menu to close it
      await page.locator("body").click({ position: { x: 10, y: 10 } });

      // Wait for menu to close
      await expect(page.getByRole("menu")).not.toBeVisible();
    })();

    await step("Invite member user & verify user appears in table")(async () => {
      // Invite member user
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(member.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await expectToastMessage(context, "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Ensure the invitation is complete and the page is stable before proceeding
      await expect(page.locator("tbody").first()).toContainText(member.email);
    })();

    await step("Log out from owner and log in as member & verify authentication")(async () => {
      // Ensure the user table is stable and all users are loaded
      await expect(page.locator("tbody").first().locator("tr")).toHaveCount(2); // owner + member

      // Ensure the invite button is visible and the page is fully interactive
      await expect(page.getByRole("button", { name: "Invite user" })).toBeVisible();
      await expect(page.getByRole("button", { name: "Invite user" })).toBeEnabled();

      // Verify user emails are visible in the table to ensure data is loaded
      await expect(page.locator("tbody").first()).toContainText(owner.email);
      await expect(page.locator("tbody").first()).toContainText(member.email);

      // Mark 401 as expected during logout transition (React Query may have in-flight requests)
      context.monitoring.expectedStatusCodes.push(401);

      // Navigate away from users page first to prevent background requests
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

      // Wait for logout to complete and page to navigate
      await expect(page).toHaveURL("/login?returnPath=%2Fadmin");

      // Accept whatever return path we get
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // Login as member
      await page.getByRole("textbox", { name: "Email" }).fill(member.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await typeOneTimeCode(page, getVerificationCode());

      await page.waitForURL("/admin");
    })();

    await step("Complete member profile setup & verify profile saved")(async () => {
      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();
      await page.getByRole("textbox", { name: "First name" }).fill(member.firstName);
      await page.getByRole("textbox", { name: "Last name" }).fill(member.lastName);
      await page.getByRole("textbox", { name: "Title" }).fill("Team Member");
      await page.getByRole("button", { name: "Save changes" }).click();

      await expectToastMessage(context, "Profile updated successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
    })();

    await step("Navigate to users page as Member & verify invite button is hidden")(async () => {
      await page.goto("/admin/users");

      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(page.getByRole("button", { name: "Invite user" })).not.toBeVisible();
    })();

    await step("Navigate to account settings as Member & verify tenant name field is readonly")(async () => {
      await page.goto("/admin/account");

      // Members should see readonly account name field
      await expect(page.getByRole("heading", { name: "Account settings" })).toBeVisible();
      await expect(page.getByRole("textbox", { name: "Account name" })).toHaveAttribute("readonly");
      await expect(page.getByText("Only account owners can modify the account name")).toBeVisible();
      await expect(page.getByRole("button", { name: "Save changes" })).not.toBeVisible();
    })();

    await step("Verify danger zone is hidden for Member")(async () => {
      // Danger zone should be hidden from Members
      await expect(page.getByRole("heading", { name: "Danger zone" })).not.toBeVisible();
      await expect(page.getByRole("button", { name: "Delete account" })).not.toBeVisible();
      await expect(
        page.getByText("Delete your account and all data. This action is irreversible—proceed with caution.")
      ).not.toBeVisible();
    })();

    await step("Open member's actions menu & verify self-action restrictions")(async () => {
      await page.goto("/admin/users");

      // Wait for page to load
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();

      // Find the member's own row by filtering by email - use first() to handle duplicates
      const memberRow = page.locator("tbody tr").filter({ hasText: member.email }).first();
      const memberActionsButton = memberRow.locator("button[aria-label='User actions']").first();
      await memberActionsButton.dispatchEvent("click");

      // Verify delete and change role menu items are not visible (members don't see these options)
      await expect(page.getByRole("menuitem", { name: "Delete" })).not.toBeVisible();
      await expect(page.getByRole("menuitem", { name: "Change role" })).not.toBeVisible();

      // Verify only View profile is available
      await expect(page.getByRole("menuitem", { name: "View profile" })).toBeVisible();

      // Click outside the menu to close it
      await page.locator("body").click({ position: { x: 10, y: 10 } });
    })();

    // === ACCESS DENIED PAGE TESTS ===
    await step("Navigate to recycle-bin as Member & verify access denied page displays")(async () => {
      await page.goto("/admin/users/recycle-bin");

      await expect(page.getByRole("heading", { name: "Access denied" })).toBeVisible();
      await expect(page.getByText("You do not have permission to access this page.")).toBeVisible();
      await expect(page.getByRole("button", { name: "Go to home" })).toBeVisible();
    })();

    await step("Navigate to back-office as Member & verify access denied page displays")(async () => {
      await page.goto("/back-office");

      await expect(page.getByRole("heading", { name: "Access denied" })).toBeVisible();
      await expect(page.getByText("You do not have permission to access this page.")).toBeVisible();
    })();

    await step("Click Go to home on access denied page & verify navigation to home")(async () => {
      await page.getByRole("button", { name: "Go to home" }).click();

      await expect(page).toHaveURL("/");
    })();
  });

  /**
   * BULK DELETE PERMISSION TESTS
   *
   * Tests that bulk delete functionality is only available to Owners.
   */
  test("should show bulk delete controls only for Owners", async ({ page }) => {
    const context = createTestContext(page);
    const owner = testUser();
    const member = testUser();

    const user1 = testUser();
    const user2 = testUser();

    await step("Create owner account with signup flow & verify welcome page")(async () => {
      await completeSignupFlow(page, expect, owner, context);
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
    })();

    await step("Set account name & verify save confirmation")(async () => {
      await page.goto("/admin/account");
      await expect(page.getByRole("heading", { name: "Account settings" })).toBeVisible();
      await page.getByRole("textbox", { name: "Account name" }).fill("Test Organization");
      await page.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(context, "Account name updated successfully");
    })();

    await step("Navigate to users page & verify owner is listed")(async () => {
      await page.goto("/admin/users");
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
    })();

    await step("Invite first test user & verify user appears in table")(async () => {
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(user1.email);
      await page.getByRole("button", { name: "Send invite" }).click();

      await expectToastMessage(context, "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
      await expect(page.locator("tbody").first()).toContainText(user1.email);
    })();

    await step("Invite second test user & verify user appears in table")(async () => {
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(user2.email);
      await page.getByRole("button", { name: "Send invite" }).click();

      await expectToastMessage(context, "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
      await expect(page.locator("tbody").first()).toContainText(user2.email);
    })();

    await step("Invite member user & verify all users are in table")(async () => {
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(member.email);
      await page.getByRole("button", { name: "Send invite" }).click();

      await expectToastMessage(context, "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
      await expect(page.locator("tbody").first()).toContainText(member.email);
      // Should now have owner + 3 invited users = 4 total
      await expect(page.locator("tbody").first().locator("tr")).toHaveCount(4);
    })();

    await step("Select multiple users as Owner & verify bulk delete button appears")(async () => {
      // Select the first two invited users - use first tbody due to mobile rendering
      const rows = page.locator("tbody").first().locator("tr");
      const secondRow = rows.nth(1); // First invited user
      const thirdRow = rows.nth(2); // Second invited user

      // Select first user using force click to bypass visibility
      await secondRow.dispatchEvent("click");
      await expect(secondRow).toHaveAttribute("aria-selected", "true");

      // Select second user with Ctrl/Cmd modifier - use evaluate to simulate click with modifier
      await page.keyboard.down("ControlOrMeta");
      await thirdRow.dispatchEvent("click");
      await page.keyboard.up("ControlOrMeta");
      await expect(thirdRow).toHaveAttribute("aria-selected", "true");
      await expect(secondRow).toHaveAttribute("aria-selected", "true");

      // Verify bulk delete button is visible for Owner
      await expect(page.getByRole("button", { name: "Delete 2 users" })).toBeVisible();

      // Ensure the selections are stable and the UI has updated
      await expect(secondRow).toHaveAttribute("aria-selected", "true");
      await expect(thirdRow).toHaveAttribute("aria-selected", "true");
    })();

    await step("Log out as owner and log in as member & verify authentication")(async () => {
      // Ensure the bulk delete button is still visible and selections are stable
      await expect(page.getByRole("button", { name: "Delete 2 users" })).toBeVisible();
      await expect(page.getByRole("button", { name: "Delete 2 users" })).toBeEnabled();

      // Verify that the selected rows are still selected
      const allRows = page.locator("tbody").first().locator("tr");
      const secondRow = allRows.nth(1);
      const thirdRow = allRows.nth(2);
      await expect(secondRow).toHaveAttribute("aria-selected", "true");
      await expect(thirdRow).toHaveAttribute("aria-selected", "true");

      // Mark 401 as expected during logout transition (React Query may have in-flight requests)
      context.monitoring.expectedStatusCodes.push(401);

      // Navigate away from users page first to prevent background requests
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

      // Wait for logout to complete and page to navigate
      await expect(page).toHaveURL("/login?returnPath=%2Fadmin");

      // Accept whatever return path we get
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // Login as member
      await page.getByRole("textbox", { name: "Email" }).fill(member.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await typeOneTimeCode(page, getVerificationCode());

      await page.waitForURL("/admin");
    })();

    await step("Complete member profile setup & verify profile saved")(async () => {
      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();
      await page.getByRole("textbox", { name: "First name" }).fill(member.firstName);
      await page.getByRole("textbox", { name: "Last name" }).fill(member.lastName);
      await page.getByRole("textbox", { name: "Title" }).fill("Team Member");
      await page.getByRole("button", { name: "Save changes" }).click();

      await expectToastMessage(context, "Profile updated successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
    })();

    await step("Navigate to users page as Member & verify no bulk operations available")(async () => {
      await page.goto("/admin/users");

      // Ensure we can see the users that were created
      // Use first tbody due to mobile rendering creating duplicate tables
      await expect(page.locator("tbody").first().locator("tr")).toHaveCount(4);

      // Try to select rows (member can still select, but no bulk actions should appear)
      // Set viewport to 2xl to avoid side pane backdrop issues
      await page.setViewportSize({ width: 1536, height: 1024 });

      const rows = page.locator("tbody").first().locator("tr");
      const secondRow = rows.nth(1);
      const thirdRow = rows.nth(2);

      // Select users as Member using force click to bypass visibility
      await secondRow.dispatchEvent("click");
      await expect(secondRow).toHaveAttribute("aria-selected", "true");

      await page.keyboard.down("ControlOrMeta");
      await thirdRow.dispatchEvent("click");
      await page.keyboard.up("ControlOrMeta");
      await expect(thirdRow).toHaveAttribute("aria-selected", "true");

      // Verify bulk delete button is NOT visible for Member even with selections
      await expect(page.getByRole("button", { name: "Delete 2 users" })).not.toBeVisible();
      await expect(page.getByRole("button", { name: "Delete user" })).not.toBeVisible();

      // Reset viewport
      await page.setViewportSize({ width: 1280, height: 720 });
    })();
  });
});

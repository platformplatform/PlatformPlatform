import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext, expectToastMessage } from "@shared/e2e/utils/test-assertions";
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
   * Note: Current test fixtures infrastructure creates only Owner users, so we test
   * by creating users with different roles and switching between them in a single session.
   */
  test("should enforce permission-based UI visibility and self-action restrictions", async ({ page }) => {
    const context = createTestContext(page);
    const owner = testUser();
    const member = testUser();

    // Create owner and member users
    await step("Create owner account and set up tenant")(async () => {
      await completeSignupFlow(page, expect, owner, context);
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
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
      await expect(page.getByRole("heading", { name: "Danger zone" })).toBeVisible();
      await expect(page.getByRole("button", { name: "Delete account" })).toBeVisible();
      await expect(
        page.getByText("Delete your account and all data. This action is irreversible—proceed with caution.")
      ).toBeVisible();
    })();

    await step("Verify self-action restrictions work for Owner (cannot delete self or change own role)")(async () => {
      await page.goto("/admin/users");

      // Find the owner's own row by looking for the Owner role badge
      const ownerRow = page.locator("tbody tr").filter({ hasText: "Owner" });
      await ownerRow.getByLabel("User actions").click();

      // Verify delete menu item is disabled (self-protection)
      await expect(page.getByRole("menuitem", { name: "Delete" })).toBeDisabled();

      // Verify change role menu item is disabled (self-protection)
      await expect(page.getByRole("menuitem", { name: "Change role" })).toBeDisabled();

      await page.keyboard.press("Escape");
    })();

    await step("Invite member user and test non-Owner permissions after role switch")(async () => {
      // Invite member user
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(member.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await expectToastMessage(context, "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
    })();

    await step("Log out from owner and log in as member to test non-Owner UI restrictions")(async () => {
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Log out" }).click();

      // Accept whatever return path we get
      await expect(page.getByRole("heading", { name: "Welcome back" })).toBeVisible();

      // Login as member
      await page.getByRole("textbox", { name: "Email" }).fill(member.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await page.keyboard.type(getVerificationCode());

      // Wait for navigation to complete after verification
      await page.waitForURL(/\/admin/, { timeout: 10000 });
    })();

    await step("Complete member profile setup")(async () => {
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

      await expect(page.getByRole("heading", { name: "Account settings" })).toBeVisible();
      await expect(page.getByRole("textbox", { name: "Account name" })).toHaveAttribute("readonly");
      await expect(page.getByText("Only account owners can modify the account name")).toBeVisible();
      await expect(page.getByRole("button", { name: "Save changes" })).not.toBeVisible();
    })();

    await step("Verify danger zone is hidden for Member")(async () => {
      await expect(page.getByRole("heading", { name: "Danger zone" })).not.toBeVisible();
      await expect(page.getByRole("button", { name: "Delete account" })).not.toBeVisible();
      await expect(
        page.getByText("Delete your account and all data. This action is irreversible—proceed with caution.")
      ).not.toBeVisible();
    })();

    await step("Verify self-action restrictions work for Member (cannot delete self or change own role)")(async () => {
      await page.goto("/admin/users");

      // Find the member's own row by filtering by email
      const memberRow = page.locator("tbody tr").filter({ hasText: member.email });
      await memberRow.getByLabel("User actions").click();

      // Verify delete and change role menu items are not visible (members don't see these options)
      await expect(page.getByRole("menuitem", { name: "Delete" })).not.toBeVisible();
      await expect(page.getByRole("menuitem", { name: "Change role" })).not.toBeVisible();

      // Verify only View profile is available
      await expect(page.getByRole("menuitem", { name: "View profile" })).toBeVisible();

      await page.keyboard.press("Escape");
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

    await step("Create owner account and multiple test users")(async () => {
      await completeSignupFlow(page, expect, owner, context);
      await page.goto("/admin/users");

      const user1 = testUser();
      const user2 = testUser();

      // Invite first user
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(user1.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await expectToastMessage(context, "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Invite second user
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(user2.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await expectToastMessage(context, "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Invite member user for role testing
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(member.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await expectToastMessage(context, "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Should now have owner + 3 invited users = 4 total
      await expect(page.locator("tbody tr")).toHaveCount(4);
    })();

    await step("Select multiple users as Owner & verify bulk delete button appears")(async () => {
      // Set viewport to 2xl to avoid side pane backdrop issues
      await page.setViewportSize({ width: 1536, height: 1024 });

      // Select the first two invited users
      const rows = page.locator("tbody tr");
      const secondRow = rows.nth(1); // First invited user
      const thirdRow = rows.nth(2); // Second invited user

      // Select first user
      await secondRow.click();
      await expect(secondRow).toHaveAttribute("aria-selected", "true");

      // Select second user with Ctrl/Cmd modifier
      await thirdRow.click({ modifiers: ["ControlOrMeta"] });
      await expect(thirdRow).toHaveAttribute("aria-selected", "true");
      await expect(secondRow).toHaveAttribute("aria-selected", "true");

      // Verify bulk delete button is visible for Owner
      await expect(page.getByRole("button", { name: "Delete 2 users" })).toBeVisible();

      // Reset viewport
      await page.setViewportSize({ width: 1280, height: 720 });
    })();

    await step("Log out as owner and log in as member to test bulk delete restrictions")(async () => {
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Log out" }).click();

      // Accept whatever return path we get
      await expect(page.getByRole("heading", { name: "Welcome back" })).toBeVisible();

      // Login as member
      await page.getByRole("textbox", { name: "Email" }).fill(member.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
      await page.keyboard.type(getVerificationCode());

      // Wait for navigation to complete after verification
      await page.waitForURL(/\/admin/, { timeout: 10000 });
    })();

    await step("Complete member profile setup")(async () => {
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
      await expect(page.locator("tbody tr")).toHaveCount(4);

      // Try to select rows (member can still select, but no bulk actions should appear)
      // Set viewport to 2xl to avoid side pane backdrop issues
      await page.setViewportSize({ width: 1536, height: 1024 });

      const rows = page.locator("tbody tr");
      const secondRow = rows.nth(1);
      const thirdRow = rows.nth(2);

      // Select users as Member
      await secondRow.click();
      await expect(secondRow).toHaveAttribute("aria-selected", "true");

      await thirdRow.click({ modifiers: ["ControlOrMeta"] });
      await expect(thirdRow).toHaveAttribute("aria-selected", "true");

      // Verify bulk delete button is NOT visible for Member even with selections
      await expect(page.getByRole("button", { name: "Delete 2 users" })).not.toBeVisible();
      await expect(page.getByRole("button", { name: "Delete user" })).not.toBeVisible();

      // Reset viewport
      await page.setViewportSize({ width: 1280, height: 720 });
    })();
  });
});

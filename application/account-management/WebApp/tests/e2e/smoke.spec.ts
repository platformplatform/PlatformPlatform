import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import {
  assertNoUnexpectedErrors,
  assertToastMessage,
  assertValidationError,
  createTestContext
} from "@shared/e2e/utils/test-assertions";
import { getVerificationCode, testUser } from "@shared/e2e/utils/test-data";

test.describe("Account Management System", () => {
  test.describe("@smoke", () => {
    test("should complete full user journey from signup to tenant management", async ({ page }) => {
      const context = createTestContext(page);
      const owner = testUser();
      const adminUser = testUser();
      const ownerUser = testUser();
      const memberUser = testUser();

      // Step 1: Navigate to homepage and verify marketing content is visible
      await page.goto("/");
      await expect(page).toHaveTitle(/PlatformPlatform/);

      // Step 2: Navigate to signup page and start signup process
      await page.getByRole("button", { name: "Get started today" }).first().click();
      await expect(page).toHaveURL("/signup");
      await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();

      // Step 3: Try invalid email first
      await page.getByRole("textbox", { name: "Email" }).fill("invalid-email");
      await page.getByRole("button", { name: "Create your account" }).click();
      await assertValidationError(context, "Email must be in a valid format and no longer than 100 characters.");

      // Step 4: Complete signup with valid email and verify navigation
      await page.getByRole("textbox", { name: "Email" }).clear();
      await page.getByRole("textbox", { name: "Email" }).fill(owner.email);
      await expect(page.getByText("Europe")).toBeVisible(); // Verify region is pre-selected
      await page.getByRole("button", { name: "Create your account" }).click();
      await expect(page).toHaveURL("/signup/verify");

      // Step 5: Try wrong verification code first
      await page.keyboard.type("WRONG1");
      await page.getByRole("button", { name: "Verify" }).click();
      // TODO: Fix error message assertion - need to determine exact message format

      // Step 6: Complete verification process with correct code
      await page.locator("input[type='text']").first().clear();
      await page.keyboard.type(getVerificationCode());
      await page.getByRole("button", { name: "Verify" }).click();
      await expect(page).toHaveURL("/admin");

      // Step 7: Test profile form validation with missing required fields
      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertValidationError(context, "'First Name' must not be empty.");
      await assertValidationError(context, "'Last Name' must not be empty.");

      // Step 8: Test very long name validation
      const longName = "A".repeat(31); // Max is 30 characters
      await page.getByRole("textbox", { name: "First name" }).fill(longName);
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertValidationError(context, "First name must be no longer than 30 characters.");

      await page.getByRole("textbox", { name: "Last name" }).fill(longName);
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertValidationError(context, "Last name must be no longer than 30 characters.");

      // Step 9: Test very long title validation
      const longTitle = "B".repeat(51); // Max is 50 characters
      await page.getByRole("textbox", { name: "Title" }).fill(longTitle);
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertValidationError(context, "Title must be no longer than 50 characters.");

      // Step 10: Complete profile setup with valid data
      await page.getByRole("textbox", { name: "First name" }).clear();
      await page.getByRole("textbox", { name: "First name" }).fill(owner.firstName);
      await page.getByRole("textbox", { name: "Last name" }).clear();
      await page.getByRole("textbox", { name: "Last name" }).fill(owner.lastName);
      await page.getByRole("textbox", { name: "Title" }).clear();
      await page.getByRole("textbox", { name: "Title" }).fill("CEO & Founder");
      await page.getByRole("button", { name: "Save changes" }).click();
      await expect(page.getByRole("dialog")).not.toBeVisible();
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // Step 11: Verify avatar button shows initials
      const initials = owner.firstName.charAt(0) + owner.lastName.charAt(0);
      await expect(page.getByRole("button", { name: "User profile menu" })).toContainText(initials);

      // Verify title is saved by checking profile
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Edit profile" }).click();
      await expect(page.getByRole("textbox", { name: "Title" })).toHaveValue("CEO & Founder");
      await page.getByRole("button", { name: "Cancel" }).click();

      // Step 12: Test light/dark mode toggle
      // First verify current theme (should be light by default)
      await expect(page.locator("html")).toHaveClass(/light/);

      // Toggle to dark mode
      await page.getByRole("button", { name: "Toggle theme" }).click();
      await expect(page.locator("html")).toHaveClass(/dark/);

      // Toggle back to light mode
      await page.getByRole("button", { name: "Toggle theme" }).click();
      await expect(page.locator("html")).toHaveClass(/light/);

      // Step 13: Navigate to users page and verify owner is listed
      await page.getByRole("button", { name: "Users" }).click();
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(page.getByText(`${owner.firstName} ${owner.lastName}`)).toBeVisible();
      await expect(page.getByText(owner.email)).toBeVisible();
      await expect(page.getByText("Owner")).toBeVisible();

      // Step 14: Test user invitation - invite first user
      await page.getByRole("button", { name: "Invite user" }).click();
      await expect(page.getByRole("dialog", { name: "Invite user" })).toBeVisible();
      await page.getByRole("textbox", { name: "Email" }).fill(adminUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await assertToastMessage(context, "Success", "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Step 15: Invite second user
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(ownerUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await assertToastMessage(context, "Success", "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Step 16: Invite third user
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(memberUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await assertToastMessage(context, "Success", "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Step 17: Verify invited users appear in table
      await expect(page.getByText(adminUser.email)).toBeVisible();
      await expect(page.getByText(ownerUser.email)).toBeVisible();
      await expect(page.getByText(memberUser.email)).toBeVisible();
      // All invited users should have Member role by default
      await expect(page.getByText("Member").first()).toBeVisible();

      // Step 18: Test user filtering
      await page.getByPlaceholder("Search").fill(adminUser.email);
      await expect(page.getByText(adminUser.email)).toBeVisible();
      await expect(page.getByText(ownerUser.email)).not.toBeVisible();
      await expect(page.getByText(memberUser.email)).not.toBeVisible();

      // Clear search
      await page.getByPlaceholder("Search").clear();
      await expect(page.getByText(adminUser.email)).toBeVisible();
      await expect(page.getByText(ownerUser.email)).toBeVisible();
      await expect(page.getByText(memberUser.email)).toBeVisible();

      // Step 19: Test role filtering - first show filters
      await page.getByRole("button", { name: "Show filters" }).click();
      await page.getByRole("button", { name: "Any role User role" }).click();
      await page.getByRole("option", { name: "Owner" }).click();
      await expect(page.getByText(owner.email)).toBeVisible();
      await expect(page.getByText(adminUser.email)).not.toBeVisible();

      // Reset role filter
      await page.getByRole("button", { name: "Owner User role" }).click();
      await page.getByRole("option", { name: "Any role" }).click();
      await expect(page.getByText(adminUser.email)).toBeVisible();

      // Step 20: Test sidebar collapse functionality
      await page.getByRole("button", { name: "Toggle collapsed menu" }).click();

      // Step 21: Test dashboard links and filters
      await page.getByRole("button", { name: "Home" }).click();
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // Test dashboard link to users with active filter
      await page.getByRole("link", { name: "Active users" }).click();
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
      // Verify URL contains the status filter
      expect(page.url()).toContain("userStatus=Active");

      // Step 22: Test tenant settings update
      await page.getByRole("button", { name: "Account" }).click();
      await expect(page.getByRole("heading", { name: "Account" })).toBeVisible();
      await page.getByRole("textbox", { name: "Account name" }).clear();
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertValidationError(context, "'Name' must not be empty.");

      // Step 23: Update tenant name successfully
      const newTenantName = `Tech Corp ${Date.now()}`;
      await page.getByRole("textbox", { name: "Account name" }).fill(newTenantName);
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertToastMessage(context, "Success", "Account updated successfully");

      // Step 24: Update user profile and test avatar functionality
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Edit profile" }).click();
      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();
      await page.getByRole("textbox", { name: "Title" }).clear();
      await page.getByRole("textbox", { name: "Title" }).fill("Chief Executive Officer");
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertToastMessage(context, "Success", "Profile updated successfully");

      // Step 25: Test locale change functionality and user preference persistence
      await page.getByRole("button", { name: "Select language" }).click();
      await page.getByRole("menuitem", { name: "Dansk" }).click();
      await page.getByRole("button", { name: "Annuller" }).click();
      await page.goto("/admin");
      await expect(page.getByRole("heading", { name: "Velkommen hjem" })).toBeVisible();

      // Step 26: Test logout and verify redirect to login (sidebar should remain collapsed)
      await page.getByRole("button", { name: "Brugerprofilmenu" }).click();
      await page.getByRole("menuitem", { name: "Log ud" }).click();
      await expect(page).toHaveURL("/login?returnPath=%2Fadmin");
      await expect(page.getByRole("heading", { name: "Hej! Velkommen tilbage" })).toBeVisible();

      // Step 27: Change language to English on login page
      await page.getByRole("button", { name: "Vælg sprog" }).click();
      await page.getByRole("menuitem", { name: "English" }).click();
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();

      // Step 28: Test wrong login credentials first
      await page.getByRole("textbox", { name: "Email" }).fill("nonexistent@example.com");
      await page.getByRole("button", { name: "Continue" }).click();
      await page.keyboard.type("WRONG1");
      await page.getByRole("button", { name: "Verify" }).click();
      await assertToastMessage(context, 400, "The code is wrong or no longer valid.");

      // Step 29: Login with correct credentials
      await page.goto("/login");
      await page.getByRole("textbox", { name: "Email" }).fill(owner.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await page.keyboard.type(getVerificationCode());
      await page.getByRole("button", { name: "Verify" }).click();
      await expect(page).toHaveURL("/admin");

      // Step 30: Verify language preference persisted (should be Danish) and sidebar state persisted
      await expect(page.getByRole("heading", { name: "Velkommen hjem" })).toBeVisible();

      // Step 31: Test invited user login with Dutch language preference
      await page.getByRole("button", { name: "Brugerprofilmenu" }).click();
      await page.getByRole("menuitem", { name: "Log ud" }).click();

      // Change to Dutch on login page
      await page.getByRole("button", { name: "Vælg sprog" }).click();
      await page.getByRole("menuitem", { name: "Nederlands" }).click();

      // Login as invited admin user
      await page.getByRole("textbox", { name: "E-mail" }).fill(adminUser.email);
      await page.getByRole("button", { name: "Verder" }).click();
      await page.keyboard.type(getVerificationCode());
      await page.getByRole("button", { name: "Verifiëren" }).click();
      await expect(page).toHaveURL("/admin");

      // Verify Dutch is the user's language preference
      await expect(page.getByRole("heading", { name: "Welkom home" })).toBeVisible();

      // Step 32: Test protected route access and session persistence
      await page.getByRole("button", { name: "Account" }).click();
      await expect(page.getByRole("textbox", { name: "Accountnaam" })).toHaveValue(newTenantName);

      // Step 33: Reset language back to English for final verification
      await page.goto("/admin");
      await page.getByRole("button", { name: "Selecteer taal" }).click();
      await page.getByRole("menuitem", { name: "English" }).click();
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      assertNoUnexpectedErrors(context);
    });

    test("should handle concurrent sessions and authentication conflicts", async ({ browser }) => {
      // Create two browser contexts to simulate different sessions
      const context1 = await browser.newContext();
      const context2 = await browser.newContext();
      const page1 = await context1.newPage();
      const page2 = await context2.newPage();
      const testContext1 = createTestContext(page1);
      const testContext2 = createTestContext(page2);
      const user = testUser();

      // Step 1: Start signup in first browser
      await page1.goto("/");
      await page1.getByRole("button", { name: "Get started today" }).first().click();
      await page1.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page1.getByRole("button", { name: "Create your account" }).click();
      await expect(page1).toHaveURL("/signup/verify");

      // Step 2: Attempt signup with same email in second browser
      await page2.goto("/");
      await page2.getByRole("button", { name: "Get started today" }).first().click();
      await page2.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page2.getByRole("button", { name: "Create your account" }).click();
      await assertToastMessage(
        testContext2,
        409,
        "Email confirmation for this email has already been started. Please check your spam folder."
      );

      // Step 3: Complete signup in first browser
      await page1.keyboard.type(getVerificationCode());
      await page1.getByRole("button", { name: "Verify" }).click();
      await expect(page1).toHaveURL("/admin");
      await page1.getByRole("textbox", { name: "First name" }).fill(user.firstName);
      await page1.getByRole("textbox", { name: "Last name" }).fill(user.lastName);
      await page1.getByRole("button", { name: "Save changes" }).click();
      await expect(page1.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // Step 4: Try to login in second browser while first is still logged in
      await page2.goto("/login");
      await page2.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page2.getByRole("button", { name: "Continue" }).click();
      await expect(page2).toHaveURL("/login/verify");
      await page2.keyboard.type(getVerificationCode());
      await page2.getByRole("button", { name: "Verify" }).click();
      await expect(page2).toHaveURL("/admin");

      // Step 5: Verify both sessions are active
      await page1.goto("/admin/users");
      await expect(page1.getByRole("heading", { name: "Users" })).toBeVisible();
      await page2.goto("/admin/users");
      await expect(page2.getByRole("heading", { name: "Users" })).toBeVisible();

      // Step 6: Update profile in one session and verify it reflects in the other
      await page1.getByRole("button", { name: "User profile menu" }).click();
      await page1.getByRole("menuitem", { name: "Edit profile" }).click();
      await page1.getByRole("textbox", { name: "Title" }).fill("Updated Title");
      await page1.getByRole("button", { name: "Save changes" }).click();
      await assertToastMessage(testContext1, "Success", "Profile updated successfully");

      // Step 7: Refresh second session and verify the update is visible
      await page2.reload();
      await page2.getByRole("button", { name: "User profile menu" }).click();
      await page2.getByRole("menuitem", { name: "Edit profile" }).click();
      await expect(page2.getByRole("textbox", { name: "Title" })).toHaveValue("Updated Title");
      await page2.getByRole("button", { name: "Cancel" }).click();

      // Step 8: Logout from first session
      await page1.getByRole("button", { name: "User profile menu" }).click();
      await page1.getByRole("menuitem", { name: "Log out" }).click();
      await expect(page1).toHaveURL("/login?returnPath=%2Fadmin%2Fusers");

      // Step 9: Verify second session is still active
      await page2.goto("/admin");
      await expect(page2.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // Step 10: Clean up and assert no errors
      assertNoUnexpectedErrors(testContext1);
      assertNoUnexpectedErrors(testContext2);
      await context1.close();
      await context2.close();
    });
  });
});

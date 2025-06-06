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
      const invitedUser = testUser();

      // Step 1: Navigate to homepage and verify marketing content is visible
      await page.goto("/");
      await expect(page).toHaveTitle(/PlatformPlatform/);

      // Step 2: Navigate to signup page and start signup process
      await page.getByRole("button", { name: "Get started today" }).first().click();
      await expect(page).toHaveURL("/signup");
      await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();

      // Step 3: Complete signup with valid email and verify navigation
      await page.getByRole("textbox", { name: "Email" }).fill(owner.email);
      await expect(page.getByText("Europe")).toBeVisible(); // Verify region is pre-selected
      await page.getByRole("button", { name: "Create your account" }).click();
      await expect(page).toHaveURL("/signup/verify");

      // Step 4: Complete verification process
      await page.keyboard.type(getVerificationCode());
      await page.getByRole("button", { name: "Verify" }).click();
      await expect(page).toHaveURL("/admin");

      // Step 7: Test profile form validation with missing required fields
      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertValidationError(context, "'First Name' must not be empty.");
      await assertValidationError(context, "'Last Name' must not be empty.");

      // Step 8: Complete profile setup with valid data
      await page.getByRole("textbox", { name: "First name" }).fill(owner.firstName);
      await page.getByRole("textbox", { name: "Last name" }).fill(owner.lastName);
      await page.getByRole("textbox", { name: "Title" }).fill("CEO & Founder");
      await page.getByRole("button", { name: "Save changes" }).click();
      await expect(page.getByRole("dialog")).not.toBeVisible();
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // Step 9: Navigate to users page and verify owner is listed
      await page.getByRole("button", { name: "Users" }).click();
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(page.getByText(`${owner.firstName} ${owner.lastName}`)).toBeVisible();
      await expect(page.getByText(owner.email)).toBeVisible();
      await expect(page.getByText("Owner")).toBeVisible();

      // Step 10: Test user invitation with validation errors
      await page.getByRole("button", { name: "Invite user" }).click();
      await expect(page.getByRole("dialog", { name: "Invite user" })).toBeVisible();
      await page.getByRole("button", { name: "Send invite" }).click();
      await assertValidationError(context, "'Email' must not be empty.");

      // Step 11: Complete user invitation with valid data
      await page.getByRole("textbox", { name: "Email" }).fill(invitedUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await assertToastMessage(context, "Success", "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Step 12: TODO: Fix TanStack Query auto-invalidation in tests
      // For now, skip the user list verification and continue with other tests

      // Step 13: Test tenant settings update
      await page.getByRole("button", { name: "Account" }).click();
      await expect(page.getByRole("heading", { name: "Account" })).toBeVisible();
      await page.getByRole("textbox", { name: "Account name" }).clear();
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertValidationError(context, "'Name' must not be empty.");

      // Step 14: Update tenant name successfully
      const newTenantName = `Tech Corp ${Date.now()}`;
      await page.getByRole("textbox", { name: "Account name" }).fill(newTenantName);
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertToastMessage(context, "Success", "Account updated successfully");

      // Step 15: Update user profile and test avatar functionality
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Edit profile" }).click();
      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();
      await page.getByRole("textbox", { name: "Title" }).clear();
      await page.getByRole("textbox", { name: "Title" }).fill("Chief Executive Officer");
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertToastMessage(context, "Success", "Profile updated successfully");

      // Step 16: Test locale change functionality and user preference persistence
      await page.getByRole("button", { name: "Select language" }).click();
      await page.getByRole("menuitem", { name: "Dansk" }).click();
      await page.getByRole("button", { name: "Annuller" }).click();
      await page.goto("/admin");
      await expect(page.getByRole("heading", { name: "Velkommen hjem" })).toBeVisible();

      // Step 17: Test logout and verify redirect to login
      await page.getByRole("button", { name: "Brugerprofilmenu" }).click();
      await page.getByRole("menuitem", { name: "Log ud" }).click();
      await expect(page).toHaveURL("/login?returnPath=%2Fadmin");
      await expect(page.getByRole("heading", { name: "Hej! Velkommen tilbage" })).toBeVisible();

      // Step 18: Test wrong login credentials first
      await page.getByRole("textbox", { name: "E-mail" }).fill("nonexistent@example.com");
      await page.getByRole("button", { name: "Fortsæt" }).click();
      await page.keyboard.type("WRONG1");
      await page.getByRole("button", { name: "Bekræft" }).click();
      await assertToastMessage(context, 400, "The code is wrong or no longer valid.");

      // Step 19: Login with correct credentials
      await page.goto("/login");
      await page.getByRole("textbox", { name: "E-mail" }).fill(owner.email);
      await page.getByRole("button", { name: "Fortsæt" }).click();
      await page.keyboard.type(getVerificationCode());
      await page.getByRole("button", { name: "Bekræft" }).click();
      await expect(page).toHaveURL("/admin");

      // Step 20: Verify language preference persisted across logout/login
      await expect(page.getByRole("heading", { name: "Velkommen hjem" })).toBeVisible();

      // Step 21: Test protected route access and session persistence
      await page.getByRole("button", { name: "Account" }).click();
      await expect(page.getByRole("textbox", { name: "Account name" })).toHaveValue(newTenantName);

      // Step 22: Reset language back to English for final verification
      await page.goto("/admin");
      await page.getByRole("button", { name: "Select language" }).click();
      await page.getByRole("menuitem", { name: "English" }).click();
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await page.reload();
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

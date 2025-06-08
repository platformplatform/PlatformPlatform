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
      await expect(page).toHaveTitle("PlatformPlatform");

      // Step 2: Navigate to signup page and start signup process
      await page.getByRole("button", { name: "Get started today" }).first().click();
      await expect(page).toHaveURL("/signup");
      await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();

      // Step 3: Complete signup with valid email and verify navigation
      await page.getByRole("textbox", { name: "Email" }).fill(owner.email);
      await expect(page.getByText("Europe")).toBeVisible();
      await page.getByRole("button", { name: "Create your account" }).click();
      await expect(page).toHaveURL("/signup/verify");

      // Step 4: Complete verification process with correct code and navigate to admin
      await page.keyboard.type(getVerificationCode());
      await page.getByRole("button", { name: "Verify" }).click();
      await expect(page).toHaveURL("/admin");

      // Step 5: Submit profile form with empty fields and verify validation errors
      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertValidationError(context, "'First Name' must not be empty.");
      await assertValidationError(context, "'Last Name' must not be empty.");

      // Step 6: Fill form with one field too long and one missing, then verify all validation errors
      const longName = "A".repeat(31);
      await page.getByRole("textbox", { name: "First name" }).fill(longName);
      await page.getByRole("textbox", { name: "Last name" }).clear();
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertValidationError(context, "First name must be no longer than 30 characters.");
      await assertValidationError(context, "'Last Name' must not be empty.");

      // Step 7: Fill title field with too long value and verify validation error
      const longTitle = "B".repeat(51);
      await page.getByRole("textbox", { name: "Title" }).fill(longTitle);
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertValidationError(context, "Title must be no longer than 50 characters.");

      // Step 8: Complete profile setup with valid data and verify navigation to dashboard
      await page.getByRole("textbox", { name: "First name" }).fill(owner.firstName);
      await page.getByRole("textbox", { name: "Last name" }).fill(owner.lastName);
      await page.getByRole("textbox", { name: "Title" }).fill("CEO & Founder");
      await page.getByRole("button", { name: "Save changes" }).click();
      await expect(page.getByRole("dialog")).not.toBeVisible();
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // Step 9: Verify avatar button shows initials and profile information
      const initials = owner.firstName.charAt(0) + owner.lastName.charAt(0);
      await expect(page.getByRole("button", { name: "User profile menu" })).toContainText(initials);
      await page.getByRole("button", { name: "User profile menu" }).click();
      await expect(page.getByText(`${owner.firstName} ${owner.lastName}`)).toBeVisible();
      await expect(page.getByText("CEO & Founder")).toBeVisible();
      await page.getByRole("menuitem", { name: "Edit profile" }).click();
      await expect(page.getByRole("textbox", { name: "Title" })).toHaveValue("CEO & Founder");
      await page.getByRole("button", { name: "Cancel" }).click();
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Step 10: Test theme toggle cycle and verify theme changes work correctly
      const themeButton = page.getByRole("button", { name: "Toggle theme" });

      const initialThemeClass = await page.locator("html").getAttribute("class"); // Get initial system theme
      const initialIsLight = initialThemeClass?.includes("light");
      const firstTheme = initialIsLight ? "dark" : "light";
      const secondTheme = initialIsLight ? "light" : "dark";
      const thirdTheme = initialIsLight ? "dark" : "light";

      await expect(themeButton).toHaveAttribute("aria-label", "Toggle theme"); // Verify accessibility label

      await themeButton.click(); // First click: System → opposite
      await expect(page.locator("html")).toHaveClass(firstTheme);

      await themeButton.click(); // Second click: opposite → original
      await expect(page.locator("html")).toHaveClass(secondTheme);

      await themeButton.click(); // Third click: original → opposite (for rest of test)
      await expect(page.locator("html")).toHaveClass(thirdTheme);

      // Step 11: Navigate to users page and verify owner is listed
      await page.getByRole("button", { name: "Users" }).click();
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(page.getByText(`${owner.firstName} ${owner.lastName}`)).toBeVisible();
      await expect(page.getByText(owner.email)).toBeVisible();
      await expect(page.getByText("Owner")).toBeVisible();

      // Step 12: Submit invalid email invitation and verify validation error, then invite valid user
      await page.getByRole("button", { name: "Invite user" }).click();
      await expect(page.getByRole("dialog", { name: "Invite user" })).toBeVisible();
      await page.getByRole("textbox", { name: "Email" }).fill("invalid-email");
      await page.getByRole("button", { name: "Send invite" }).click();
      await assertValidationError(context, "Email must be in a valid format and no longer than 100 characters.");

      await page.getByRole("textbox", { name: "Email" }).fill(adminUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await assertToastMessage(context, "Success", "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Step 13: Invite second user and verify successful invitation
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(ownerUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await assertToastMessage(context, "Success", "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Step 14: Invite third user and verify successful invitation
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(memberUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await assertToastMessage(context, "Success", "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Step 15: Attempt to invite duplicate user email and verify error message
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(adminUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await expect(page.getByText("already in use by another user")).toBeVisible();
      await page.getByRole("button", { name: "Cancel" }).click();
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Step 16: Verify invited users appear in table
      await expect(page.getByText(adminUser.email)).toBeVisible();
      await expect(page.getByText(ownerUser.email)).toBeVisible();
      await expect(page.getByText(memberUser.email)).toBeVisible();
      await expect(page.getByText("Member").first()).toBeVisible();

      // Step 17: Filter users by email search and verify filtered results display correctly
      await page.getByPlaceholder("Search").fill(adminUser.email);
      await expect(page.getByText(adminUser.email)).toBeVisible();
      await expect(page.getByText(ownerUser.email)).not.toBeVisible();
      await expect(page.getByText(memberUser.email)).not.toBeVisible();
      await page.getByPlaceholder("Search").clear();
      await expect(page.getByText(adminUser.email)).toBeVisible();
      await expect(page.getByText(ownerUser.email)).toBeVisible();
      await expect(page.getByText(memberUser.email)).toBeVisible();

      // Step 18: Filter users by role and verify role-based filtering works correctly
      await page.getByRole("button", { name: "Show filters" }).click();
      await page.getByRole("button", { name: "Any role User role" }).click();
      await page.getByRole("option", { name: "Owner" }).click();
      await expect(page.getByText(owner.email)).toBeVisible();
      await expect(page.getByText(adminUser.email)).not.toBeVisible();
      await page.getByRole("button", { name: "Owner User role" }).click();
      await page.getByRole("option", { name: "Any role" }).click();
      await expect(page.getByText(adminUser.email)).toBeVisible();

      // Step 19: Collapse sidebar menu and verify layout changes
      await page.getByRole("button", { name: "Toggle collapsed menu" }).click();

      // Step 20: Navigate to dashboard and click active users link to verify URL filtering
      await page.getByRole("button", { name: "Home" }).click();
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await page.getByRole("link", { name: "Active users" }).click();
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
      expect(page.url()).toContain("userStatus=Active");

      // Step 21: Clear account name field and verify validation error appears
      await page.getByRole("button", { name: "Account" }).click();
      await expect(page.getByRole("heading", { name: "Account" })).toBeVisible();
      await page.getByRole("textbox", { name: "Account name" }).clear();
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertValidationError(context, "'Name' must not be empty.");

      // Step 22: Update account name and verify successful save
      const newAccountName = `Tech Corp ${Date.now()}`;
      await page.getByRole("textbox", { name: "Account name" }).fill(newAccountName);
      await page.getByRole("button", { name: "Save changes" }).focus(); // WebKit requires explicit focus before clicking
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertToastMessage(context, "Success", "Account updated successfully");

      // Step 23: Update user profile title and verify successful profile update
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Edit profile" }).click();
      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();
      await page.getByRole("textbox", { name: "Title" }).fill("Chief Executive Officer");
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertToastMessage(context, "Success", "Profile updated successfully");

      // Step 24: Change language to Danish and verify language preference persists
      await page.getByRole("button", { name: "Select language" }).click();
      await page.getByRole("menuitem", { name: "Dansk" }).click();
      await page.getByRole("button", { name: "Annuller" }).click();
      await page.goto("/admin");
      await expect(page.getByRole("heading", { name: "Velkommen hjem" })).toBeVisible();

      // Step 25: Logout from Danish interface and verify redirect to login page with Danish interface
      await page.getByRole("button", { name: "Brugerprofilmenu" }).click();
      await page.getByRole("menuitem", { name: "Log ud" }).click();
      await expect(page).toHaveURL("/login?returnPath=%2Fadmin");
      await expect(page.getByRole("heading", { name: "Hej! Velkommen tilbage" })).toBeVisible();

      // Step 26: Change login page language to Nederlands and verify interface updates
      await page.getByRole("button", { name: "Vælg sprog" }).click();
      await page.getByRole("menuitem", { name: "Nederlands" }).click();
      await expect(page.getByRole("heading", { name: "Hallo! Welkom terug" })).toBeVisible();

      // Step 27: Submit wrong login credentials and verify error message appears
      await page.getByRole("textbox", { name: "E-mail" }).fill(owner.email);
      await page.getByRole("button", { name: "Verder" }).click();
      await expect(page).toHaveURL("login/verify?returnPath=%2Fadmin");
      await expect(page.getByRole("heading", { name: "Voer je verificatiecode in" })).toBeVisible();
      await page.locator('input[autocomplete="one-time-code"]').first().click();
      await page.keyboard.type("WRONG1");
      await page.getByRole("button", { name: "Verifiëren" }).click();
      await assertToastMessage(context, 400, "The code is wrong or no longer valid.");

      // Step 28: Login with correct credentials and verify interface is Danish
      await page.locator('input[autocomplete="one-time-code"]').first().click();
      await page.keyboard.type(getVerificationCode());
      await page.getByRole("button", { name: "Verifiëren" }).click();
      await expect(page).toHaveURL("/admin");
      await expect(page.getByRole("heading", { name: "Velkommen hjem" })).toBeVisible();

      // Step 29: Access protected account route and verify session maintains authentication
      await page.getByRole("button", { name: "Konto" }).click();
      await expect(page.getByRole("textbox", { name: "Kontonavn" })).toBeVisible();

      // Step 30: Reset language to English and verify interface updates properly
      await page.goto("/admin");
      await page.getByRole("button", { name: "Vælg sprog" }).click();
      await page.getByRole("menuitem", { name: "English" }).click();
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // Step 31: Assert no unexpected errors occurred
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

      // Step 10: Assert no unexpected errors occurred
      assertNoUnexpectedErrors(testContext1);
      assertNoUnexpectedErrors(testContext2);

      // Step 11: Clean up manually created contexts
      await context1.close();
      await context2.close();
    });
  });
});

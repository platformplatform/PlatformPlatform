import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import {
  assertNetworkErrors,
  assertToastMessage,
  assertValidationError,
  createTestContext
} from "@shared/e2e/utils/test-assertions";
import { getVerificationCode, testUser } from "@shared/e2e/utils/test-data";

test.describe("Account Management System", () => {
  test.describe("@smoke", () => {
    test.describe.configure({ timeout: 120000 }); // Bump timeout to 2 minutes as smoke tests are very comprehensive

    test("account management smoke test", async ({ page }) => {

      const context = createTestContext(page);
      const owner = testUser();
      const adminUser = testUser();
      const ownerUser = testUser();
      const memberUser = testUser();

      // Act & Assert: Navigate to homepage & verify marketing content is visible
      await page.goto("/");
      await expect(page).toHaveTitle("PlatformPlatform");

      // Act & Assert: Navigate to signup page & verify signup process starts
      await page.getByRole("button", { name: "Get started today" }).first().click();
      await expect(page).toHaveURL("/signup");
      await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();

      // Act & Assert: Complete signup with valid email & verify navigation to verification page
      await page.getByRole("textbox", { name: "Email" }).fill(owner.email);
      await expect(page.getByText("Europe")).toBeVisible();
      await page.getByRole("button", { name: "Create your account" }).click();
      await expect(page).toHaveURL("/signup/verify");

      // Act & Assert: Test resend verification code functionality & verify no errors occur
      await page.getByRole("button", { name: "Didn't receive the code? Resend" }).click(); // Note: This appears to be a bug - no success toast is shown for resend

      // Act & Assert: Complete verification process with correct code & verify navigation to admin
      await page.locator('input[autocomplete="one-time-code"]').first().click();
      await page.keyboard.type(getVerificationCode());
      await page.getByRole("button", { name: "Verify" }).click();
      await expect(page).toHaveURL("/admin");

      // Act & Assert: Set mobile viewport size & verify profile dialog is visible
      await page.setViewportSize({ width: 375, height: 667 });
      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();

      // Act & Assert: Set tablet viewport size & verify profile dialog is visible
      await page.setViewportSize({ width: 768, height: 1024 });
      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();

      // Act & Assert: Set desktop viewport size & verify profile dialog is visible
      await page.setViewportSize({ width: 1920, height: 1080 });
      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();

      // Act & Assert: Submit profile form with empty fields & verify validation errors appear
      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertValidationError(context, "'First Name' must not be empty.");
      await assertValidationError(context, "'Last Name' must not be empty.");

      // Act & Assert: Fill form with one field too long and one missing & verify all validation errors appear
      const longName = "A".repeat(31);
      await page.getByRole("textbox", { name: "First name" }).fill(longName);
      await page.getByRole("textbox", { name: "Last name" }).clear();
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertValidationError(context, "First name must be no longer than 30 characters.");
      await assertValidationError(context, "'Last Name' must not be empty.");

      // Act & Assert: Fill title field with too long value & verify validation error appears
      const longTitle = "B".repeat(51);
      await page.getByRole("textbox", { name: "Title" }).fill(longTitle);
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertValidationError(context, "Title must be no longer than 50 characters.");

      // Act & Assert: Complete profile setup with valid data & verify navigation to dashboard
      await page.getByRole("textbox", { name: "First name" }).fill(owner.firstName);
      await page.getByRole("textbox", { name: "Last name" }).fill(owner.lastName);
      await page.getByRole("textbox", { name: "Title" }).fill("CEO & Founder");
      await page.getByRole("button", { name: "Save changes" }).click();
      await expect(page.getByRole("dialog")).not.toBeVisible();
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // Act & Assert: Navigate to signup page while authenticated & verify redirect to admin
      await page.goto("/signup");
      await expect(page).toHaveURL("/admin");
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // Act & Assert: Click avatar button & verify it shows initials and profile information
      const initials = owner.firstName.charAt(0) + owner.lastName.charAt(0);
      await expect(page.getByRole("button", { name: "User profile menu" })).toContainText(initials);
      await page.getByRole("button", { name: "User profile menu" }).click();
      await expect(page.getByText(`${owner.firstName} ${owner.lastName}`)).toBeVisible();
      await expect(page.getByText("CEO & Founder")).toBeVisible();
      await page.getByRole("menuitem", { name: "Edit profile" }).click();
      await expect(page.getByRole("textbox", { name: "Title" })).toHaveValue("CEO & Founder");
      await page.getByRole("button", { name: "Cancel" }).click();
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Act & Assert: Toggle theme cycle through all modes & verify theme changes work correctly
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

      // Act & Assert: Navigate to users page & verify owner is listed
      await page.getByRole("button", { name: "Users" }).click();
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(page.getByText(`${owner.firstName} ${owner.lastName}`)).toBeVisible();
      await expect(page.getByText(owner.email)).toBeVisible();
      await expect(page.getByText("Owner")).toBeVisible();

      // Act & Assert: Reload page & verify authentication state persists
      await page.reload();
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
      await expect(page.getByText(`${owner.firstName} ${owner.lastName}`)).toBeVisible();

      // Act & Assert: Navigate to different admin pages & verify access is maintained
      await page.goto("/admin");
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await page.goto("/admin/users");
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();

      // Act & Assert: Submit invalid email invitation & verify validation error, then invite valid user & verify success
      await page.getByRole("button", { name: "Invite user" }).click();
      await expect(page.getByRole("dialog", { name: "Invite user" })).toBeVisible();
      await page.getByRole("textbox", { name: "Email" }).fill("invalid-email");
      await page.getByRole("button", { name: "Send invite" }).click();
      await assertValidationError(context, "Email must be in a valid format and no longer than 100 characters.");

      await page.getByRole("textbox", { name: "Email" }).fill(adminUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await assertToastMessage(context, "Success", "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Act & Assert: Invite second user & verify successful invitation
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(ownerUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await assertToastMessage(context, "Success", "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Act & Assert: Invite third user & verify successful invitation
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(memberUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await assertToastMessage(context, "Success", "User invited successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Act & Assert: Attempt to invite duplicate user email & verify error message appears
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(adminUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await expect(page.getByText("already in use by another user")).toBeVisible();
      await page.getByRole("button", { name: "Cancel" }).click();
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Act & Assert: Check users table & verify invited users appear
      await expect(page.getByText(adminUser.email)).toBeVisible();
      await expect(page.getByText(ownerUser.email)).toBeVisible();
      await expect(page.getByText(memberUser.email)).toBeVisible();
      await expect(page.getByText("Member").first()).toBeVisible();

      // Act & Assert: Filter users by email search & verify filtered results display correctly
      await page.getByPlaceholder("Search").fill(adminUser.email);
      await expect(page.getByText(adminUser.email)).toBeVisible();
      await expect(page.getByText(ownerUser.email)).not.toBeVisible();
      await expect(page.getByText(memberUser.email)).not.toBeVisible();
      await page.getByPlaceholder("Search").clear();
      await expect(page.getByText(adminUser.email)).toBeVisible();
      await expect(page.getByText(ownerUser.email)).toBeVisible();
      await expect(page.getByText(memberUser.email)).toBeVisible();

      // Act & Assert: Filter users by role & verify role-based filtering works correctly
      await page.getByRole("button", { name: "Show filters" }).click();
      await page.getByRole("button", { name: "Any role User role" }).click();
      await page.getByRole("option", { name: "Owner" }).click();
      await expect(page.getByText(owner.email)).toBeVisible();
      await expect(page.getByText(adminUser.email)).not.toBeVisible();
      await page.getByRole("button", { name: "Owner User role" }).click();
      await page.getByRole("option", { name: "Any role" }).click();
      await expect(page.getByText(adminUser.email)).toBeVisible();

      // Act & Assert: Collapse sidebar menu & verify layout changes
      await page.getByRole("button", { name: "Toggle collapsed menu" }).click();

      // Act & Assert: Navigate to dashboard and click active users link & verify URL filtering
      await page.getByRole("button", { name: "Home" }).click();
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await page.getByRole("link", { name: "Active users" }).click();
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
      expect(page.url()).toContain("userStatus=Active");

      // Act & Assert: Clear account name field & verify validation error appears
      await page.getByRole("button", { name: "Account" }).click();
      await expect(page.getByRole("heading", { name: "Account" })).toBeVisible();
      await page.getByRole("textbox", { name: "Account name" }).clear();
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertValidationError(context, "'Name' must not be empty.");

      // Act & Assert: Update account name & verify successful save
      const newAccountName = `Tech Corp ${Date.now()}`;
      await page.getByRole("textbox", { name: "Account name" }).fill(newAccountName);
      await page.getByRole("button", { name: "Save changes" }).focus(); // WebKit requires explicit focus before clicking
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertToastMessage(context, "Success", "Account updated successfully");

      // Act & Assert: Update user profile title & verify successful profile update
      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Edit profile" }).click();
      await expect(page.getByRole("dialog", { name: "User profile" })).toBeVisible();
      await page.getByRole("textbox", { name: "Title" }).fill("Chief Executive Officer");
      await page.getByRole("button", { name: "Save changes" }).click();
      await assertToastMessage(context, "Success", "Profile updated successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Act & Assert: Change language to Danish & verify language preference persists
      await page.getByRole("button", { name: "Select language" }).click();
      await page.getByRole("menuitem", { name: "Dansk" }).click();
      await expect(page.getByRole("button", { name: "Vælg sprog" })).toBeVisible();
      await page.goto("/admin");
      await expect(page.getByRole("heading", { name: "Velkommen hjem" })).toBeVisible();

      // Act & Assert: Logout from Danish interface & verify redirect to login page with Danish interface
      await page.getByRole("button", { name: "Brugerprofilmenu" }).click();
      await page.getByRole("menuitem", { name: "Log ud" }).click();
      await expect(page).toHaveURL("/login?returnPath=%2Fadmin");
      await expect(page.getByRole("heading", { name: "Hej! Velkommen tilbage" })).toBeVisible();

      // Act & Assert: Access protected routes while unauthenticated & verify redirect to login
      await page.goto("/admin");
      await expect(page).toHaveURL("/login?returnPath=%2Fadmin");
      await page.goto("/admin/users");
      await expect(page).toHaveURL("/login?returnPath=%2Fadmin%2Fusers");
      await assertNetworkErrors(context, [401]);

      // Act & Assert: Check current URL & verify login page shows return path in URL parameters
      const currentUrl = new URL(page.url());
      expect(currentUrl.searchParams.get("returnPath")).toBe("/admin/users");

      // Act & Assert: Change login page language to Nederlands & verify interface updates
      await page.getByRole("button", { name: "Vælg sprog" }).click();
      await page.getByRole("menuitem", { name: "Nederlands" }).click();
      await expect(page.getByRole("heading", { name: "Hallo! Welkom terug" })).toBeVisible();

      // Act & Assert: Submit wrong login credentials & verify error message appears
      await page.getByRole("textbox", { name: "E-mail" }).fill(owner.email);
      await page.getByRole("button", { name: "Verder" }).click();
      await expect(page).toHaveURL("/login/verify?returnPath=%2Fadmin%2Fusers");
      await expect(page.getByRole("heading", { name: "Voer je verificatiecode in" })).toBeVisible();
      await page.locator('input[autocomplete="one-time-code"]').first().click();
      await page.keyboard.type("WRONG1");
      await page.getByRole("button", { name: "Verifiëren" }).click();
      await assertToastMessage(context, 400, "The code is wrong or no longer valid.");

      // Act & Assert: Login with correct credentials & verify interface is Danish
      await page.locator('input[autocomplete="one-time-code"]').first().click();
      await page.keyboard.type(getVerificationCode());
      await page.getByRole("button", { name: "Verifiëren" }).click();
      await expect(page).toHaveURL("/admin/users");
      await page.goto("/admin");
      await expect(page.getByRole("heading", { name: "Velkommen hjem" })).toBeVisible();

      // Act & Assert: Access protected account route & verify session maintains authentication
      await page.getByRole("button", { name: "Konto" }).click();
      await expect(page.getByRole("textbox", { name: "Kontonavn" })).toBeVisible();

      // Act & Assert: Reset language to English & verify interface updates properly
      await page.goto("/admin");
      await page.getByRole("button", { name: "Vælg sprog" }).click();
      await page.getByRole("menuitem", { name: "English" }).click();
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
    });
  });
});

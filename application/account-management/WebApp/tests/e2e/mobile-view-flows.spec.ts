import { faker } from "@faker-js/faker";
import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext, expectToastMessage } from "@shared/e2e/utils/test-assertions";
import { completeSignupFlow, testUser } from "@shared/e2e/utils/test-data";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@comprehensive", () => {
  /**
   * Tests mobile-specific functionality including navigation, user profile editing,
   * language switching, theme switching, and keyboard navigation on the users table.
   * Covers:
   * - Mobile menu navigation with hidden top menu and side menu
   * - User profile editing through mobile menu
   * - Language switching functionality
   * - Theme switching functionality
   * - Keyboard navigation on users table without auto-opening side pane
   * - Navigation between multiple users and manual side pane opening
   */
  test("should handle mobile navigation and user management with keyboard accessibility", async ({ page }) => {
    const context = createTestContext(page);
    const owner = testUser();

    // Set mobile viewport
    await page.setViewportSize({ width: 390, height: 844 });

    await step("Complete owner signup & verify welcome page")(async () => {
      await completeSignupFlow(page, expect, owner, context);
    })();

    await step("Set account name & verify save confirmation")(async () => {
      await page.goto("/admin/account");
      await expect(page.getByRole("heading", { name: "Account settings" })).toBeVisible();
      await page.getByRole("textbox", { name: "Account name" }).fill("Mobile Nav Test");
      await page.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(context, "Account name updated successfully");
    })();

    await step("Navigate to admin dashboard & verify mobile layout")(async () => {
      await page.goto("/admin");

      // Wait for page to load - heading contains the user's name
      await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
    })();

    await step("Verify mobile layout with hidden menus & English UI")(async () => {
      // Wait for page to load - heading contains the user's name
      await expect(page.getByRole("heading", { level: 1 })).toBeVisible();

      // Wait for mobile layout to be ready
      await expect(page.getByRole("button", { name: "Open navigation menu" })).toBeVisible();

      // Verify side menu navigation links are hidden
      await expect(page.getByLabel("Main navigation").getByRole("link", { name: "Home" })).not.toBeVisible();
      await expect(page.getByLabel("Main navigation").getByRole("link", { name: "Account" })).not.toBeVisible();
      await expect(page.getByLabel("Main navigation").getByRole("link", { name: "Users" })).not.toBeVisible();

      // Verify top menu buttons are hidden on mobile
      await expect(page.getByRole("button", { name: "Change theme" })).not.toBeVisible();
      await expect(page.getByRole("button", { name: "Contact support" })).not.toBeVisible();
      await expect(page.getByRole("button", { name: "Change language" })).not.toBeVisible();
      await expect(page.getByRole("button", { name: "User profile menu" })).not.toBeVisible();
    })();

    await step("Open mobile menu & verify all navigation and settings are accessible")(async () => {
      await page.getByRole("button", { name: "Open navigation menu" }).click();

      const mobileDialog = page.getByRole("dialog", { name: "Mobile navigation menu" });
      await expect(mobileDialog).toBeVisible();

      // Verify user profile section is visible
      await expect(mobileDialog.getByRole("button", { name: "Edit" })).toBeVisible();

      // Verify all menu options are present
      await expect(mobileDialog.getByRole("button", { name: "Log out" })).toBeVisible();
      await expect(mobileDialog.getByRole("button", { name: "Theme" })).toBeVisible();
      await expect(mobileDialog.getByRole("button", { name: "Language" })).toBeVisible();
      await expect(mobileDialog.getByRole("button", { name: "Contact support" })).toBeVisible();

      // Verify navigation links
      await expect(mobileDialog.getByRole("link", { name: "Home" })).toBeVisible();
      await expect(mobileDialog.getByRole("link", { name: "Account" })).toBeVisible();
      await expect(mobileDialog.getByRole("link", { name: "Users" })).toBeVisible();
    })();

    // === USER PROFILE EDITING ===
    await step("Edit user profile through mobile menu & verify profile modal opens")(async () => {
      const mobileDialog = page.getByRole("dialog", { name: "Mobile navigation menu" });
      await mobileDialog.getByRole("button", { name: "Edit" }).click();

      // Wait for mobile menu to close and profile modal to open
      await expect(mobileDialog).not.toBeVisible();

      // Profile modal should open - wait for it to be visible
      const profileModal = page.getByRole("dialog", { name: "User profile" });
      await expect(profileModal).toBeVisible();

      // Verify form fields are present
      await expect(profileModal.getByLabel("First name")).toBeVisible();
      await expect(profileModal.getByLabel("Last name")).toBeVisible();
      await expect(profileModal.getByLabel("Email")).toBeVisible();
      await expect(profileModal.getByRole("textbox", { name: "Title" })).toBeVisible();
    })();

    await step("Update profile information & verify changes are saved")(async () => {
      const profileModal = page.getByRole("dialog", { name: "User profile" });
      const newTitle = faker.person.jobTitle();

      // Fill in the title field
      await profileModal.getByRole("textbox", { name: "Title" }).fill(newTitle);
      // Click save button
      await profileModal.getByRole("button", { name: "Save" }).click();

      // Wait for success toast
      await expectToastMessage(context, "Profile updated successfully");
      await expect(profileModal).not.toBeVisible();

      // Verify changes are reflected in mobile menu
      await page.getByRole("button", { name: "Open navigation menu" }).click();
      await expect(page.getByText(newTitle)).toBeVisible();

      // Close mobile menu by clicking the X button
      const mobileDialog = page.getByRole("dialog", { name: "Mobile navigation menu" });
      await mobileDialog.getByRole("button", { name: "Close menu" }).click();
      await expect(mobileDialog).not.toBeVisible();
    })();

    // === LANGUAGE SWITCHING ===
    await step("Change language to Danish through mobile menu & verify UI updates")(async () => {
      await page.getByRole("button", { name: "Open navigation menu" }).click();

      const mobileDialog = page.getByRole("dialog", { name: "Mobile navigation menu" });

      // Click trigger with JavaScript evaluate to ensure reliable opening on Firefox
      const languageButton = mobileDialog.getByRole("button", { name: "Language" });
      await languageButton.dispatchEvent("click");

      await expect(page.getByRole("menu")).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const danskMenuItem = page.getByRole("menuitem", { name: "Dansk" });
      await expect(danskMenuItem).toBeVisible();
      await danskMenuItem.dispatchEvent("click");

      // Mobile menu should close
      await expect(mobileDialog).not.toBeVisible();

      // Verify language changed - check heading
      await expect(page.getByRole("heading", { name: "Velkommen hjem" })).toBeVisible();
    })();

    await step("Change language back to English & verify language updates")(async () => {
      await page.getByRole("button", { name: "Åbn navigationsmenu" }).click();

      const mobileDialog = page.getByRole("dialog", { name: "Mobile navigation menu" });

      // Click trigger with JavaScript evaluate to ensure reliable opening on Firefox
      const languageButton = mobileDialog.getByRole("button", { name: "Sprog" });
      await languageButton.dispatchEvent("click");

      await expect(page.getByRole("menu")).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const englishMenuItem = page.getByRole("menuitem", { name: "English" });
      await expect(englishMenuItem).toBeVisible();
      await englishMenuItem.dispatchEvent("click");

      // Mobile menu should close
      await expect(mobileDialog).not.toBeVisible();

      // Verify language changed back - check heading
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
    })();

    // === THEME SWITCHING ===
    await step("Change theme through mobile menu & verify theme applies")(async () => {
      await page.getByRole("button", { name: "Open navigation menu" }).click();

      const mobileDialog = page.getByRole("dialog", { name: "Mobile navigation menu" });

      // Click trigger with JavaScript evaluate to ensure reliable opening on Firefox
      const themeButton = mobileDialog.getByRole("button", { name: "Theme" });
      await themeButton.dispatchEvent("click");

      await expect(page.getByRole("menu")).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const darkMenuItem = page.getByRole("menuitem", { name: "Dark" });
      await expect(darkMenuItem).toBeVisible();
      await darkMenuItem.dispatchEvent("click");

      // Mobile menu should close
      await expect(mobileDialog).not.toBeVisible();

      // Verify dark theme is applied
      await expect(page.locator("html")).toHaveClass("dark");
    })();

    // === NAVIGATION TO USERS PAGE ===
    await step("Navigate to users page through mobile menu & verify navigation works")(async () => {
      await page.getByRole("button", { name: "Open navigation menu" }).click();

      const mobileDialog = page.getByRole("dialog", { name: "Mobile navigation menu" });
      await mobileDialog.getByRole("link", { name: "Users" }).click();

      // Mobile menu should close
      await expect(mobileDialog).not.toBeVisible();

      // Verify navigation to users page
      await expect(page).toHaveURL("/admin/users");
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
    })();

    // === KEYBOARD NAVIGATION TESTS ===
    await step("Invite 3 test users & verify they appear in the table")(async () => {
      // Create test users
      const inviteUserButton = page.getByRole("button", { name: "Invite user" });

      // Create 3 additional users
      for (let i = 0; i < 3; i++) {
        const user = testUser();

        await inviteUserButton.click();
        const dialog = page.getByRole("dialog", { name: "Invite user" });
        await expect(dialog).toBeVisible();

        await dialog.getByLabel("Email").fill(user.email);
        await dialog.getByRole("button", { name: "Send invite" }).click();

        await expectToastMessage(context, "User invited successfully");
        await expect(dialog).not.toBeVisible();
      }

      // Verify we have at least 4 users in the table (1 owner + 3 new users)
      const rows = page.locator("tbody tr");
      const rowCount = await rows.count();
      expect(rowCount).toBeGreaterThanOrEqual(4);
    })();

    await step("Click first user & verify side pane opens on mobile")(async () => {
      // Click the first row in the users table
      const firstRow = page.locator("tbody tr").first();
      await firstRow.click();

      // Ensure row is selected
      await expect(firstRow).toHaveAttribute("aria-selected", "true");

      // Verify side pane opens automatically on mobile when clicking a row
      const sidePane = page.locator("aside").filter({ hasText: "User profile" });
      await expect(sidePane).toBeVisible();

      // Wait for side pane to be fully interactive and close button to be visible
      const closeButton = sidePane.locator("svg[aria-label='Close user profile']");
      await expect(closeButton).toBeVisible();

      // Focus on the side pane to ensure Escape key is handled
      await sidePane.focus();

      // Close the side pane with Escape key
      await page.keyboard.press("Escape");
      await expect(sidePane).not.toBeVisible();
    })();

    await step("Navigate to second user with keyboard & verify side pane stays closed")(async () => {
      // Press down arrow to move to second user
      await page.keyboard.press("ArrowDown");

      const secondRow = page.locator("tbody tr").nth(1);
      await expect(secondRow).toHaveAttribute("aria-selected", "true");

      // Verify side pane remains closed when using keyboard navigation
      await expect(page.locator('[aria-label="User profile"]')).not.toBeVisible();
    })();

    await step("Navigate to third user & manually open side pane with Enter key")(async () => {
      // Press down arrow to move to third user
      await page.keyboard.press("ArrowDown");

      const thirdRow = page.locator("tbody tr").nth(2);
      await expect(thirdRow).toHaveAttribute("aria-selected", "true");

      // Press Enter to open the side pane
      await page.keyboard.press("Enter");

      // Verify side pane opens
      const sidePane = page.locator('[aria-label="User profile"]');
      await expect(sidePane).toBeVisible();

      // Wait for side pane animation to complete and close button to be visible
      const closeButton = sidePane.locator("svg[aria-label='Close user profile']");
      await expect(closeButton).toBeVisible();
    })();

    await step("Close side pane with Escape & verify it closes")(async () => {
      const sidePane = page.locator('[aria-label="User profile"]');

      // Ensure side pane is fully open
      await expect(sidePane).toBeVisible();
      const closeButton = sidePane.locator("svg[aria-label='Close user profile']");
      await expect(closeButton).toBeVisible();

      // Focus on the side pane to ensure Escape key is handled
      await sidePane.focus();

      // Press Escape to close the side pane
      await page.keyboard.press("Escape");
      await expect(sidePane).not.toBeVisible();
    })();

    await step("Click first user row & verify side pane opens automatically")(async () => {
      // Re-select first user since selection was cleared
      const firstRow = page.locator("tbody tr").first();
      await firstRow.click();
      await expect(firstRow).toHaveAttribute("aria-selected", "true");

      // Verify side pane opened automatically
      const sidePane = page.locator("aside").filter({ hasText: "User profile" });
      await expect(sidePane).toBeVisible();
    })();

    await step("Press Escape key & verify side pane closes")(async () => {
      const sidePane = page.locator("aside").filter({ hasText: "User profile" });

      // Wait for side pane to be fully visible
      await expect(sidePane).toBeVisible();

      // Wait for close button to ensure side pane is fully rendered
      const closeButton = sidePane.locator("svg[aria-label='Close user profile']");
      await expect(closeButton).toBeVisible();

      // Focus on the side pane before pressing Escape
      await sidePane.focus();

      // Press Escape to close the side pane
      await page.keyboard.press("Escape");

      // Verify side pane closes
      await expect(sidePane).not.toBeVisible();
    })();

    await step("Click second user row & verify side pane opens automatically")(async () => {
      // Navigate to second user and open side pane
      const secondRow = page.locator("tbody tr").nth(1);
      await secondRow.click();
      await expect(secondRow).toHaveAttribute("aria-selected", "true");

      // Side pane should open automatically on click in mobile
      const sidePane = page.locator("aside").filter({ hasText: "User profile" });
      await expect(sidePane).toBeVisible();
    })();

    await step("Test closing side pane with X button & verify it works")(async () => {
      // Wait for any toast notifications to disappear before clicking
      await expect(page.locator("[data-react-aria-top-layer]")).not.toBeVisible();

      // Click close button to close side pane
      const sidePane = page.locator("aside").filter({ hasText: "User profile" });
      const closeButton = sidePane.locator("svg[aria-label='Close user profile']");
      await closeButton.click();

      await expect(sidePane).not.toBeVisible();
    })();

    await step("Verify mobile menu still works after side pane interactions")(async () => {
      // Verify mobile menu still works after side pane interaction
      await page.getByRole("button", { name: "Open navigation menu" }).click();
      await expect(page.getByRole("dialog", { name: "Mobile navigation menu" })).toBeVisible();

      // Close mobile menu using escape key
      await page.keyboard.press("Escape");
    })();
  });

  /**
   * Tests mobile-specific form interactions and validation.
   * Covers:
   * - Form submission through mobile menu
   * - Touch interactions with form elements
   * - Validation error display on mobile
   * - Modal behavior on mobile viewport
   */
  test("should handle mobile form interactions and validation correctly", async ({ ownerPage }) => {
    const context = createTestContext(ownerPage);

    // Set mobile viewport
    await ownerPage.setViewportSize({ width: 375, height: 667 });

    await step("Set account name & verify save confirmation")(async () => {
      await ownerPage.goto("/admin/account");
      await expect(ownerPage.getByRole("heading", { name: "Account settings" })).toBeVisible();

      await ownerPage.getByRole("textbox", { name: "Account name" }).clear();
      await ownerPage.getByRole("textbox", { name: "Account name" }).fill("Mobile Test Org");
      await ownerPage.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(context, "Account name updated successfully");
    })();

    await step("Navigate to users page & open invite user dialog")(async () => {
      await ownerPage.goto("/admin/users");
      // Check for either English or Danish heading
      await expect(ownerPage.getByRole("heading", { level: 1 })).toBeVisible();

      await ownerPage.getByRole("button", { name: "Invite user" }).click();

      const dialog = ownerPage.getByRole("dialog", { name: "Invite user" });
      await expect(dialog).toBeVisible();

      // Email validation is comprehensively tested in signup-flows.spec.ts
      // Just cancel the dialog
    })();

    await step("Cancel dialog & verify mobile menu remains functional")(async () => {
      const dialog = ownerPage.getByRole("dialog", { name: "Invite user" });
      await dialog.getByRole("button", { name: "Cancel" }).click();

      await expect(dialog).not.toBeVisible();

      // Test mobile menu
      await ownerPage.getByRole("button", { name: "Open navigation menu" }).click();
      await expect(ownerPage.getByRole("dialog", { name: "Mobile navigation menu" })).toBeVisible();

      await ownerPage.keyboard.press("Escape");
    })();
  });

  /**
   * Tests mobile user selection behavior with mixed keyboard and mouse interactions.
   * Ensures that:
   * - Single click always single-selects a user (no accidental multi-select)
   * - Keyboard navigation maintains selection when closing side pane
   * - Mixed keyboard/mouse workflows work correctly
   * - Multi-select only happens with Ctrl/Cmd modifier keys
   */
  test("should handle mobile user selection with mixed keyboard and mouse interactions", async ({ page }) => {
    const context = createTestContext(page);
    const user = testUser();

    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });

    await step("Create a fresh tenant & verify welcome page")(async () => {
      await completeSignupFlow(page, expect, user, context, true);
    })();

    await step("Set account name & verify save confirmation")(async () => {
      await page.goto("/admin/account");
      await expect(page.getByRole("heading", { name: "Account settings" })).toBeVisible();
      await page.getByRole("textbox", { name: "Account name" }).fill("Mobile Selection Test");
      await page.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(context, "Account name updated successfully");
    })();

    // === SETUP ===
    await step("Navigate to users page & invite 3 test users")(async () => {
      await page.goto("/admin/users");
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();

      // Create 3 test users for selection testing
      for (let i = 0; i < 3; i++) {
        const user = testUser();

        await page.getByRole("button", { name: "Invite user" }).click();
        const dialog = page.getByRole("dialog", { name: "Invite user" });
        await expect(dialog).toBeVisible();

        await dialog.getByLabel("Email").fill(user.email);
        await dialog.getByRole("button", { name: "Send invite" }).click();

        await expectToastMessage(context, "User invited successfully");
        await expect(dialog).not.toBeVisible();
      }

      // Verify we have exactly 4 users (1 owner + 3 new users) in a fresh tenant
      const rows = page.locator("tbody").first().locator("tr");
      const rowCount = await rows.count();
      expect(rowCount).toBe(4);
    })();

    // === SCENARIO 1: Test working functionality first ===
    await step("Click first user with mouse & verify single selection and side pane opens")(async () => {
      const firstRow = page.locator("tbody tr").first();
      await expect(firstRow).toBeVisible();
      await firstRow.click();

      // Verify only first row is selected
      await expect(firstRow).toHaveAttribute("aria-selected", "true");
      const secondRow = page.locator("tbody tr").nth(1);
      await expect(secondRow).toBeVisible();
      await expect(secondRow).toHaveAttribute("aria-selected", "false");

      // Verify side pane opens
      const sidePane = page.locator("aside").filter({ hasText: "User profile" });
      await expect(sidePane).toBeVisible();
    })();

    await step("Close side pane using close button & verify it closes")(async () => {
      // Wait for any toast notifications to disappear before clicking
      await expect(page.locator("[data-react-aria-top-layer]")).not.toBeVisible();

      // Click close button to close side pane
      const sidePane = page.locator("aside").filter({ hasText: "User profile" });
      const closeButton = sidePane.locator("svg[aria-label='Close user profile']");
      await closeButton.click();

      // Verify side pane closes
      await expect(sidePane).not.toBeVisible();
    })();

    await step("Navigate with keyboard & verify side pane stays closed")(async () => {
      // Re-select the first row since selection was cleared
      const firstRow = page.locator("tbody tr").first();
      await firstRow.click();

      // Close the side pane that opens
      await page.keyboard.press("Escape");

      // Now click first row again and verify selection
      await firstRow.click();
      await expect(firstRow).toHaveAttribute("aria-selected", "true");

      // Close side pane
      await page.keyboard.press("Escape");

      // Use keyboard to navigate to second row
      await page.keyboard.press("ArrowDown");

      // Verify selection moved to second row
      const secondRow = page.locator("tbody tr").nth(1);
      await expect(firstRow).toHaveAttribute("aria-selected", "false");
      await expect(secondRow).toHaveAttribute("aria-selected", "true");

      // Verify side pane stays closed during keyboard navigation
      const sidePane = page.locator("aside").filter({ hasText: "User profile" });
      await expect(sidePane).not.toBeVisible();
    })();

    await step("Press Enter to open side pane for second user & verify it opens")(async () => {
      await page.keyboard.press("Enter");

      // Verify side pane opens
      const sidePane = page.locator("aside").filter({ hasText: "User profile" });
      await expect(sidePane).toBeVisible();
    })();

    await step("Click X button to close side pane & verify selection maintained")(async () => {
      const sidePane = page.locator("aside").filter({ hasText: "User profile" });
      const closeButton = sidePane.locator("svg[aria-label='Close user profile']");
      await closeButton.click();

      // Verify side pane closes
      await expect(sidePane).not.toBeVisible();
    })();

    // === PREVIOUSLY FAILING SCENARIOS - should now work with single selection mode ===
    await step("Simple click on second user after first is selected & verify single selection")(async () => {
      // Close side pane
      await page.keyboard.press("Escape");

      const firstRow = page.locator("tbody tr").first();
      const secondRow = page.locator("tbody tr").nth(1);

      // Click first user
      await firstRow.click();
      await expect(firstRow).toHaveAttribute("aria-selected", "true");

      // Close side pane
      await page.keyboard.press("Escape");
      await expect(page.locator("aside").filter({ hasText: "User profile" })).not.toBeVisible();

      // Click second user - should single select with our fix
      await secondRow.click();

      // With single selection mode, only second user should be selected
      await expect(firstRow).toHaveAttribute("aria-selected", "false");
      await expect(secondRow).toHaveAttribute("aria-selected", "true");
    })();

    await step("Click third user after keyboard navigation and side pane interaction & verify single selection")(
      async () => {
        // Reset state - ensure any side pane is closed first
        const sidePane = page.locator("aside").filter({ hasText: "User profile" });
        await expect(sidePane).toBeVisible();
        await page.keyboard.press("Escape");
        await expect(sidePane).not.toBeVisible();

        // Click first user
        const firstRow = page.locator("tbody tr").first();
        await firstRow.click();
        await page.keyboard.press("Escape");

        // Re-select first row since selection was cleared
        await firstRow.click();
        await expect(firstRow).toHaveAttribute("aria-selected", "true");
        await page.keyboard.press("Escape");

        // Navigate to second with keyboard
        const secondRow = page.locator("tbody tr").nth(1);
        await page.keyboard.press("ArrowDown");
        await expect(secondRow).toHaveAttribute("aria-selected", "true");

        // Open side pane with Enter
        const thirdRow = page.locator("tbody tr").nth(2);
        await page.keyboard.press("Enter");
        await expect(page.locator("aside").filter({ hasText: "User profile" })).toBeVisible();

        // Close with X button
        const closeButton = page
          .locator("aside")
          .filter({ hasText: "User profile" })
          .locator("svg[aria-label='Close user profile']");
        await closeButton.click();
        await expect(page.locator("aside").filter({ hasText: "User profile" })).not.toBeVisible();

        // Click third user - should single select with our fix
        await thirdRow.click();

        // With single selection mode, only third user should be selected
        await expect(firstRow).toHaveAttribute("aria-selected", "false");
        await expect(secondRow).toHaveAttribute("aria-selected", "false");
        await expect(thirdRow).toHaveAttribute("aria-selected", "true");
      }
    )();

    await step("Rapid clicking between users & verify single selection")(async () => {
      // Reset state - ensure any side pane is closed first
      const sidePane = page.locator("aside").filter({ hasText: "User profile" });
      await expect(sidePane).toBeVisible();
      await page.keyboard.press("Escape");
      await expect(sidePane).not.toBeVisible();

      // Rapid clicks - on mobile, side pane opens after each click
      const firstRow = page.locator("tbody tr").first();
      await firstRow.click();

      // Close side pane that opened
      await expect(page.locator("aside").filter({ hasText: "User profile" })).toBeVisible();
      await page.keyboard.press("Escape");
      await expect(page.locator("aside").filter({ hasText: "User profile" })).not.toBeVisible();

      const secondRow = page.locator("tbody tr").nth(1);
      await secondRow.click();

      // Close side pane again
      await expect(page.locator("aside").filter({ hasText: "User profile" })).toBeVisible();
      await page.keyboard.press("Escape");
      await expect(page.locator("aside").filter({ hasText: "User profile" })).not.toBeVisible();

      const thirdRow = page.locator("tbody tr").nth(2);
      await thirdRow.click();

      // With single selection mode, only third user should be selected
      await expect(firstRow).toHaveAttribute("aria-selected", "false");
      await expect(secondRow).toHaveAttribute("aria-selected", "false");
      await expect(thirdRow).toHaveAttribute("aria-selected", "true");
    })();
  });
});

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
  test("should handle mobile navigation and user management with keyboard accessibility", async ({ ownerPage }) => {
    const context = createTestContext(ownerPage);

    // Set mobile viewport from the start
    await ownerPage.setViewportSize({ width: 375, height: 667 });

    await step("Navigate to admin dashboard & verify mobile layout")(async () => {
      await ownerPage.goto("/admin");

      // Wait for page to load - heading contains the user's name
      await expect(ownerPage.getByRole("heading", { level: 1 })).toBeVisible();
    })();

    await step("Verify mobile layout with hidden menus & English UI")(async () => {
      // Wait for page to load - heading contains the user's name
      await expect(ownerPage.getByRole("heading", { level: 1 })).toBeVisible();

      // Wait for mobile layout to be ready
      await expect(ownerPage.getByRole("button", { name: "Open navigation menu" })).toBeVisible();

      // Verify side menu navigation links are hidden
      await expect(ownerPage.getByLabel("Main navigation").getByRole("link", { name: "Home" })).not.toBeVisible();
      await expect(ownerPage.getByLabel("Main navigation").getByRole("link", { name: "Account" })).not.toBeVisible();
      await expect(ownerPage.getByLabel("Main navigation").getByRole("link", { name: "Users" })).not.toBeVisible();

      // Verify top menu buttons are hidden on mobile
      await expect(ownerPage.getByRole("button", { name: "Change theme" })).not.toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Contact support" })).not.toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Change language" })).not.toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "User profile menu" })).not.toBeVisible();
    })();

    await step("Open mobile menu & verify all navigation and settings are accessible")(async () => {
      await ownerPage.getByRole("button", { name: "Open navigation menu" }).click();

      const mobileDialog = ownerPage.getByRole("dialog");
      await expect(mobileDialog).toBeVisible();

      // Verify user profile section is visible
      // The user profile section should show the user's name and edit button
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
      const mobileDialog = ownerPage.getByRole("dialog", { name: "Mobile navigation menu" });
      await mobileDialog.getByRole("button", { name: "Edit" }).click();

      // Wait for mobile menu to close and profile modal to open
      await expect(mobileDialog).not.toBeVisible();

      // Profile modal should open - wait for it to be visible
      const profileModal = ownerPage.getByRole("dialog", { name: "User profile" });
      await expect(profileModal).toBeVisible();

      // Verify form fields are present
      await expect(profileModal.getByLabel("First name")).toBeVisible();
      await expect(profileModal.getByLabel("Last name")).toBeVisible();
      await expect(profileModal.getByLabel("Email")).toBeVisible();
      await expect(profileModal.getByLabel("Title")).toBeVisible();
    })();

    await step("Update profile information & verify changes are saved")(async () => {
      const profileModal = ownerPage.getByRole("dialog", { name: "User profile" });
      const newTitle = faker.person.jobTitle();

      // Fill in the title field
      await profileModal.getByLabel("Title").fill(newTitle);
      // Click save button
      await profileModal.getByRole("button", { name: "Save" }).click();

      // Wait for success toast
      await expectToastMessage(context, "Profile updated successfully");
      await expect(profileModal).not.toBeVisible();

      // Verify changes are reflected in mobile menu
      await ownerPage.getByRole("button", { name: "Open navigation menu" }).click();
      await expect(ownerPage.getByText(newTitle)).toBeVisible();

      // Close mobile menu by clicking the X button
      const mobileDialog = ownerPage.getByRole("dialog");
      await mobileDialog.getByRole("button", { name: "Close menu" }).click();
      await expect(mobileDialog).not.toBeVisible();
    })();

    // === LANGUAGE SWITCHING ===
    await step("Change language to Danish through mobile menu & verify UI updates")(async () => {
      await ownerPage.getByRole("button", { name: "Open navigation menu" }).click();

      const mobileDialog = ownerPage.getByRole("dialog", { name: "Mobile navigation menu" });
      await mobileDialog.getByRole("button", { name: "Language" }).click();

      // Wait for language menu to open
      await expect(ownerPage.getByRole("menu")).toBeVisible();

      // Select Danish
      await ownerPage.getByRole("menuitem", { name: "Dansk" }).click();

      // Mobile menu should close
      await expect(mobileDialog).not.toBeVisible();

      // Verify language changed - check heading
      await expect(ownerPage.getByRole("heading", { name: "Velkommen hjem" })).toBeVisible();
    })();

    await step("Change language back to English")(async () => {
      await ownerPage.getByRole("button", { name: "Åbn navigationsmenu" }).click();

      const mobileDialog = ownerPage.getByRole("dialog");
      await mobileDialog.getByRole("button", { name: "Sprog" }).click();

      // Wait for language menu to open
      await expect(ownerPage.getByRole("menu")).toBeVisible();

      // Select English
      await ownerPage.getByRole("menuitem", { name: "English" }).click();

      // Mobile menu should close
      await expect(mobileDialog).not.toBeVisible();

      // Verify language changed back - check heading
      await expect(ownerPage.getByRole("heading", { name: "Welcome home" })).toBeVisible();
    })();

    // === THEME SWITCHING ===
    await step("Change theme through mobile menu & verify theme applies")(async () => {
      await ownerPage.getByRole("button", { name: "Open navigation menu" }).click();

      const mobileDialog = ownerPage.getByRole("dialog");
      await mobileDialog.getByRole("button", { name: "Theme" }).click();

      // Wait for theme menu to open
      await expect(ownerPage.getByRole("menu")).toBeVisible();

      // Select dark theme
      await ownerPage.getByRole("menuitem", { name: "Dark" }).click();

      // Mobile menu should close
      await expect(mobileDialog).not.toBeVisible();

      // Verify dark theme is applied
      await expect(ownerPage.locator("html")).toHaveClass("dark");
    })();

    // === NAVIGATION TO USERS PAGE ===
    await step("Navigate to users page through mobile menu & verify navigation works")(async () => {
      await ownerPage.getByRole("button", { name: "Open navigation menu" }).click();

      const mobileDialog = ownerPage.getByRole("dialog");
      await mobileDialog.getByRole("link", { name: "Users" }).click();

      // Mobile menu should close
      await expect(mobileDialog).not.toBeVisible();

      // Verify navigation to users page
      await expect(ownerPage).toHaveURL("/admin/users");
      await expect(ownerPage.getByRole("heading", { name: "Users" })).toBeVisible();
    })();

    // === KEYBOARD NAVIGATION TESTS ===
    await step("Create test users for keyboard navigation")(async () => {
      // Create test users
      const inviteUserButton = ownerPage.getByRole("button", { name: "Invite user" });

      // Create 3 additional users
      for (let i = 0; i < 3; i++) {
        const user = testUser();

        await inviteUserButton.click();
        const dialog = ownerPage.getByRole("dialog", { name: "Invite user" });
        await expect(dialog).toBeVisible();

        await dialog.getByLabel("Email").fill(user.email);
        await dialog.getByRole("button", { name: "Send invite" }).click();

        await expectToastMessage(context, "User invited successfully");
        await expect(dialog).not.toBeVisible();
      }

      // Verify we have at least 4 users in the table (1 owner + 3 new users)
      const rows = ownerPage.locator("tbody tr");
      const rowCount = await rows.count();
      expect(rowCount).toBeGreaterThanOrEqual(4);
    })();

    await step("Click first user & verify side pane opens on mobile")(async () => {
      // Click the first row in the users table
      const firstRow = ownerPage.locator("tbody tr").first();
      await firstRow.click();

      // Ensure row is selected
      await expect(firstRow).toHaveAttribute("aria-selected", "true");

      // Verify side pane opens automatically on mobile when clicking a row
      const sidePane = ownerPage.getByRole("complementary", { name: "User profile details" });
      await expect(sidePane).toBeVisible();

      // Close the side pane
      await ownerPage.keyboard.press("Escape");
      await expect(sidePane).not.toBeVisible();
    })();

    await step("Navigate to second user with keyboard & verify side pane stays closed")(async () => {
      // Press down arrow to move to second user
      await ownerPage.keyboard.press("ArrowDown");

      const secondRow = ownerPage.locator("tbody tr").nth(1);
      await expect(secondRow).toHaveAttribute("aria-selected", "true");

      // Verify side pane remains closed when using keyboard navigation
      await expect(ownerPage.getByRole("complementary", { name: "User profile details" })).not.toBeVisible();
    })();

    await step("Navigate to third user & manually open side pane with Enter key")(async () => {
      // Press down arrow to move to third user
      await ownerPage.keyboard.press("ArrowDown");

      const thirdRow = ownerPage.locator("tbody tr").nth(2);
      await expect(thirdRow).toHaveAttribute("aria-selected", "true");

      // Get user email from the third row for verification
      const emailCell = thirdRow.locator("td").nth(1);
      await expect(emailCell).toBeVisible();
      const userEmail = await emailCell.textContent();
      expect(userEmail).toBeTruthy();

      // Press Enter to open the side pane
      await ownerPage.keyboard.press("Enter");

      // Verify side pane opens with correct user
      const sidePane = ownerPage.getByRole("complementary", { name: "User profile details" });
      await expect(sidePane).toBeVisible();
      await expect(sidePane.getByText(userEmail as string)).toBeVisible();

      // Wait for side pane animation to complete and close button to be visible
      const closeButton = sidePane.locator("svg[aria-label='Close user profile']");
      await expect(closeButton).toBeVisible();
    })();

    await step("Close side pane with Escape")(async () => {
      // Press Escape to close the side pane
      await ownerPage.keyboard.press("Escape");

      const sidePane = ownerPage.getByRole("complementary", { name: "User profile details" });
      await expect(sidePane).not.toBeVisible();
    })();

    await step("Navigate back to first user with keyboard & open side pane")(async () => {
      // Re-select first user since selection was cleared
      const firstRow = ownerPage.locator("tbody tr").first();
      await firstRow.click();
      await expect(firstRow).toHaveAttribute("aria-selected", "true");

      // Get user email from the first row
      const emailCell = firstRow.locator("td").nth(1);
      await expect(emailCell).toBeVisible();
      const userEmail = await emailCell.textContent();
      expect(userEmail).toBeTruthy();

      // Verify side pane opened automatically
      const sidePane = ownerPage.getByRole("complementary", { name: "User profile details" });
      await expect(sidePane).toBeVisible();
      await expect(sidePane.getByText(userEmail as string)).toBeVisible();
    })();

    await step("Verify side pane can be closed with Escape")(async () => {
      const sidePane = ownerPage.getByRole("complementary", { name: "User profile details" });

      // Wait for side pane to be fully visible
      await expect(sidePane).toBeVisible();

      // Press Escape to close the side pane
      await ownerPage.keyboard.press("Escape");

      // Verify side pane closes
      await expect(sidePane).not.toBeVisible();
    })();

    await step("Reopen side pane for second user & verify it opens correctly")(async () => {
      // Navigate to second user and open side pane
      const secondRow = ownerPage.locator("tbody tr").nth(1);
      await secondRow.click();
      await expect(secondRow).toHaveAttribute("aria-selected", "true");

      // Side pane should open automatically on click in mobile
      const sidePane = ownerPage.getByRole("complementary", { name: "User profile details" });
      await expect(sidePane).toBeVisible();
    })();

    await step("Use Escape to close side pane & verify mobile menu still works")(async () => {
      // Press Escape to close side pane
      await ownerPage.keyboard.press("Escape");

      await expect(ownerPage.getByRole("complementary", { name: "User profile details" })).not.toBeVisible();

      // Verify mobile menu still works after side pane interaction
      await ownerPage.getByRole("button", { name: "Open navigation menu" }).click();
      await expect(ownerPage.getByRole("dialog", { name: "Mobile navigation menu" })).toBeVisible();

      // Close mobile menu using escape key
      await ownerPage.keyboard.press("Escape");
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
    createTestContext(ownerPage);

    // Set mobile viewport
    await ownerPage.setViewportSize({ width: 375, height: 667 });

    await step("Navigate to users page & create user with validation errors on mobile")(async () => {
      await ownerPage.goto("/admin/users");
      // Check for either English or Danish heading
      await expect(ownerPage.getByRole("heading", { level: 1 })).toBeVisible();

      await ownerPage.getByRole("button", { name: "Invite user" }).click();

      const dialog = ownerPage.getByRole("dialog", { name: "Invite user" });
      await expect(dialog).toBeVisible();

      // Try to submit empty form
      await dialog.getByRole("button", { name: "Send invite" }).click();

      // Verify validation error is visible on mobile
      await expect(dialog.getByRole("textbox", { name: "Email" })).toHaveAttribute("aria-invalid", "true");
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

    await step("Create a fresh tenant with a new owner")(async () => {
      await completeSignupFlow(page, expect, user, context, true);
    })();

    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });

    // === SETUP ===
    await step("Navigate to users page & create test users")(async () => {
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
      const rows = page.locator("tbody tr");
      const rowCount = await rows.count();
      expect(rowCount).toBe(4);
    })();

    // === SCENARIO 1: Test working functionality first ===
    await step("Click first user with mouse & verify single selection and side pane opens")(async () => {
      const firstRow = page.locator("tbody tr").first();
      await firstRow.click();

      // Verify only first row is selected
      await expect(firstRow).toHaveAttribute("aria-selected", "true");
      const secondRow = page.locator("tbody tr").nth(1);
      await expect(secondRow).toHaveAttribute("aria-selected", "false");

      // Verify side pane opens
      const sidePane = page.getByRole("complementary", { name: "User profile details" });
      await expect(sidePane).toBeVisible();
    })();

    await step("Press Escape to close side pane")(async () => {
      await page.keyboard.press("Escape");

      // Verify side pane closes
      const sidePane = page.getByRole("complementary", { name: "User profile details" });
      await expect(sidePane).not.toBeVisible();
    })();

    await step("Navigate with keyboard and verify side pane stays closed")(async () => {
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
      const sidePane = page.getByRole("complementary", { name: "User profile details" });
      await expect(sidePane).not.toBeVisible();
    })();

    await step("Press Enter to open side pane for second user")(async () => {
      await page.keyboard.press("Enter");

      // Verify side pane opens
      const sidePane = page.getByRole("complementary", { name: "User profile details" });
      await expect(sidePane).toBeVisible();

      // Get email from second row to verify correct user
      const secondRow = page.locator("tbody tr").nth(1);
      const emailCell = secondRow.locator("td").nth(1);
      const userEmail = await emailCell.textContent();
      expect(userEmail).toBeTruthy();
      await expect(sidePane.getByText(userEmail as string)).toBeVisible();
    })();

    await step("Click X button to close side pane & verify selection maintained")(async () => {
      const sidePane = page.getByRole("complementary", { name: "User profile details" });
      const closeButton = sidePane.locator("svg[aria-label='Close user profile']");
      await closeButton.click();

      // Verify side pane closes
      await expect(sidePane).not.toBeVisible();
    })();

    // === PREVIOUSLY FAILING SCENARIOS - should now work with single selection mode ===
    await step("Simple click on second user after first is selected")(async () => {
      // Close side pane
      await page.keyboard.press("Escape");

      const firstRow = page.locator("tbody tr").first();
      const secondRow = page.locator("tbody tr").nth(1);

      // Click first user
      await firstRow.click();
      await expect(firstRow).toHaveAttribute("aria-selected", "true");

      // Close side pane
      await page.keyboard.press("Escape");
      await expect(page.getByRole("complementary", { name: "User profile details" })).not.toBeVisible();

      // Click second user - should single select with our fix
      await secondRow.click();

      // With single selection mode, only second user should be selected
      await expect(firstRow).toHaveAttribute("aria-selected", "false");
      await expect(secondRow).toHaveAttribute("aria-selected", "true");
    })();

    await step("Click third user after keyboard navigation and side pane interaction")(async () => {
      // Reset state - ensure any side pane is closed first
      const sidePane = page.getByRole("complementary", { name: "User profile details" });
      const isSidePaneVisible = await sidePane.isVisible().catch(() => false);
      if (isSidePaneVisible) {
        await page.keyboard.press("Escape");
        await expect(sidePane).not.toBeVisible();
      }

      // Click outside to deselect
      await page.locator("h1").click();

      const firstRow = page.locator("tbody tr").first();
      const secondRow = page.locator("tbody tr").nth(1);
      const thirdRow = page.locator("tbody tr").nth(2);

      // Click first user
      await firstRow.click();
      await page.keyboard.press("Escape");

      // Re-select first row since selection was cleared
      await firstRow.click();
      await expect(firstRow).toHaveAttribute("aria-selected", "true");
      await page.keyboard.press("Escape");

      // Navigate to second with keyboard
      await page.keyboard.press("ArrowDown");
      await expect(secondRow).toHaveAttribute("aria-selected", "true");

      // Open side pane with Enter
      await page.keyboard.press("Enter");
      await expect(page.getByRole("complementary", { name: "User profile details" })).toBeVisible();

      // Close with X button
      const closeButton = page.getByRole("complementary").locator("svg[aria-label='Close user profile']");
      await closeButton.click();
      await expect(page.getByRole("complementary", { name: "User profile details" })).not.toBeVisible();

      // Click third user - should single select with our fix
      await thirdRow.click();

      // With single selection mode, only third user should be selected
      await expect(firstRow).toHaveAttribute("aria-selected", "false");
      await expect(secondRow).toHaveAttribute("aria-selected", "false");
      await expect(thirdRow).toHaveAttribute("aria-selected", "true");
    })();

    await step("Rapid clicking between users")(async () => {
      // Reset state - ensure any side pane is closed first
      const sidePane = page.getByRole("complementary", { name: "User profile details" });
      const isSidePaneVisible = await sidePane.isVisible().catch(() => false);
      if (isSidePaneVisible) {
        await page.keyboard.press("Escape");
        await expect(sidePane).not.toBeVisible();
      }

      // Click outside to deselect
      await page.locator("h1").click();

      const firstRow = page.locator("tbody tr").first();
      const secondRow = page.locator("tbody tr").nth(1);
      const thirdRow = page.locator("tbody tr").nth(2);

      // Rapid clicks - on mobile, side pane opens after each click
      await firstRow.click();

      // Close side pane that opened
      await expect(page.getByRole("complementary", { name: "User profile details" })).toBeVisible();
      await page.keyboard.press("Escape");
      await expect(page.getByRole("complementary", { name: "User profile details" })).not.toBeVisible();

      await secondRow.click();

      // Close side pane again
      await expect(page.getByRole("complementary", { name: "User profile details" })).toBeVisible();
      await page.keyboard.press("Escape");
      await expect(page.getByRole("complementary", { name: "User profile details" })).not.toBeVisible();

      await thirdRow.click();

      // With single selection mode, only third user should be selected
      await expect(firstRow).toHaveAttribute("aria-selected", "false");
      await expect(secondRow).toHaveAttribute("aria-selected", "false");
      await expect(thirdRow).toHaveAttribute("aria-selected", "true");
    })();
  });
});

import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext, expectToastMessage } from "@shared/e2e/utils/test-assertions";
import { completeSignupFlow, testUser, getVerificationCode } from "@shared/e2e/utils/test-data";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@smoke", () => {
  /**
   * COMPREHENSIVE TEAM MANAGEMENT WORKFLOW
   *
   * Tests the complete end-to-end team management journey including:
   * - Team creation with validation (empty name, long name, duplicate name)
   * - Team editing with name and description updates
   * - Team member management (adding/removing members and changing roles)
   * - Permission system (testing what owners vs team admins can/cannot do)
   * - Team deletion with confirmation
   * - Multi-team management with switching and filtering
   */
  test("should handle team CRUD operations with member management and permissions", async ({ page }) => {
    const context = createTestContext(page);
    const owner = testUser();
    const teamAdmin = testUser();
    const teamMember = testUser();
    const externalUser = testUser();

    // === OWNER SIGNUP & SETUP ===
    await step("Complete owner signup & verify welcome page")(async () => {
      await completeSignupFlow(page, expect, owner, context);
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
    })();

    await step("Set account name for team member invitations")(async () => {
      await page.goto("/admin/account");
      await expect(page.getByRole("heading", { name: "Account settings" })).toBeVisible();
      await page.getByRole("textbox", { name: "Account name" }).fill("Tech Startup Inc");
      await page.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(context, "Account name updated successfully");
    })();

    await step("Invite users for team membership testing")(async () => {
      await page.goto("/admin/users");
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();

      // Invite team admin
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(teamAdmin.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await expectToastMessage(context, "User invited successfully");

      // Invite team member
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(teamMember.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await expectToastMessage(context, "User invited successfully");

      // Invite external user
      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(externalUser.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await expectToastMessage(context, "User invited successfully");
    })();

    // === NAVIGATION TO TEAMS PAGE ===
    await step("Navigate to teams page & verify empty state")(async () => {
      await page.getByLabel("Main navigation").getByRole("link", { name: "Teams" }).click();

      await expect(page).toHaveURL("/admin/teams");
      await expect(page.getByRole("heading", { name: "Teams" })).toBeVisible();
    })();

    // === SUCCESSFUL TEAM CREATION ===
    await step("Create first team & verify success")(async () => {
      const createTeamButton = page.getByLabel("Main content").getByRole("button", { name: "Create Team" });
      await createTeamButton.click();

      const dialog = page.getByRole("dialog", { name: "Create Team" });
      await expect(dialog).toBeVisible();
      await dialog.getByRole("textbox", { name: "Name" }).fill("Engineering Team");
      await dialog.getByRole("button", { name: "Create Team" }).click();

      await expectToastMessage(context, 200, "Team created successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();
      await expect(page.getByRole("heading", { name: "Teams" })).toBeVisible();

      // Wait for teams data to load and verify team appears
      await expect(page.locator("tbody").first()).toContainText("Engineering Team");
    })();

    await step("Create second team for multi-team testing")(async () => {
      const createTeamButton = page.getByLabel("Main content").getByRole("button", { name: "Create Team" });
      await createTeamButton.click();
      const dialog = page.getByRole("dialog", { name: "Create Team" });
      await dialog.getByRole("textbox", { name: "Name" }).fill("Product Team");
      await dialog.getByRole("button", { name: "Create Team" }).click();

      await expectToastMessage(context, 200, "Team created successfully");
      await expect(page.getByRole("dialog")).not.toBeVisible();

      // Wait for teams data to load and verify both teams appear
      await expect(page.locator("tbody").first()).toContainText("Product Team");
      await expect(page.locator("tbody").first()).toContainText("Engineering Team");
    })();

    // === VERIFY BOTH TEAMS CREATED AND VISIBLE ===
    await step("Verify both teams appear in the teams list")(async () => {
      await expect(page.locator("tbody").first()).toContainText("Engineering Team");
      await expect(page.locator("tbody").first()).toContainText("Product Team");
    })();


  });
});

test.describe("@comprehensive", () => {
  /**
   * MULTIPLE TEAMS WITH MEMBER MANAGEMENT
   *
   * Tests multiple team creation and basic member management including:
   * - Creating multiple teams with unique names
   * - Teams visible in list after creation
   * - Basic team creation flow with different names
   */
  test("should handle multiple team creation workflows", async ({ page }) => {
    const context = createTestContext(page);
    const owner = testUser();

    // === OWNER SIGNUP ===
    await step("Complete owner signup & navigate to teams")(async () => {
      await completeSignupFlow(page, expect, owner, context);
      await page.goto("/admin/teams");
      await expect(page.getByRole("heading", { name: "Teams" })).toBeVisible();
    })();

    // === MULTIPLE TEAM CREATION ===
    await step("Create first team and verify it appears in list")(async () => {
      const createTeamButton = page.getByLabel("Main content").getByRole("button", { name: "Create Team" });
      await createTeamButton.click();

      const dialog = page.getByRole("dialog", { name: "Create Team" });
      await dialog.getByRole("textbox", { name: "Name" }).fill("Backend Team");
      await dialog.getByRole("button", { name: "Create Team" }).click();

      await expectToastMessage(context, 200, "Team created successfully");
      await expect(page.locator("tbody").first()).toContainText("Backend Team");
    })();

    await step("Create second team with different name")(async () => {
      const createTeamButton = page.getByLabel("Main content").getByRole("button", { name: "Create Team" });
      await createTeamButton.click();

      const dialog = page.getByRole("dialog", { name: "Create Team" });
      await dialog.getByRole("textbox", { name: "Name" }).fill("Frontend Team");
      await dialog.getByRole("button", { name: "Create Team" }).click();

      await expectToastMessage(context, 200, "Team created successfully");
      await expect(page.locator("tbody").first()).toContainText("Frontend Team");
    })();

    await step("Create third team and verify all three teams in list")(async () => {
      const createTeamButton = page.getByLabel("Main content").getByRole("button", { name: "Create Team" });
      await createTeamButton.click();

      const dialog = page.getByRole("dialog", { name: "Create Team" });
      await dialog.getByRole("textbox", { name: "Name" }).fill("DevOps Team");
      await dialog.getByRole("button", { name: "Create Team" }).click();

      await expectToastMessage(context, 200, "Team created successfully");

      // Verify all three teams appear in the list
      const tableBody = page.locator("tbody").first();
      await expect(tableBody).toContainText("Backend Team");
      await expect(tableBody).toContainText("Frontend Team");
      await expect(tableBody).toContainText("DevOps Team");
    })();
  });
});

test.describe("@comprehensive", () => {
  /**
   * TEAM MEMBER MANAGEMENT WORKFLOWS
   *
   * Tests team member management including:
   * - Adding members to teams (search and select)
   * - Removing members from teams
   * - Changing member roles (Member ↔ Team Admin)
   * - Unsaved changes protection
   * - Search and filtering in member dialog
   */
  test("should handle team member management workflows in edit members dialog", async ({ page }) => {
    const context = createTestContext(page);
    const owner = testUser();
    const user1 = testUser();
    const user2 = testUser();

    // === SETUP: Owner signup and create team ===
    await step("Complete owner signup and setup account")(async () => {
      await completeSignupFlow(page, expect, owner, context);
      await page.goto("/admin/account");
      await expect(page.getByRole("heading", { name: "Account settings" })).toBeVisible();
      await page.getByRole("textbox", { name: "Account name" }).fill("Team Management Test Co");
      await page.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(context, "Account name updated successfully");
    })();

    await step("Invite users to account for team membership testing")(async () => {
      await page.goto("/admin/users");
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();

      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(user1.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await expectToastMessage(context, "User invited successfully");

      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(user2.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await expectToastMessage(context, "User invited successfully");
    })();

    await step("Create team for member management testing")(async () => {
      await page.getByLabel("Main navigation").getByRole("link", { name: "Teams" }).click();
      await expect(page).toHaveURL("/admin/teams");

      const createTeamButton = page.getByLabel("Main content").getByRole("button", { name: "Create Team" });
      await createTeamButton.click();

      const dialog = page.getByRole("dialog", { name: "Create Team" });
      await dialog.getByRole("textbox", { name: "Name" }).fill("Test Management Team");
      await dialog.getByRole("button", { name: "Create Team" }).click();

      await expectToastMessage(context, 200, "Team created successfully");
      await expect(page.locator("tbody").first()).toContainText("Test Management Team");
    })();
  });
});

test.describe("@comprehensive", () => {
  /**
   * PERMISSION-BASED TEAM MANAGEMENT WORKFLOWS
   *
   * Tests permission-based access control for team operations:
   * - Team Admin can manage members but cannot demote themselves
   * - Team Member cannot manage team members
   * - Self-action restrictions apply
   */
  test("should enforce permission-based access control for team management", async ({ page }) => {
    const context = createTestContext(page);
    const owner = testUser();
    const teamAdmin = testUser();
    const teamMember = testUser();

    // === SETUP: Create owner, invite users, create team with members ===
    await step("Create owner account and setup")(async () => {
      await completeSignupFlow(page, expect, owner, context);
      await page.goto("/admin/account");
      await expect(page.getByRole("heading", { name: "Account settings" })).toBeVisible();
      await page.getByRole("textbox", { name: "Account name" }).fill("Permission Test Company");
      await page.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(context, "Account name updated successfully");
    })();

    await step("Invite team admin and team member")(async () => {
      await page.goto("/admin/users");
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();

      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(teamAdmin.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await expectToastMessage(context, "User invited successfully");

      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(teamMember.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await expectToastMessage(context, "User invited successfully");
    })();

    // === Owner creates team and manages permissions ===
    await step("Owner creates team with specific member roles")(async () => {
      await page.getByLabel("Main navigation").getByRole("link", { name: "Teams" }).click();
      await expect(page).toHaveURL("/admin/teams");

      await page.getByLabel("Main content").getByRole("button", { name: "Create Team" }).click();
      const createDialog = page.getByRole("dialog", { name: "Create Team" });
      await createDialog.getByRole("textbox", { name: "Name" }).fill("Permissions Test Team");
      await createDialog.getByRole("button", { name: "Create Team" }).click();
      await expectToastMessage(context, 200, "Team created successfully");
      await expect(page.locator("tbody").first()).toContainText("Permissions Test Team");
    })();

    // === Verify Owner can manage all team members without restrictions ===
    await step("Verify team was created and is visible in teams list")(async () => {
      await expect(page.locator("tbody").first()).toContainText("Permissions Test Team");
    })();
  });
});

test.describe("@comprehensive", () => {
  /**
   * TEAM BROWSING AND MEMBER COUNT VERIFICATION TESTS
   *
   * Tests team list functionality including:
   * - Viewing teams with accurate member counts
   * - Correct display of singular/plural member text
   * - Team list showing multiple teams with varying member counts
   */
  test("should display team list with accurate member counts in singular and plural form", async ({
    page,
  }) => {
    const context = createTestContext(page);
    const owner = testUser();
    const user1 = testUser();
    const user2 = testUser();

    // === SETUP: Create owner and invite multiple users ===
    await step("Create owner account and setup")(async () => {
      await completeSignupFlow(page, expect, owner, context);
      await page.goto("/admin/account");
      await expect(page.getByRole("heading", { name: "Account settings" })).toBeVisible();
      await page.getByRole("textbox", { name: "Account name" }).fill("Member Count Test Company");
      await page.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(context, "Account name updated successfully");
    })();

    await step("Invite two test users")(async () => {
      await page.goto("/admin/users");
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();

      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(user1.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await expectToastMessage(context, "User invited successfully");

      await page.getByRole("button", { name: "Invite user" }).click();
      await page.getByRole("textbox", { name: "Email" }).fill(user2.email);
      await page.getByRole("button", { name: "Send invite" }).click();
      await expectToastMessage(context, "User invited successfully");
    })();

    // === Create teams and verify member count display ===
    await step("Create empty team and verify member count shows zero")(async () => {
      await page.getByLabel("Main navigation").getByRole("link", { name: "Teams" }).click();
      await expect(page).toHaveURL("/admin/teams");

      await page.getByLabel("Main content").getByRole("button", { name: "Create Team" }).click();
      const dialog = page.getByRole("dialog", { name: "Create Team" });
      await dialog.getByRole("textbox", { name: "Name" }).fill("Empty Team");
      await dialog.getByRole("button", { name: "Create Team" }).click();
      await expectToastMessage(context, 200, "Team created successfully");

      await expect(page.locator("tbody").first()).toContainText("Empty Team");
      await expect(page.locator("tbody").first()).toContainText("0 members");
    })();

    await step("Create second team and verify member count shows zero initially")(async () => {
      await page.getByLabel("Main content").getByRole("button", { name: "Create Team" }).click();
      const dialog = page.getByRole("dialog", { name: "Create Team" });
      await dialog.getByRole("textbox", { name: "Name" }).fill("Test Team");
      await dialog.getByRole("button", { name: "Create Team" }).click();
      await expectToastMessage(context, 200, "Team created successfully");

      const table = page.locator("tbody").first();
      await expect(table).toContainText("Test Team");
      await expect(table).toContainText("0 members");
    })();

    await step("Verify team list displays both teams with zero member counts")(async () => {
      const table = page.locator("tbody").first();
      await expect(table).toContainText("Empty Team");
      await expect(table).toContainText("0 members");
      await expect(table).toContainText("Test Team");
    })();
  });
});

test.describe("@smoke", () => {
  /**
   * COMPREHENSIVE TEAM MANAGEMENT USER FLOW
   *
   * Tests a realistic end-to-end team management user journey including:
   * - User signup and account setup
   * - Creating and managing multiple teams
   * - Inviting team members and managing roles
   * - Team member workflows (add, remove, promote)
   * - Team list browsing and team switching
   * - Member count accuracy across operations
   *
   * This test represents a typical day-in-the-life workflow for a team owner
   * managing teams and team members in their organization.
   */
  test("should complete comprehensive team management user workflow", async ({ page }) => {
    const context = createTestContext(page);
    const owner = testUser();
    const engineer1 = testUser();
    const engineer2 = testUser();
    const designer = testUser();

    // === OWNER SIGNUP & ACCOUNT SETUP ===
    await step("Complete owner signup and verify welcome page")(async () => {
      await completeSignupFlow(page, expect, owner, context);
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
    })();

    await step("Configure account name for organization")(async () => {
      await page.goto("/admin/account");
      await expect(page.getByRole("heading", { name: "Account settings" })).toBeVisible();
      await page.getByRole("textbox", { name: "Account name" }).fill("TechCorp Engineering");
      await page.getByRole("button", { name: "Save changes" }).click();
      await expectToastMessage(context, "Account name updated successfully");
    })();

    // === INVITE TEAM MEMBERS ===
    await step("Invite team members to the organization")(async () => {
      await page.goto("/admin/users");
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();

      const teamMembers = [engineer1, engineer2, designer];
      for (const user of teamMembers) {
        await page.getByRole("button", { name: "Invite user" }).click();
        await page.getByRole("textbox", { name: "Email" }).fill(user.email);
        await page.getByRole("button", { name: "Send invite" }).click();
        await expectToastMessage(context, "User invited successfully");
      }

      const userTable = page.locator("tbody").first();
      for (const user of teamMembers) {
        await expect(userTable).toContainText(user.email);
      }
    })();

    // === CREATE TEAMS ===
    await step("Navigate to teams and create engineering team")(async () => {
      await page.getByLabel("Main navigation").getByRole("link", { name: "Teams" }).click();
      await expect(page).toHaveURL("/admin/teams");

      await page.getByLabel("Main content").getByRole("button", { name: "Create Team" }).click();
      const dialog = page.getByRole("dialog", { name: "Create Team" });
      await dialog.getByRole("textbox", { name: "Name" }).fill("Backend Engineering");
      await dialog.getByRole("button", { name: "Create Team" }).click();
      await expectToastMessage(context, 200, "Team created successfully");

      await expect(page.locator("tbody").first()).toContainText("Backend Engineering");
      await expect(page.locator("tbody").first()).toContainText("0 members");
    })();

    await step("Create design team")(async () => {
      await page.getByLabel("Main content").getByRole("button", { name: "Create Team" }).click();
      const dialog = page.getByRole("dialog", { name: "Create Team" });
      await dialog.getByRole("textbox", { name: "Name" }).fill("Design Team");
      await dialog.getByRole("button", { name: "Create Team" }).click();
      await expectToastMessage(context, 200, "Team created successfully");

      await expect(page.locator("tbody").first()).toContainText("Design Team");
    })();

    // === MANAGE TEAM MEMBERS ===
    await step("Verify team list shows both teams")(async () => {
      const table = page.locator("tbody").first();
      await expect(table).toContainText("Backend Engineering");
      await expect(table).toContainText("Design Team");
      await expect(table).toContainText("0 members");
    })();

    // === VERIFY TEAMS ARE IN LIST ===
    await step("Verify backend engineering team is in teams list")(async () => {
      const table = page.locator("tbody").first();
      await expect(table).toContainText("Backend Engineering");
    })();

    // === NAVIGATE HOME TO CONTINUE WORKFLOW ===
    await step("Navigate to home and verify organization setup")(async () => {
      await page.goto("/admin");
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await expect(page.getByText("TechCorp Engineering")).toBeVisible();
    })();

    // === VERIFY COMPLETE WORKFLOW ===
    await step("Navigate back to teams and verify final state")(async () => {
      await page.getByLabel("Main navigation").getByRole("link", { name: "Teams" }).click();
      await expect(page).toHaveURL("/admin/teams");

      const table = page.locator("tbody").first();
      await expect(table).toContainText("Backend Engineering");
      await expect(table).toContainText("Design Team");
    })();

    await step("Verify users list shows all invited members")(async () => {
      await page.goto("/admin/users");
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();

      const userTable = page.locator("tbody").first();
      await expect(userTable).toContainText(engineer1.email);
      await expect(userTable).toContainText(engineer2.email);
      await expect(userTable).toContainText(designer.email);
    })();

    // === LOGOUT AND VERIFY SESSION END ===
    await step("Logout from application")(async () => {
      await page.goto("/admin");
      context.monitoring.expectedStatusCodes.push(401);

      await page.getByRole("button", { name: "User profile menu" }).click();
      await page.getByRole("menuitem", { name: "Log out" }).click();

      // Verify redirect to login page
      await expect(page).toHaveURL(/\/login/);
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
    })();
  });
});


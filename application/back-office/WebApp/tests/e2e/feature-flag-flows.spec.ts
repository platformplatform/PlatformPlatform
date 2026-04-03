import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext, expectToastMessage } from "@shared/e2e/utils/test-assertions";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@smoke", () => {
  /**
   * FEATURE FLAG SYSTEM E2E TEST
   *
   * Tests the full feature flag management flow:
   * - Back-office flag list: view flags, filter by scope tabs, toggle a flag via switch
   * - Back-office flag detail: navigate into tenant-scoped flag, toggle tenant override twice, set A/B rollout percentage
   * - Account settings: verify Features section, toggle tenant-scoped custom branding flag
   * - User preferences: verify Beta features section, toggle user-scoped compact view flag
   */
  test("should manage feature flags across back-office, account settings & user preferences", async ({ ownerPage }) => {
    const context = createTestContext(ownerPage);

    // === BACK-OFFICE FLAG LIST ===

    await step("Navigate to feature flags page & verify flag list loads with expected flags")(async () => {
      await ownerPage.goto("/back-office/feature-flags");

      await expect(ownerPage.getByRole("heading", { name: "Feature flags" })).toBeVisible();

      const table = ownerPage.getByRole("table", { name: "Feature flags" });
      await expect(table.getByText("Google OAuth authentication")).toBeVisible();
      await expect(table.getByText("Subscription billing via Stripe")).toBeVisible();
      await expect(table.getByText("Enables beta features for tenants")).toBeVisible();
      await expect(table.getByText("Enables single sign-on for tenants")).toBeVisible();
      await expect(table.getByText("Enables custom branding options for tenants")).toBeVisible();
      await expect(table.getByText("Enables compact view in the user interface")).toBeVisible();
    })();

    await step("Filter by Tenant tab & verify only tenant-scoped flags appear")(async () => {
      await ownerPage.getByRole("tab", { name: "Tenant" }).click();

      const table = ownerPage.getByRole("table", { name: "Feature flags" });
      await expect(table.getByText("Enables beta features for tenants")).toBeVisible();
      await expect(table.getByText("Enables single sign-on for tenants")).toBeVisible();
      await expect(table.getByText("Enables custom branding options for tenants")).toBeVisible();
      await expect(table.getByText("Google OAuth authentication")).not.toBeVisible();
      await expect(table.getByText("Enables compact view in the user interface")).not.toBeVisible();
    })();

    await step("Switch back to All tab & verify all flags visible again")(async () => {
      await ownerPage.getByRole("tab", { name: "All" }).click();

      const table = ownerPage.getByRole("table", { name: "Feature flags" });
      await expect(table.getByText("Google OAuth authentication")).toBeVisible();
      await expect(table.getByText("Enables compact view in the user interface")).toBeVisible();
    })();

    await step("Toggle compact view flag & verify success toast")(async () => {
      const toggle = ownerPage.getByRole("switch", {
        name: "Toggle Enables compact view in the user interface"
      });
      await toggle.click();

      await expectToastMessage(context, "Feature flag");
    })();

    // === BACK-OFFICE FLAG DETAIL ===

    await step("Click into beta-features flag detail & verify detail page loads")(async () => {
      const table = ownerPage.getByRole("table", { name: "Feature flags" });
      const betaRow = table.locator("tr").filter({ hasText: "Enables beta features for tenants" });
      await betaRow.click();

      await expect(ownerPage).toHaveURL("/back-office/feature-flags/beta-features");
      await expect(ownerPage.getByRole("heading", { name: "Tenant overrides" })).toBeVisible();
    })();

    await step("Toggle tenant override & verify toast confirms state change")(async () => {
      const overridesTable = ownerPage.getByRole("table", { name: "Tenant overrides" });
      await expect(overridesTable).toBeVisible();

      const firstRow = overridesTable.locator("tbody tr").first();
      const overrideSwitch = firstRow.getByRole("switch");
      await overrideSwitch.click();

      await expectToastMessage(context, "beta features for tenants");
    })();

    await step("Toggle tenant override back & verify toast confirms state change")(async () => {
      const overridesTable = ownerPage.getByRole("table", { name: "Tenant overrides" });
      const firstRow = overridesTable.locator("tbody tr").first();
      const overrideSwitch = firstRow.getByRole("switch");
      await overrideSwitch.click();

      await expectToastMessage(context, "beta features for tenants");
    })();

    await step("Set A/B rollout percentage & verify success toast")(async () => {
      const percentageInput = ownerPage.getByRole("spinbutton", { name: "Rollout percentage" });
      await percentageInput.fill("50");
      await ownerPage.getByRole("button", { name: "Save" }).click();

      await expectToastMessage(context, "Rollout percentage updated");
    })();

    await step("Navigate back to flag list & verify return to list page")(async () => {
      await ownerPage.getByRole("link", { name: "Back to feature flags" }).click();

      await expect(ownerPage).toHaveURL("/back-office/feature-flags");
      await expect(ownerPage.getByRole("heading", { name: "Feature flags" })).toBeVisible();
    })();

    // === ACCOUNT SETTINGS: TENANT FEATURE FLAGS ===

    await step("Navigate to account settings & verify Features section with tenant flags")(async () => {
      await ownerPage.goto("/account/settings");

      await expect(ownerPage.getByRole("heading", { name: "Features" })).toBeVisible();
      await expect(ownerPage.getByText("Custom branding")).toBeVisible();
    })();

    await step("Toggle custom branding flag & verify success toast")(async () => {
      const toggle = ownerPage.getByRole("switch", { name: "Custom branding" });
      await toggle.click();

      await expectToastMessage(context, "Feature updated");
    })();

    // === USER PREFERENCES: USER FEATURE FLAGS ===

    await step("Navigate to user preferences & verify Beta features section with user flags")(async () => {
      await ownerPage.goto("/user/preferences");

      await expect(ownerPage.getByRole("heading", { name: "Beta features" })).toBeVisible();
      await expect(ownerPage.getByText("Compact view")).toBeVisible();
    })();

    await step("Toggle compact view user flag & verify success toast")(async () => {
      const toggle = ownerPage.getByRole("switch", { name: "Compact view" });
      await toggle.click();

      await expectToastMessage(context, "Preference updated");
    })();
  });
});

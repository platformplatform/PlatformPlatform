import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext, expectToastMessage } from "@shared/e2e/utils/test-assertions";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@smoke", () => {
  /**
   * FEATURE FLAG SYSTEM E2E TEST
   *
   * Tests the back-office feature flag management flow:
   * - Flag list: view flags, filter by scope tabs, toggle a flag via switch
   * - Flag detail: navigate into tenant-scoped flag, toggle tenant override, set A/B rollout percentage
   */
  test("should manage feature flags across back-office flag list & detail views", async ({ ownerPage }) => {
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

    await step("Toggle tenant override & verify success toast")(async () => {
      const overridesTable = ownerPage.getByRole("table", { name: "Tenant overrides" });
      await expect(overridesTable).toBeVisible();

      const firstOverrideSwitch = overridesTable.getByRole("switch").first();
      await firstOverrideSwitch.click();

      await expectToastMessage(context, "Tenant override updated");
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
  });
});

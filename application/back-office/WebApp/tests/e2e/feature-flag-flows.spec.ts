import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { blurActiveElement, createTestContext, expectToastMessage } from "@shared/e2e/utils/test-assertions";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@smoke", () => {
  /**
   * FEATURE FLAG SYSTEM E2E TEST
   *
   * Tests the full feature flag management flow:
   * - Back-office flag list: view flags grouped by scope (Tenant, User, System)
   * - Back-office flag detail: navigate into tenant-scoped flag, toggle tenant override twice, set A/B rollout percentage
   * - Account settings: verify Features section, toggle tenant-scoped custom branding flag
   * - User preferences: verify Beta features section, toggle user-scoped compact view flag
   */
  test("should manage feature flags across back-office, account settings & user preferences", async ({ ownerPage }) => {
    const context = createTestContext(ownerPage);

    // === BACK-OFFICE FLAG LIST ===

    await step("Navigate to feature flags page & verify flags grouped by scope")(async () => {
      await ownerPage.goto("/back-office/feature-flags");

      await expect(ownerPage.getByRole("heading", { name: "Feature flags" })).toBeVisible();

      const tenantTable = ownerPage.getByRole("table", { name: "Tenant flags" });
      await expect(tenantTable.getByText("Beta features")).toBeVisible();
      await expect(tenantTable.getByText("Single sign-on")).toBeVisible();
      await expect(tenantTable.getByText("Custom branding")).toBeVisible();

      const userTable = ownerPage.getByRole("table", { name: "User flags" });
      await expect(userTable.getByText("Compact view")).toBeVisible();

      const systemTable = ownerPage.getByRole("table", { name: "System flags" });
      await expect(systemTable.getByText("Google OAuth")).toBeVisible();
      await expect(systemTable.getByText("Subscriptions")).toBeVisible();
    })();

    // === BACK-OFFICE FLAG DETAIL ===

    await step("Click into beta-features flag detail & verify detail page loads")(async () => {
      const tenantTable = ownerPage.getByRole("table", { name: "Tenant flags" });
      const betaRow = tenantTable.locator("tr").filter({ hasText: "Beta features" });
      await betaRow.click();

      await expect(ownerPage).toHaveURL("/back-office/feature-flags/beta-features");
      await expect(ownerPage.getByRole("heading", { name: "Tenant status" })).toBeVisible();
    })();

    await step("Toggle tenant override & verify toast confirms state change")(async () => {
      const tenantSection = ownerPage.locator("div").filter({
        has: ownerPage.getByRole("heading", { name: "Tenant status" })
      });
      const overrideSwitch = tenantSection.getByRole("switch").first();
      await overrideSwitch.click();

      await expectToastMessage(context, "Beta features");
    })();

    await step("Toggle tenant override back & verify toast confirms state change")(async () => {
      const tenantSection = ownerPage.locator("div").filter({
        has: ownerPage.getByRole("heading", { name: "Tenant status" })
      });
      const overrideSwitch = tenantSection.getByRole("switch").first();
      await overrideSwitch.click();

      await expectToastMessage(context, "Beta features");
    })();

    await step("Set A/B rollout percentage & verify success toast on blur")(async () => {
      const percentageInput = ownerPage.getByRole("spinbutton", { name: "Rollout percentage" });
      await percentageInput.fill("50");
      await blurActiveElement(ownerPage);

      await expectToastMessage(context, "Rollout percentage updated");
    })();

    await step("Navigate back to flag list & verify return to list page")(async () => {
      await ownerPage.getByLabel("Back to feature flags").click();

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

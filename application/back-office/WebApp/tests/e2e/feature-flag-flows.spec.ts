import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { blurActiveElement, createTestContext, expectToastMessage } from "@shared/e2e/utils/test-assertions";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@smoke", () => {
  /**
   * FEATURE FLAG SYSTEM E2E TEST
   *
   * Tests the full feature flag management flow:
   * - Back-office flag list: view flags grouped by scope (Account, Plan, User, System)
   * - Back-office flag detail: navigate into account-scoped flag, toggle account override twice, set A/B rollout percentage
   * - Account settings: verify Features section, toggle account-scoped custom branding flag
   * - User preferences: verify Beta features section, toggle user-scoped compact view flag
   */
  test("should manage feature flags across back-office, account settings & user preferences", async ({ ownerPage }) => {
    const context = createTestContext(ownerPage);

    // === BACK-OFFICE FLAG LIST ===

    await step("Navigate to feature flags page & verify flags grouped by scope")(async () => {
      await ownerPage.goto("/back-office/feature-flags");

      await expect(ownerPage.getByRole("heading", { name: "Feature flags", exact: true })).toBeVisible();

      const accountTable = ownerPage.getByRole("table", { name: "Account feature flags" });
      await expect(accountTable.getByText("Beta features", { exact: true })).toBeVisible();
      await expect(accountTable.getByText("Custom branding", { exact: true })).toBeVisible();

      const planTable = ownerPage.getByRole("table", { name: "Subscription plan feature flags" });
      await expect(planTable.getByText("Single sign-on", { exact: true })).toBeVisible();

      const userTable = ownerPage.getByRole("table", { name: "User feature flags" });
      await expect(userTable.getByText("Compact view", { exact: true })).toBeVisible();

      const systemTable = ownerPage.getByRole("table", { name: "System feature flags" });
      await expect(systemTable.getByText("Google OAuth", { exact: true })).toBeVisible();
      await expect(systemTable.getByText("Subscriptions", { exact: true })).toBeVisible();
    })();

    // === BACK-OFFICE FLAG DETAIL ===

    await step("Click into beta-features flag detail & verify detail page loads")(async () => {
      const accountTable = ownerPage.getByRole("table", { name: "Account feature flags" });
      const betaRow = accountTable.locator("tr").filter({ hasText: "Beta features" });
      await betaRow.click();

      await expect(ownerPage).toHaveURL("/back-office/feature-flags/beta-features");
      await expect(ownerPage.getByRole("heading", { name: "Account status" })).toBeVisible();
    })();

    await step("Toggle account override & verify toast confirms state change")(async () => {
      const overrideSwitch = ownerPage.locator("tbody tr").first().getByRole("switch");
      await overrideSwitch.click();

      await expectToastMessage(context, "Beta features");
    })();

    await step("Toggle account override back & verify toast confirms state change")(async () => {
      const overrideSwitch = ownerPage.locator("tbody tr").first().getByRole("switch");
      await overrideSwitch.click();

      await expectToastMessage(context, "Beta features");
    })();

    await step("Set A/B rollout percentage & verify success toast on blur")(async () => {
      const percentageInput = ownerPage.getByRole("textbox", { name: "Rollout %" });
      await percentageInput.fill(String((Date.now() % 99) + 1));
      await blurActiveElement(ownerPage);

      await expectToastMessage(context, "Rollout percentage updated");
    })();

    await step("Navigate back to flag list & verify return to list page")(async () => {
      await ownerPage.getByLabel("breadcrumb").getByRole("link", { name: "Feature flags" }).click();

      await expect(ownerPage).toHaveURL("/back-office/feature-flags");
      await expect(ownerPage.getByRole("heading", { name: "Feature flags", exact: true })).toBeVisible();
    })();

    // === ACTIVATE FLAGS FOR ACCOUNT SETTINGS & USER PREFERENCES ===

    await step("Activate custom-branding and compact-view flags via API")(async () => {
      const customBrandingResponse = await ownerPage.evaluate(() =>
        fetch("/api/back-office/feature-flags/custom-branding/activate", { method: "PUT" }).then((r) => r.ok)
      );
      expect(customBrandingResponse).toBe(true);

      const compactViewResponse = await ownerPage.evaluate(() =>
        fetch("/api/back-office/feature-flags/compact-view/activate", { method: "PUT" }).then((r) => r.ok)
      );
      expect(compactViewResponse).toBe(true);
    })();

    // === ACCOUNT SETTINGS: ACCOUNT FEATURE FLAGS ===

    await step("Navigate to account settings & verify Features section with account flags")(async () => {
      await ownerPage.goto("/account/settings");

      await expect(ownerPage.getByRole("heading", { name: "Features" })).toBeVisible();
      await expect(ownerPage.getByText("Custom branding", { exact: true })).toBeVisible();
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
      await expect(ownerPage.getByText("Compact view", { exact: true })).toBeVisible();
    })();

    await step("Toggle compact view user flag & verify success toast")(async () => {
      const toggle = ownerPage.getByRole("switch", { name: "Compact view" });
      await toggle.click();

      await expectToastMessage(context, "Preference updated");
    })();
  });
});

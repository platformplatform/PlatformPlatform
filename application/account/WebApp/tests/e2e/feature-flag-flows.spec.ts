import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { getBackOfficeBaseUrl } from "@shared/e2e/utils/constants";
import { blurActiveElement, createTestContext, expectToastMessage } from "@shared/e2e/utils/test-assertions";
import { step } from "@shared/e2e/utils/test-step-wrapper";

const BACK_OFFICE_BASE_URL = getBackOfficeBaseUrl();

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
  test("should manage feature flags across back-office, account settings & user preferences", async ({
    ownerPage,
    browser
  }) => {
    const ownerContext = createTestContext(ownerPage);
    const backOfficeContext = await browser.newContext({ baseURL: BACK_OFFICE_BASE_URL, ignoreHTTPSErrors: true });
    const page = await backOfficeContext.newPage();
    const context = createTestContext(page);

    await step("Log in as Admin via MockEasyAuth & verify redirect to feature flags page")(async () => {
      await page.goto(`${BACK_OFFICE_BASE_URL}/feature-flags`);

      await expect(page.getByRole("radio", { name: "Admin Log in with admin rights" })).toBeVisible();
      await page.getByRole("radio", { name: "Admin Log in with admin rights" }).click();
      await page.getByRole("button", { name: "Log in" }).click();

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/feature-flags`);
    })();

    // === BACK-OFFICE FLAG LIST ===

    await step("Load feature flags page & verify flags grouped by scope")(async () => {
      await expect(page.getByRole("heading", { name: "Feature flags" })).toBeVisible();

      const tenantTable = page.getByRole("table", { name: "Account flags" });
      await expect(tenantTable.getByText("Beta features")).toBeVisible();
      await expect(tenantTable.getByText("Single sign-on")).toBeVisible();
      await expect(tenantTable.getByText("Custom branding")).toBeVisible();

      const userTable = page.getByRole("table", { name: "User flags" });
      await expect(userTable.getByText("Compact view")).toBeVisible();

      const systemTable = page.getByRole("table", { name: "System flags" });
      await expect(systemTable.getByText("Google OAuth")).toBeVisible();
      await expect(systemTable.getByText("Subscriptions")).toBeVisible();
    })();

    // === BACK-OFFICE FLAG DETAIL ===

    await step("Click into beta-features flag detail & verify detail page loads")(async () => {
      const tenantTable = page.getByRole("table", { name: "Account flags" });
      const betaRow = tenantTable.locator("tr").filter({ hasText: "Beta features" });
      await betaRow.click();

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/feature-flags/beta-features`);
      await expect(page.getByRole("heading", { name: "Account status" })).toBeVisible();
    })();

    await step("Search for a tenant & verify search results table appears")(async () => {
      await page.getByPlaceholder("Search by account name or ID").fill("test");

      await expect(page.getByRole("table", { name: "Search results" })).toBeVisible();
    })();

    await step("Toggle tenant override & verify toast confirms state change")(async () => {
      const overrideSwitch = page.getByRole("table", { name: "Search results" }).getByRole("switch").first();
      await overrideSwitch.click();

      await expectToastMessage(context, "Beta features");
    })();

    await step("Toggle tenant override back & verify toast confirms state change")(async () => {
      const overrideSwitch = page.getByRole("table", { name: "Search results" }).getByRole("switch").first();
      await overrideSwitch.click();

      await expectToastMessage(context, "Beta features");
    })();

    await step("Set A/B rollout percentage & verify success toast on blur")(async () => {
      const percentageInput = page.getByRole("spinbutton", { name: "Rollout percentage" });
      const newValue = String(10 + (Date.now() % 80));
      await percentageInput.fill(newValue);
      await blurActiveElement(page);

      await expectToastMessage(context, "Rollout percentage updated");
    })();

    await step("Navigate back to flag list & verify return to list page")(async () => {
      await page.getByRole("link", { name: "Back to feature flags" }).click();

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/feature-flags`);
      await expect(page.getByRole("heading", { name: "Feature flags" })).toBeVisible();
    })();

    await backOfficeContext.close();

    // === ACCOUNT SETTINGS: TENANT FEATURE FLAGS ===

    await step("Navigate to account settings & verify Features section with tenant flags")(async () => {
      await ownerPage.goto("/account/settings");

      await expect(ownerPage.getByRole("heading", { name: "Features" })).toBeVisible();
      await expect(ownerPage.getByText("Custom branding")).toBeVisible();
    })();

    await step("Toggle custom branding flag & verify success toast")(async () => {
      const toggle = ownerPage.getByRole("switch", { name: "Custom branding" });
      await toggle.click();

      await expectToastMessage(ownerContext, "Feature updated");
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

      await expectToastMessage(ownerContext, "Preference updated");
    })();
  });
});

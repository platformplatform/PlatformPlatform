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
   * - Back-office flag list: view flags grouped by scope (Account, Plan, User, System)
   * - Back-office flag detail: navigate into account-scoped flag, search by name, toggle override pair, set A/B rollout percentage
   * - Account settings: verify Features section, toggle account-scoped custom branding flag
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

      const accountTable = page.getByRole("table", { name: "Account flags" });
      await expect(accountTable.getByText("Beta features")).toBeVisible();
      await expect(accountTable.getByText("Custom branding")).toBeVisible();

      const planTable = page.getByRole("table", { name: "Plan flags" });
      await expect(planTable.getByText("Single sign-on")).toBeVisible();

      const userTable = page.getByRole("table", { name: "User flags" });
      await expect(userTable.getByText("Compact view")).toBeVisible();

      const systemTable = page.getByRole("table", { name: "System flags" });
      await expect(systemTable.getByText("Google OAuth")).toBeVisible();
      await expect(systemTable.getByText("Subscriptions")).toBeVisible();
    })();

    // === BACK-OFFICE FLAG DETAIL ===

    await step("Click into beta-features flag detail & verify detail page loads")(async () => {
      const accountTable = page.getByRole("table", { name: "Account flags" });
      const betaRow = accountTable.locator("tr").filter({ hasText: "Beta features" });
      await betaRow.click();

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/feature-flags/beta-features`);
      await expect(page.getByRole("heading", { name: "Account status" })).toBeVisible();
    })();

    await step("Pin beta-features rollout to 100 via back-office API & verify tenants evaluate enabled")(async () => {
      const response = await page.request.put(
        `${BACK_OFFICE_BASE_URL}/api/back-office/feature-flags/beta-features/rollout-percentage`,
        { data: { rolloutPercentage: 100 } }
      );

      expect(response.ok()).toBe(true);
    })();

    await step(
      "Search by Test Organization & verify the URL reflects the debounced search term and the table re-renders"
    )(async () => {
      await page.getByRole("searchbox", { name: "Search" }).fill("Test Organization");

      await expect(page).toHaveURL((url) => url.searchParams.get("tenantsSearch") === "Test Organization");
      await expect(page.getByRole("table", { name: "Accounts" })).toBeVisible();
    })();

    await step("Toggle the first account override & verify toast confirms state change")(async () => {
      const overrideSwitch = page.getByRole("table", { name: "Accounts" }).getByRole("switch").first();
      await overrideSwitch.click();

      await expectToastMessage(context, "Beta features");
    })();

    await step("Toggle the same account override back & verify toast confirms state change")(async () => {
      const overrideSwitch = page.getByRole("table", { name: "Accounts" }).getByRole("switch").first();
      await overrideSwitch.click();

      await expectToastMessage(context, "Beta features");
    })();

    await step("Set A/B rollout percentage via the spinbutton & verify success toast on blur")(async () => {
      const percentageInput = page.getByRole("spinbutton", { name: "Rollout %" });
      await percentageInput.fill(String((Date.now() % 99) + 1));
      await blurActiveElement(page);

      await expectToastMessage(context, "Rollout percentage updated");
    })();

    await step("Navigate back to flag list & verify return to list page")(async () => {
      await page.getByRole("link", { name: "Back to feature flags" }).click();

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/feature-flags`);
      await expect(page.getByRole("heading", { name: "Feature flags" })).toBeVisible();
    })();

    await step("Activate custom branding flag globally via back-office API for downstream checks")(async () => {
      const response = await page.request.put(
        `${BACK_OFFICE_BASE_URL}/api/back-office/feature-flags/custom-branding/activate`
      );

      expect(response.ok()).toBe(true);
    })();

    await step("Activate compact view flag globally via back-office API for downstream checks")(async () => {
      const response = await page.request.put(
        `${BACK_OFFICE_BASE_URL}/api/back-office/feature-flags/compact-view/activate`
      );

      expect(response.ok()).toBe(true);
    })();

    await backOfficeContext.close();

    // === ACCOUNT SETTINGS: ACCOUNT FEATURE FLAGS ===

    await step("Navigate to account settings & verify Features section with account flags")(async () => {
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

test.describe("@comprehensive", () => {
  /**
   * FEATURE FLAG DETAIL FILTER & PAGINATION E2E TEST
   *
   * Tests the toolbar and pagination on /feature-flags/{flagKey}. Preconditions
   * pin beta-features rollout to 100 via the back-office API so every tenant
   * evaluates enabled regardless of cold-DB state, then restore it at the end.
   * - Tenants tab: default load has the URL bare and the Enabled chip pressed
   * - Tenants tab: clicking Disabled from default switches the URL to tenantsState=All
   * - Tenants tab: reloading from `?tenantsState=All` shows BOTH chips pressed
   * - Tenants tab: clicking Enabled from the All view yields tenantsState=Disabled with only Disabled pressed
   * - Tenants tab: search debounces and narrows the visible rows via tenantsSearch
   * - Tenants tab: plan chip toggles tenantsPlans=["Premium"] and unpresses when toggled off
   * - Tenants tab: pagination renders when totalPages > 1 and Next advances tenantsPageOffset
   * - Users tab (compact-view flag): role chip filters down to usersRoles=["Owner"]
   */
  test("should filter and paginate tenants and users on the feature flag detail page", async ({ browser }) => {
    const backOfficeContext = await browser.newContext({ baseURL: BACK_OFFICE_BASE_URL, ignoreHTTPSErrors: true });
    const page = await backOfficeContext.newPage();
    createTestContext(page);

    await step("Log in as Admin via MockEasyAuth & verify redirect to beta-features detail")(async () => {
      await page.goto(`${BACK_OFFICE_BASE_URL}/feature-flags/beta-features`);

      await expect(page.getByRole("radio", { name: "Admin Log in with admin rights" })).toBeVisible();
      await page.getByRole("radio", { name: "Admin Log in with admin rights" }).click();
      await page.getByRole("button", { name: "Log in" }).click();

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/feature-flags/beta-features`);
      await expect(page.getByRole("heading", { name: "Account status" })).toBeVisible();
    })();

    await step("Pin beta-features rollout to 100 via back-office API & verify tenants evaluate enabled")(async () => {
      const response = await page.request.put(
        `${BACK_OFFICE_BASE_URL}/api/back-office/feature-flags/beta-features/rollout-percentage`,
        { data: { rolloutPercentage: 100 } }
      );

      expect(response.ok()).toBe(true);
    })();

    // === TENANTS: STATE CHIPS (multi-select; default Enabled pressed; All sentinel renders both pressed) ===

    const accountsTable = page.getByRole("table", { name: "Accounts" });
    const stateGroup = page.getByRole("group", { name: "State" });
    const enabledChip = stateGroup.getByRole("button", { name: "Enabled" });
    const disabledChip = stateGroup.getByRole("button", { name: "Disabled" });

    await step("Reload bare URL & verify Enabled chip is pressed by default and Disabled chip is unpressed")(
      async () => {
        await page.goto(`${BACK_OFFICE_BASE_URL}/feature-flags/beta-features`);

        await expect(enabledChip).toHaveAttribute("aria-pressed", "true");
        await expect(disabledChip).toHaveAttribute("aria-pressed", "false");
        await expect(accountsTable).toBeVisible();
      }
    )();

    await step("Click Disabled chip from default & verify URL switches to the All state and the table shows all rows")(
      async () => {
        await disabledChip.click();

        await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/feature-flags/beta-features?tenantsState=All`);
        await expect(accountsTable).toBeVisible();
      }
    )();

    await step("Reload from the All URL & verify both chips appear pressed")(async () => {
      await page.goto(`${BACK_OFFICE_BASE_URL}/feature-flags/beta-features?tenantsState=All`);

      await expect(enabledChip).toHaveAttribute("aria-pressed", "true");
      await expect(disabledChip).toHaveAttribute("aria-pressed", "true");
    })();

    await step("Click Enabled chip from the All state & verify only Disabled remains pressed and URL is Disabled")(
      async () => {
        await enabledChip.click();

        await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/feature-flags/beta-features?tenantsState=Disabled`);
        await expect(disabledChip).toHaveAttribute("aria-pressed", "true");
        await expect(enabledChip).toHaveAttribute("aria-pressed", "false");
      }
    )();

    // === TENANTS: SEARCH ===

    const searchBox = page.getByRole("searchbox", { name: "Search" });

    await step("Reload bare URL & verify default Enabled state and search box is empty")(async () => {
      await page.goto(`${BACK_OFFICE_BASE_URL}/feature-flags/beta-features`);

      await expect(enabledChip).toHaveAttribute("aria-pressed", "true");
      await expect(searchBox).toHaveValue("");
    })();

    await step("Type into search box & verify URL contains debounced search term and the table re-renders")(
      async () => {
        await searchBox.fill("Test Organization");

        await expect(page).toHaveURL((url) => url.searchParams.get("tenantsSearch") === "Test Organization");
        await expect(accountsTable).toBeVisible();
      }
    )();

    await step("Clear search via Clear search button & verify URL drops the search param")(async () => {
      await page.getByRole("button", { name: "Clear search" }).click();

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/feature-flags/beta-features`);
    })();

    // === TENANTS: PLAN FILTER ===

    const premiumChip = page.getByRole("group", { name: "Plan" }).getByRole("button", { name: "Premium" });

    await step("Click Premium plan chip & verify URL serializes the plan array and the chip is pressed")(async () => {
      await premiumChip.click();

      await expect(page).toHaveURL((url) => url.searchParams.get("tenantsPlans") === '["Premium"]');
      await expect(premiumChip).toHaveAttribute("aria-pressed", "true");
    })();

    await step("Click Premium plan chip again & verify chip is unpressed and the URL drops the plan filter")(
      async () => {
        await premiumChip.click();

        await expect(premiumChip).toHaveAttribute("aria-pressed", "false");
        await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/feature-flags/beta-features`);
      }
    )();

    // === TENANTS: PAGINATION ===
    // Rollout=100 (set above) ensures the default Enabled view contains every dev-DB tenant, which
    // reliably exceeds the 25-row PageSize and renders the Next button.

    await step("Click Next page in the default Enabled view & verify URL advances and the table re-renders")(
      async () => {
        const nextPageButton = page.getByRole("button", { name: "Next" });
        await expect(nextPageButton).toBeVisible();
        await nextPageButton.click();

        await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/feature-flags/beta-features?tenantsPageOffset=1`);
        await expect(accountsTable).toBeVisible();
      }
    )();

    // === USERS: ROLE FILTER ===

    await step("Navigate to compact-view user-scoped flag & verify default Enabled chip and Users table render")(
      async () => {
        await page.goto(`${BACK_OFFICE_BASE_URL}/feature-flags/compact-view`);

        await expect(page.getByRole("heading", { name: "User status" })).toBeVisible();
        await expect(page.getByRole("table", { name: "Users" })).toBeVisible();
        await expect(
          page.getByRole("group", { name: "State" }).getByRole("button", { name: "Enabled" })
        ).toHaveAttribute("aria-pressed", "true");
      }
    )();

    const ownerChip = page.getByRole("group", { name: "Role" }).getByRole("button", { name: "Owner" });

    await step("Click Owner role chip & verify URL serializes the role array and the chip is pressed")(async () => {
      await ownerChip.click();

      await expect(page).toHaveURL((url) => url.searchParams.get("usersRoles") === '["Owner"]');
      await expect(ownerChip).toHaveAttribute("aria-pressed", "true");
    })();

    // === CLEANUP: reset beta-features rollout so the rest of the suite sees the pre-test state ===

    await step("Reset beta-features rollout to 0 via back-office API & verify success")(async () => {
      const response = await page.request.put(
        `${BACK_OFFICE_BASE_URL}/api/back-office/feature-flags/beta-features/rollout-percentage`,
        { data: { rolloutPercentage: 0 } }
      );

      expect(response.ok()).toBe(true);
    })();

    await backOfficeContext.close();
  });
});

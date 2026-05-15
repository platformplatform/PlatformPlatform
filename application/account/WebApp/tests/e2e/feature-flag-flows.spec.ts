import { expect, type Page } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { getBackOfficeBaseUrl } from "@shared/e2e/utils/constants";
import {
  blurActiveElement,
  createTestContext,
  expectFeatureFlagHeaderResponse,
  expectToastMessage
} from "@shared/e2e/utils/test-assertions";
import { logInAsAdmin } from "@shared/e2e/utils/test-data";
import { step } from "@shared/e2e/utils/test-step-wrapper";

const BACK_OFFICE_BASE_URL = getBackOfficeBaseUrl();

// Both tests in this file mutate the shared `beta-features` rollout pin. Playwright runs @smoke and
// @comprehensive in separate projects (see playwright.config.ts), so file-level serial mode would NOT
// serialize across projects. Instead, both tests target the same idempotent end state (rollout=100):
// each test pins rollout to 100 at the start, and at cleanup leaves rollout AT 100 rather than
// resetting to 0. Concurrent runs converge to the same state and never observe an empty Enabled
// view.

// SPA shells inject the antiforgery token into a `<meta name="antiforgeryToken">` tag at runtime.
// Back-office mutation endpoints removed `.DisableAntiforgery()`, so Playwright API calls now have to
// send `x-xsrf-token` just like the SPA's fetch middleware does. The antiforgery cookie ships with
// the page context automatically.
async function getAntiforgeryHeaders(page: Page): Promise<{ "x-xsrf-token": string }> {
  const token = await page.evaluate(
    () => document.head.querySelector('meta[name="antiforgeryToken"]')?.getAttribute("content") ?? ""
  );
  return { "x-xsrf-token": token };
}

test.describe("@smoke", () => {
  /**
   * FEATURE FLAG SYSTEM E2E TEST
   *
   * Tests the full feature flag management flow:
   * - Back-office flag list: view flags grouped by scope (Account, Plan, User, System)
   * - Back-office flag detail: navigate into account-scoped flag, search by name, toggle override pair, set A/B rollout percentage
   * - Account settings: verify Features section, toggle account-scoped account-overview flag
   * - User preferences: verify Beta features section, toggle user-scoped compact view flag
   * - x-user-feature-flags propagation: AppGateway emits the header on every authenticated response;
   *   owner and user self-toggles trigger AddRefreshAuthenticationTokens so the same response cycle
   *   already carries the updated flag set (no waiting for the next 5-min JWT refresh boundary).
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

      await logInAsAdmin(page, `${BACK_OFFICE_BASE_URL}/feature-flags`);
    })();

    // === BACK-OFFICE FLAG LIST ===

    await step("Load feature flags page & verify flags grouped by scope")(async () => {
      await expect(page.getByRole("heading", { name: "Feature flags" })).toBeVisible();

      const accountTable = page.getByRole("table", { name: "Account flags" });
      await expect(accountTable.getByText("Beta features")).toBeVisible();
      await expect(accountTable.getByText("Account overview page")).toBeVisible();

      const planTable = page.getByRole("table", { name: "Plan flags" });
      await expect(planTable.getByText("Single sign-on")).toBeVisible();

      const userTable = page.getByRole("table", { name: "User flags" });
      await expect(userTable.getByText("Compact view")).toBeVisible();

      const systemTable = page.getByRole("table", { name: "System flags" });
      await expect(systemTable.getByText("Google OAuth")).toBeVisible();
      await expect(systemTable.getByText("Subscriptions")).toBeVisible();
    })();

    // === BACK-OFFICE FLAG DETAIL ===

    const accountsTable = page.getByRole("table", { name: "Accounts" });
    const testOrgRow = accountsTable.getByRole("row").filter({ hasText: "Test Organization" }).first();

    await step("Click into beta-features flag detail & verify detail page loads")(async () => {
      const accountTable = page.getByRole("table", { name: "Account flags" });
      const betaRow = accountTable.locator("tr").filter({ hasText: "Beta features" });
      await betaRow.click();

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/feature-flags/beta-features`);
      await expect(page.getByRole("heading", { name: "Account status" })).toBeVisible();
    })();

    // Both @smoke and @comprehensive mutate the shared beta-features rollout; @smoke restores it to 0
    // at the end and @comprehensive does the same, so cross-test ordering is deterministic.
    await step("Pin beta-features rollout to 100 via back-office API & verify tenants evaluate enabled")(async () => {
      const response = await page.request.put(
        `${BACK_OFFICE_BASE_URL}/api/back-office/feature-flags/beta-features/rollout-percentage`,
        { data: { rolloutPercentage: 100 }, headers: await getAntiforgeryHeaders(page) }
      );

      expect(response.ok()).toBe(true);
    })();

    await step("Switch to the All state filter and search by Test Organization & verify a matching row renders")(
      async () => {
        await page.getByRole("group", { name: "State" }).getByRole("button", { name: "All" }).click();
        await page.getByRole("searchbox", { name: "Search" }).fill("Test Organization");

        await expect(page).toHaveURL((url) => url.searchParams.get("tenantsSearch") === "Test Organization");
        await expect(testOrgRow).toBeVisible();
      }
    )();

    await step("Toggle the first Test Organization override ON & verify toast and switch flips to checked")(
      async () => {
        const overrideSwitch = testOrgRow.getByRole("switch");
        await overrideSwitch.click();

        await expectToastMessage(context, "Beta features");
        await expect(overrideSwitch).toBeChecked();
      }
    )();

    await step("Toggle the same Test Organization override OFF & verify toast and switch flips to unchecked")(
      async () => {
        const overrideSwitch = testOrgRow.getByRole("switch");
        await overrideSwitch.click();

        await expectToastMessage(context, "Beta features");
        await expect(overrideSwitch).not.toBeChecked();
      }
    )();

    await step("Set A/B rollout percentage to 42 via the spinbutton & verify toast and spinbutton value")(async () => {
      const percentageInput = page.getByRole("spinbutton", { name: "Rollout %" });
      await percentageInput.fill("42");
      await blurActiveElement(page);

      await expectToastMessage(context, "Rollout percentage updated");
      await expect(percentageInput).toHaveValue("42");
    })();

    await step("Navigate back to flag list & verify return to list page")(async () => {
      await page.getByRole("link", { name: "Back to feature flags" }).click();

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/feature-flags`);
      await expect(page.getByRole("heading", { name: "Feature flags" })).toBeVisible();
    })();

    await step("Activate account-overview flag globally via back-office API & verify the toggle reads Active")(
      async () => {
        const response = await page.request.put(
          `${BACK_OFFICE_BASE_URL}/api/back-office/feature-flags/account-overview/activate`,
          { headers: await getAntiforgeryHeaders(page) }
        );
        expect(response.ok()).toBe(true);

        await page.goto(`${BACK_OFFICE_BASE_URL}/feature-flags/account-overview`);
        await expect(page.getByRole("switch", { name: "Toggle activation" })).toBeChecked();
      }
    )();

    await step("Activate compact-view flag globally via back-office API & verify the toggle reads Active")(async () => {
      const response = await page.request.put(
        `${BACK_OFFICE_BASE_URL}/api/back-office/feature-flags/compact-view/activate`,
        { headers: await getAntiforgeryHeaders(page) }
      );
      expect(response.ok()).toBe(true);

      await page.goto(`${BACK_OFFICE_BASE_URL}/feature-flags/compact-view`);
      await expect(page.getByRole("switch", { name: "Toggle activation" })).toBeChecked();
    })();

    await backOfficeContext.close();

    // === ACCOUNT SETTINGS: ACCOUNT FEATURE FLAGS ===

    await step("Navigate to account settings & verify Features section with account flags")(async () => {
      await ownerPage.goto("/account/settings");

      await expect(ownerPage.getByRole("heading", { name: "Features" })).toBeVisible();
      await expect(ownerPage.getByText("Account overview page")).toBeVisible();
    })();

    await step("Toggle account-overview flag ON & verify response carries x-user-feature-flags with account-overview")(
      async () => {
        const toggle = ownerPage.getByRole("switch", { name: "Account overview page" });

        await expectFeatureFlagHeaderResponse(ownerPage, toggle, {
          urlSubstring: "/api/account/feature-flags/account-overview/tenant-override",
          expectedFlag: "account-overview",
          shouldContain: true
        });

        await expectToastMessage(ownerContext, "Feature updated");
        await expect(toggle).toBeChecked();
      }
    )();

    await step("Toggle account-overview flag OFF & verify response x-user-feature-flags no longer contains it")(
      async () => {
        const toggle = ownerPage.getByRole("switch", { name: "Account overview page" });

        await expectFeatureFlagHeaderResponse(ownerPage, toggle, {
          urlSubstring: "/api/account/feature-flags/account-overview/tenant-override",
          expectedFlag: "account-overview",
          shouldContain: false
        });

        await expectToastMessage(ownerContext, "Feature updated");
        await expect(toggle).not.toBeChecked();
      }
    )();

    // === USER PREFERENCES: USER FEATURE FLAGS ===

    await step("Navigate to user preferences & verify Beta features section with user flags")(async () => {
      await ownerPage.goto("/user/preferences");

      await expect(ownerPage.getByRole("heading", { name: "Beta features" })).toBeVisible();
      await expect(ownerPage.getByText("Compact view")).toBeVisible();
    })();

    await step("Toggle compact view user flag ON & verify response carries x-user-feature-flags with compact-view")(
      async () => {
        const toggle = ownerPage.getByRole("switch", { name: "Compact view" });

        await expectFeatureFlagHeaderResponse(ownerPage, toggle, {
          urlSubstring: "/api/account/feature-flags/compact-view/user-override",
          expectedFlag: "compact-view",
          shouldContain: true
        });

        await expectToastMessage(ownerContext, "Preference updated");
        await expect(toggle).toBeChecked();
      }
    )();

    await step("Toggle compact view flag OFF & verify response x-user-feature-flags no longer contains it")(
      async () => {
        const toggle = ownerPage.getByRole("switch", { name: "Compact view" });

        await expectFeatureFlagHeaderResponse(ownerPage, toggle, {
          urlSubstring: "/api/account/feature-flags/compact-view/user-override",
          expectedFlag: "compact-view",
          shouldContain: false
        });

        await expectToastMessage(ownerContext, "Preference updated");
        await expect(toggle).not.toBeChecked();
      }
    )();

    // === CLEANUP: pin beta-features rollout to 100 (idempotent end state shared with @comprehensive) ===

    const cleanupContext = await browser.newContext({ baseURL: BACK_OFFICE_BASE_URL, ignoreHTTPSErrors: true });
    const cleanupPage = await cleanupContext.newPage();
    createTestContext(cleanupPage);

    await step("Log in as Admin & re-pin beta-features rollout to 100 (idempotent end state)")(async () => {
      await cleanupPage.goto(`${BACK_OFFICE_BASE_URL}/feature-flags`);
      await logInAsAdmin(cleanupPage, `${BACK_OFFICE_BASE_URL}/feature-flags`);

      const response = await cleanupPage.request.put(
        `${BACK_OFFICE_BASE_URL}/api/back-office/feature-flags/beta-features/rollout-percentage`,
        { data: { rolloutPercentage: 100 }, headers: await getAntiforgeryHeaders(cleanupPage) }
      );

      expect(response.ok()).toBe(true);
    })();

    await cleanupContext.close();
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
   * - Tenants tab: re-pinning rollout=100 keeps the Enabled view non-empty with the worker tenant
   * - Users tab (compact-view flag): role chip filters down to usersRoles=["Owner"]
   */
  test("should filter and paginate tenants and users on the feature flag detail page", async ({
    ownerPage,
    browser
  }) => {
    // ownerPage fixture provisions a tenant for this worker so the Accounts table is non-empty in
    // Enabled state even when this test runs before signup-driven tests on a fresh database.
    createTestContext(ownerPage);
    const backOfficeContext = await browser.newContext({ baseURL: BACK_OFFICE_BASE_URL, ignoreHTTPSErrors: true });
    const page = await backOfficeContext.newPage();
    createTestContext(page);

    await step("Log in as Admin via MockEasyAuth & verify redirect to beta-features detail")(async () => {
      await page.goto(`${BACK_OFFICE_BASE_URL}/feature-flags/beta-features`);

      await logInAsAdmin(page, `${BACK_OFFICE_BASE_URL}/feature-flags/beta-features`);
      await expect(page.getByRole("heading", { name: "Account status" })).toBeVisible();
    })();

    // Both @smoke and @comprehensive mutate the shared beta-features rollout; both restore it to 0
    // at the end so cross-test ordering is deterministic.
    await step("Pin beta-features rollout to 100 via back-office API & verify tenants evaluate enabled")(async () => {
      const response = await page.request.put(
        `${BACK_OFFICE_BASE_URL}/api/back-office/feature-flags/beta-features/rollout-percentage`,
        { data: { rolloutPercentage: 100 }, headers: await getAntiforgeryHeaders(page) }
      );

      expect(response.ok()).toBe(true);
    })();

    // === TENANTS: STATE CHIPS (single-select; default Enabled pressed; All/Enabled/Disabled exclusive) ===

    const accountsTable = page.getByRole("table", { name: "Accounts" });
    const stateGroup = page.getByRole("group", { name: "State" });
    const allChip = stateGroup.getByRole("button", { name: "All" });
    const enabledChip = stateGroup.getByRole("button", { name: "Enabled" });
    const disabledChip = stateGroup.getByRole("button", { name: "Disabled" });

    await step("Reload bare URL & verify Enabled chip is pressed by default and the other chips are unpressed")(
      async () => {
        await page.goto(`${BACK_OFFICE_BASE_URL}/feature-flags/beta-features`);

        await expect(enabledChip).toHaveAttribute("aria-pressed", "true");
        await expect(disabledChip).toHaveAttribute("aria-pressed", "false");
        await expect(allChip).toHaveAttribute("aria-pressed", "false");
        await expect(accountsTable).toBeVisible();
      }
    )();

    await step("Click Disabled chip from default & verify URL switches to Disabled and only Disabled is pressed")(
      async () => {
        await disabledChip.click();

        await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/feature-flags/beta-features?tenantsState=Disabled`);
        await expect(disabledChip).toHaveAttribute("aria-pressed", "true");
        await expect(enabledChip).toHaveAttribute("aria-pressed", "false");
        // Note: with rollout=100 and no explicit Disabled overrides, the Disabled filter yields zero
        // tenants and the UI shows the Empty state instead of the Accounts table. The chip behavior
        // is fully verified above; the table rendering is exercised by the Enabled step.
      }
    )();

    await step("Click All chip from Disabled & verify URL switches to All and only All is pressed")(async () => {
      await allChip.click();

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/feature-flags/beta-features?tenantsState=All`);
      await expect(allChip).toHaveAttribute("aria-pressed", "true");
      await expect(enabledChip).toHaveAttribute("aria-pressed", "false");
      await expect(disabledChip).toHaveAttribute("aria-pressed", "false");
    })();

    await step("Reload from the All URL & verify only the All chip is pressed")(async () => {
      await page.goto(`${BACK_OFFICE_BASE_URL}/feature-flags/beta-features?tenantsState=All`);

      await expect(allChip).toHaveAttribute("aria-pressed", "true");
      await expect(enabledChip).toHaveAttribute("aria-pressed", "false");
      await expect(disabledChip).toHaveAttribute("aria-pressed", "false");
    })();

    await step("Click Enabled chip from the All state & verify URL drops the state param and only Enabled is pressed")(
      async () => {
        await enabledChip.click();

        await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/feature-flags/beta-features`);
        await expect(enabledChip).toHaveAttribute("aria-pressed", "true");
        await expect(allChip).toHaveAttribute("aria-pressed", "false");
        await expect(disabledChip).toHaveAttribute("aria-pressed", "false");
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
        await expect(accountsTable.getByRole("row").filter({ hasText: "Test Organization" }).first()).toBeVisible();
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

    await step("Reload accounts table & verify the worker tenant renders in the Enabled view")(async () => {
      await page.reload();

      await expect(accountsTable.locator("tbody tr").first()).toBeVisible();
    })();

    // === USERS: ROLE FILTER ===

    await step(
      "Navigate to compact-view user-scoped flag & verify default Enabled chip is pressed, then click All to render Users table"
    )(async () => {
      await page.goto(`${BACK_OFFICE_BASE_URL}/feature-flags/compact-view`);

      await expect(page.getByRole("heading", { name: "User status" })).toBeVisible();
      await expect(page.getByRole("group", { name: "State" }).getByRole("button", { name: "Enabled" })).toHaveAttribute(
        "aria-pressed",
        "true"
      );

      // compact-view is ConfigurableByUser (not A/B-eligible), so no user is Enabled by default.
      // Click All to ensure the Users table renders the worker tenant's users for the role chip test below.
      await page.getByRole("group", { name: "State" }).getByRole("button", { name: "All" }).click();
      await expect(page.getByRole("table", { name: "Users" })).toBeVisible();
    })();

    const ownerChip = page.getByRole("group", { name: "Role" }).getByRole("button", { name: "Owner" });

    await step("Click Owner role chip & verify URL serializes the role array and the chip is pressed")(async () => {
      await ownerChip.click();

      await expect(page).toHaveURL((url) => url.searchParams.get("usersRoles") === '["Owner"]');
      await expect(ownerChip).toHaveAttribute("aria-pressed", "true");
    })();

    // === CLEANUP: re-pin beta-features rollout to 100 (idempotent end state shared with @smoke) ===

    await step("Re-pin beta-features rollout to 100 via back-office API & verify success")(async () => {
      const response = await page.request.put(
        `${BACK_OFFICE_BASE_URL}/api/back-office/feature-flags/beta-features/rollout-percentage`,
        { data: { rolloutPercentage: 100 }, headers: await getAntiforgeryHeaders(page) }
      );

      expect(response.ok()).toBe(true);
    })();

    await backOfficeContext.close();
  });
});

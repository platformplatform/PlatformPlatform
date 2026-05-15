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
// serialize across projects. Instead, both tests target the same idempotent end state (active +
// rollout=100 + no override on the worker tenant): each test activates the flag and pins rollout to
// 100 at the start, removes any leftover Test Organization override before driving the toggle steps,
// and at cleanup re-pins rollout to 100 and removes the override rather than resetting to 0.
// Concurrent runs converge to the same activate/rollout end state; the override-toggle steps must
// remain reload-free between clicks so a concurrent cleanup can't DELETE an override mid-sequence.

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

// Activate the flag and pin rollout to 100 — both are idempotent and the test relies on tenants
// evaluating Enabled. The flag may have been deactivated by a prior @comprehensive run (or human),
// in which case rollout alone produces an empty Accounts table.
async function activateAndPinRollout(page: Page, flagKey: string, rolloutPercentage: number): Promise<void> {
  const headers = await getAntiforgeryHeaders(page);
  const activateResponse = await page.request.put(
    `${BACK_OFFICE_BASE_URL}/api/back-office/feature-flags/${flagKey}/activate`,
    { headers }
  );
  expect(activateResponse.ok()).toBe(true);

  const rolloutResponse = await page.request.put(
    `${BACK_OFFICE_BASE_URL}/api/back-office/feature-flags/${flagKey}/rollout-percentage`,
    { data: { rolloutPercentage }, headers }
  );
  expect(rolloutResponse.ok()).toBe(true);
}

// Remove every tenant-override on `flagKey` across the entire database. The override toggles in
// @smoke pick the first "Test Organization" row, but the back-office tables interleave every
// worker's tenant (and stale rows from old runs), so the .first() row is unpredictable unless we
// scrub overrides upfront. Doing this once at the start (and once at the end) keeps the cross-test
// state symmetric without leaking implementation details (the cleanup endpoint is internal).
async function removeAllTenantOverrides(page: Page, flagKey: string): Promise<void> {
  const headers = await getAntiforgeryHeaders(page);
  // Always read page 0: every iteration deletes the current page, shifting the remaining records
  // forward into the first page on the next query. Incrementing pageOffset against a shrinking
  // result set would skip rows (e.g., with 250 overrides and pageSize=100, OFFSET=100 on the
  // remaining 150 returns rows 200-249 of the original, leaving rows 100-199 untouched).
  const pageSize = 100;
  while (true) {
    const response = await page.request.get(
      `${BACK_OFFICE_BASE_URL}/api/back-office/feature-flags/${flagKey}/tenants?HasOverride=true&PageSize=${pageSize}&PageOffset=0`
    );
    expect(response.ok()).toBe(true);
    const body = await response.json();
    const tenants = body.tenants as { id: string }[];
    if (tenants.length === 0) return;

    for (const tenant of tenants) {
      const deleteResponse = await page.request.delete(
        `${BACK_OFFICE_BASE_URL}/api/back-office/feature-flags/${flagKey}/tenant-override?tenantId=${tenant.id}`,
        { headers }
      );
      // 200 = override existed and was removed; 404 = override raced away (e.g., concurrent worker).
      expect([200, 404]).toContain(deleteResponse.status());
    }
    if (tenants.length < pageSize) return;
  }
}

test.describe("@smoke", () => {
  // Holds the already-logged-in back-office page from the test body so the afterEach can reuse it
  // for the cleanup PUTs (one MockEasyAuth roundtrip instead of two). Falls back to spinning up a
  // fresh context if the test failed before the back-office login completed.
  let backOfficePageForCleanup: Page | null = null;

  // Restore the idempotent end state (beta-features active, rollout=100, no Test Organization
  // override) after every test, including failures. Without this hook a mid-test failure between
  // the start-of-test scrub and the end-of-test scrub would leak the override into subsequent runs.
  test.afterEach(async ({ browser }) => {
    if (backOfficePageForCleanup) {
      await removeAllTenantOverrides(backOfficePageForCleanup, "beta-features");
      await activateAndPinRollout(backOfficePageForCleanup, "beta-features", 100);
      await backOfficePageForCleanup.context().close();
      backOfficePageForCleanup = null;
      return;
    }

    const cleanupContext = await browser.newContext({ baseURL: BACK_OFFICE_BASE_URL, ignoreHTTPSErrors: true });
    const cleanupPage = await cleanupContext.newPage();

    await cleanupPage.goto(`${BACK_OFFICE_BASE_URL}/feature-flags`);
    await logInAsAdmin(cleanupPage, `${BACK_OFFICE_BASE_URL}/feature-flags`);
    await removeAllTenantOverrides(cleanupPage, "beta-features");
    await activateAndPinRollout(cleanupPage, "beta-features", 100);

    await cleanupContext.close();
  });

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

    // Hand the logged-in back-office page to afterEach so the cleanup PUTs reuse this session
    // rather than spinning up a third browser context.
    backOfficePageForCleanup = page;

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

    // Both @smoke and @comprehensive mutate the shared beta-features rollout; both leave it at 100
    // and remove any tenant-override they created, so cross-test ordering is deterministic.
    await step("Activate beta-features and pin rollout to 100 via back-office API & verify tenants evaluate enabled")(
      async () => {
        await activateAndPinRollout(page, "beta-features", 100);
      }
    )();

    await step("Remove all leftover tenant overrides for beta-features & verify clean precondition")(async () => {
      await removeAllTenantOverrides(page, "beta-features");
      // The detail page caches tenant override state in TanStack Query; the API DELETEs above
      // don't invalidate that cache. Reload so the next steps see post-cleanup data.
      await page.reload();
      await expect(page.getByRole("heading", { name: "Account status" })).toBeVisible();
    })();

    await step("Switch to the All state filter and search by Test Organization & verify a matching row renders")(
      async () => {
        await page.getByRole("group", { name: "State" }).getByRole("button", { name: "All" }).click();
        await page.getByRole("searchbox", { name: "Search" }).fill("Test Organization");

        await expect(page).toHaveURL((url) => url.searchParams.get("tenantsSearch") === "Test Organization");
        await expect(testOrgRow).toBeVisible();
      }
    )();

    // Tenant override toggle starts ON because rollout=100; first click writes a disable-override,
    // second flips to enable-override. Cleanup deletes the override so the next run starts from the
    // same precondition.
    await step(
      "Click the Test Organization switch from default-enabled to disabled & verify disable toast and switch off"
    )(async () => {
      const overrideSwitch = testOrgRow.getByRole("switch");
      await overrideSwitch.click();

      await expectToastMessage(context, "Beta features");
      await expect(overrideSwitch).not.toBeChecked();
    })();

    await step(
      "Click the Test Organization switch back from disabled override to enabled override & verify enable toast and switch on"
    )(async () => {
      const overrideSwitch = testOrgRow.getByRole("switch");
      await overrideSwitch.click();

      await expectToastMessage(context, "Beta features");
      await expect(overrideSwitch).toBeChecked();
    })();

    await step("Remove all tenant overrides for beta-features via back-office API & verify clean end state")(
      async () => {
        await removeAllTenantOverrides(page, "beta-features");
      }
    )();

    await step("Set A/B rollout percentage to 42 via the rollout input & verify toast and input value")(async () => {
      const percentageInput = page.getByLabel("Rollout %");
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

    // Globally activating non-kill-switch flags (account-overview, compact-view) was previously
    // tested here, but the ActivateFeatureFlagValidator now rejects flags whose definition has
    // `IsKillSwitchEnabled: false` with a 400. The activation surface for those flags is the
    // owner-scoped tenant-override and the user-scoped user-override toggles exercised below.
    // The back-office context stays open so afterEach reuses its logged-in admin session.

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

    await step("Navigate to /account with account-overview ON & verify the dashboard heading renders")(async () => {
      await ownerPage.goto("/account");

      await expect(ownerPage).toHaveURL("/account");
      await expect(ownerPage.getByRole("heading", { name: "Account overview" })).toBeVisible();
    })();

    await step("Return to account settings & verify Features section ready for next toggle")(async () => {
      await ownerPage.goto("/account/settings");

      await expect(ownerPage.getByRole("heading", { name: "Features" })).toBeVisible();
    })();

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

    await step("Navigate to /account with account-overview OFF & verify redirect to /account/users")(async () => {
      await ownerPage.goto("/account");

      await expect(ownerPage).toHaveURL("/account/users");
      await expect(ownerPage.getByRole("heading", { name: "Users" })).toBeVisible();
    })();

    // === USER PREFERENCES: USER FEATURE FLAGS ===

    await step("Navigate to user preferences & verify Feature preferences section with user flags")(async () => {
      await ownerPage.goto("/user/preferences");

      // The section was renamed from "Beta features" to "Feature preferences" when the toggle list
      // grew to surface every user-configurable feature flag, not just the Beta features flag.
      await expect(ownerPage.getByRole("heading", { name: "Feature preferences" })).toBeVisible();
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

    // Cleanup (remove overrides + re-activate with rollout=100) runs in test.afterEach so it
    // executes on both success and failure paths.
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

    // Both @smoke and @comprehensive mutate the shared beta-features rollout; both leave it active
    // with rollout=100 so cross-test ordering is deterministic.
    await step("Activate beta-features and pin rollout to 100 via back-office API & verify tenants evaluate enabled")(
      async () => {
        await activateAndPinRollout(page, "beta-features", 100);
      }
    )();

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

    // === CLEANUP: remove any tenant overrides + re-activate + pin beta-features rollout to 100 ===
    // (idempotent end state shared with @smoke — enforced inside this step, not by earlier step ordering)

    await step(
      "Remove all beta-features tenant overrides and re-pin rollout to 100 via back-office API & verify idempotent end state"
    )(async () => {
      await removeAllTenantOverrides(page, "beta-features");
      await activateAndPinRollout(page, "beta-features", 100);
    })();

    await backOfficeContext.close();
  });
});

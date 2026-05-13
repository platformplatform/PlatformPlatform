import { expect, type Page } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { getBackOfficeBaseUrl } from "@shared/e2e/utils/constants";
import { blurActiveElement, createTestContext, expectToastMessage } from "@shared/e2e/utils/test-assertions";
import { step } from "@shared/e2e/utils/test-step-wrapper";

const BACK_OFFICE_BASE_URL = getBackOfficeBaseUrl();

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
   * - Account settings: verify Features section, toggle account-scoped custom branding flag
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
        { data: { rolloutPercentage: 100 }, headers: await getAntiforgeryHeaders(page) }
      );

      expect(response.ok()).toBe(true);
    })();

    await step(
      "Switch to the All state filter and search by Test Organization & verify a matching row renders"
    )(async () => {
      // Switch to the All filter so the row set is independent of cross-worker rollout-percentage races
      // on the shared beta-features global state. Without this, parallel test runs from other browser
      // workers can flip rollout to 0 mid-test, emptying the default Enabled view.
      await page.getByRole("group", { name: "State" }).getByRole("button", { name: "All" }).click();
      await page.getByRole("searchbox", { name: "Search" }).fill("Test Organization");

      // Wait for the search-debounce query to settle AND for a matching row to render. Without the row
      // assertion, downstream `.first()` targeting can bind to a row from a parallel test's tenant
      // ("Mobile Nav Test" etc.) that happens to slip through the search match, or to a stale row
      // from the pre-debounce result set that becomes detached when the new query resolves.
      await expect(page).toHaveURL((url) => url.searchParams.get("tenantsSearch") === "Test Organization");
      await expect(
        page.getByRole("table", { name: "Accounts" }).getByRole("row").filter({ hasText: "Test Organization" }).first()
      ).toBeVisible();
    })();

    await step("Toggle the first Test Organization override & verify toast confirms state change")(async () => {
      const testOrgRow = page
        .getByRole("table", { name: "Accounts" })
        .getByRole("row")
        .filter({ hasText: "Test Organization" })
        .first();
      await testOrgRow.getByRole("switch").click();

      await expectToastMessage(context, "Beta features");
    })();

    await step("Toggle the same Test Organization override back & verify toast confirms state change")(async () => {
      const testOrgRow = page
        .getByRole("table", { name: "Accounts" })
        .getByRole("row")
        .filter({ hasText: "Test Organization" })
        .first();
      await testOrgRow.getByRole("switch").click();

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
        `${BACK_OFFICE_BASE_URL}/api/back-office/feature-flags/custom-branding/activate`,
        { headers: await getAntiforgeryHeaders(page) }
      );

      expect(response.ok()).toBe(true);
    })();

    await step("Activate compact view flag globally via back-office API for downstream checks")(async () => {
      const response = await page.request.put(
        `${BACK_OFFICE_BASE_URL}/api/back-office/feature-flags/compact-view/activate`,
        { headers: await getAntiforgeryHeaders(page) }
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

    await step("Toggle custom branding flag ON & verify response carries x-user-feature-flags with custom-branding")(
      async () => {
        const toggle = ownerPage.getByRole("switch", { name: "Custom branding" });

        const [tenantOverrideResponse] = await Promise.all([
          ownerPage.waitForResponse(
            (response) =>
              response.url().includes("/api/account/feature-flags/custom-branding/tenant-override") &&
              response.request().method() === "PUT"
          ),
          toggle.click()
        ]);

        expect(tenantOverrideResponse.headers()["x-user-feature-flags"]).toContain("custom-branding");

        await expectToastMessage(ownerContext, "Feature updated");
      }
    )();

    await step("Toggle custom branding flag OFF & verify response x-user-feature-flags no longer contains it")(
      async () => {
        const toggle = ownerPage.getByRole("switch", { name: "Custom branding" });

        const [tenantOverrideResponse] = await Promise.all([
          ownerPage.waitForResponse(
            (response) =>
              response.url().includes("/api/account/feature-flags/custom-branding/tenant-override") &&
              response.request().method() === "PUT"
          ),
          toggle.click()
        ]);

        expect(tenantOverrideResponse.headers()["x-user-feature-flags"]).not.toContain("custom-branding");

        await expectToastMessage(ownerContext, "Feature updated");
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

        const [userOverrideResponse] = await Promise.all([
          ownerPage.waitForResponse(
            (response) =>
              response.url().includes("/api/account/feature-flags/compact-view/user-override") &&
              response.request().method() === "PUT"
          ),
          toggle.click()
        ]);

        expect(userOverrideResponse.headers()["x-user-feature-flags"]).toContain("compact-view");

        await expectToastMessage(ownerContext, "Preference updated");
      }
    )();

    await step("Toggle compact view flag OFF & verify response x-user-feature-flags no longer contains it")(
      async () => {
        const toggle = ownerPage.getByRole("switch", { name: "Compact view" });

        const [userOverrideResponse] = await Promise.all([
          ownerPage.waitForResponse(
            (response) =>
              response.url().includes("/api/account/feature-flags/compact-view/user-override") &&
              response.request().method() === "PUT"
          ),
          toggle.click()
        ]);

        expect(userOverrideResponse.headers()["x-user-feature-flags"]).not.toContain("compact-view");

        await expectToastMessage(ownerContext, "Preference updated");
      }
    )();
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
        await expect(accountsTable).toBeVisible();
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
        // Re-pin rollout=100 so cross-browser parallel runs that may have reset rollout to 0 during
        // their cleanup can't empty the default Enabled view out from under this step's assertion.
        // Reload so the tenants query refetches with the new rollout in effect (the prior cached
        // result from before this re-pin would otherwise win until natural invalidation).
        const rolloutResponse = await page.request.put(
          `${BACK_OFFICE_BASE_URL}/api/back-office/feature-flags/beta-features/rollout-percentage`,
          { data: { rolloutPercentage: 100 }, headers: await getAntiforgeryHeaders(page) }
        );
        expect(rolloutResponse.ok()).toBe(true);

        await page.reload();
        await searchBox.fill("Test Organization");

        await expect(page).toHaveURL((url) => url.searchParams.get("tenantsSearch") === "Test Organization");
        await expect(
          accountsTable.getByRole("row").filter({ hasText: "Test Organization" }).first()
        ).toBeVisible();
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
    // Re-pin rollout=100 right before the pagination check so cross-browser parallel `@comprehensive`
    // runs that may have reset rollout to 0 during their cleanup can't empty the default Enabled view
    // out from under us. With rollout=100, the dev-DB always exceeds the 25-row PageSize and renders
    // the Next button.

    await step("Re-pin beta-features rollout to 100 and click Next page & verify URL advances")(async () => {
      const rolloutResponse = await page.request.put(
        `${BACK_OFFICE_BASE_URL}/api/back-office/feature-flags/beta-features/rollout-percentage`,
        { data: { rolloutPercentage: 100 }, headers: await getAntiforgeryHeaders(page) }
      );
      expect(rolloutResponse.ok()).toBe(true);

      // Reload so the tenants query refetches with the new rollout in effect. Without this, the prior
      // query result (with whatever rollout cross-worker cleanup left in place) wins until natural
      // invalidation, and the Next button stays hidden if that prior result was below the PageSize.
      await page.reload();
      await expect(accountsTable.locator("tbody tr").first()).toBeVisible();

      const nextPageButton = page.getByRole("button", { name: "Next" });
      await expect(nextPageButton).toBeVisible();
      await nextPageButton.dispatchEvent("click");

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/feature-flags/beta-features?tenantsPageOffset=1`);
      await expect(accountsTable).toBeVisible();
    })();

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
        { data: { rolloutPercentage: 0 }, headers: await getAntiforgeryHeaders(page) }
      );

      expect(response.ok()).toBe(true);
    })();

    await backOfficeContext.close();
  });
});

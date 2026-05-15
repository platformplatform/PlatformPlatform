import { expect, request } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { getBackOfficeBaseUrl, getBaseUrl } from "@shared/e2e/utils/constants";
import { createTestContext } from "@shared/e2e/utils/test-assertions";
import { logInAsAdmin } from "@shared/e2e/utils/test-data";
import { step } from "@shared/e2e/utils/test-step-wrapper";

const BACK_OFFICE_BASE_URL = getBackOfficeBaseUrl();
const BASE_URL = getBaseUrl();

test.describe("@smoke", () => {
  /**
   * Canonical back-office golden path: log in as Admin via MockEasyAuth, verify the dashboard renders with
   * KPI tiles and trend cards, navigate to the accounts list, open an account, and verify the tenant detail
   * page loads with the MRR KPI and Owners section visible. Touches the auth boundary, the dashboard route,
   * the accounts route, and the tenant detail route in one journey so any regression to the back-office
   * shell trips this test on every deployment.
   */
  test("should log in to back-office, render dashboard, and load tenant detail", async ({ ownerPage, browser }) => {
    // ownerPage fixture provisions a tenant for this worker so the back-office Accounts list is non-empty
    // even when this test runs before signup-driven tests on a fresh database.
    createTestContext(ownerPage);
    const backOfficeContext = await browser.newContext({ baseURL: BACK_OFFICE_BASE_URL, ignoreHTTPSErrors: true });
    const page = await backOfficeContext.newPage();
    createTestContext(page);

    await step("Log in as Admin via MockEasyAuth & verify redirect to back-office dashboard")(async () => {
      await page.goto(`${BACK_OFFICE_BASE_URL}/`);

      await logInAsAdmin(page, `${BACK_OFFICE_BASE_URL}/`);
      await expect(page.getByRole("heading", { name: "Dashboard" })).toBeVisible();
    })();

    await step("Verify dashboard renders KPI tiles and chart cards on the default 30d period")(async () => {
      await expect(page.getByText("Total accounts")).toBeVisible();
      await expect(page.getByText("Blended MRR")).toBeVisible();
      await expect(page.getByText("Users active")).toBeVisible();
      await expect(page.getByText("Active sessions")).toBeVisible();

      await expect(page.getByText("MRR trend", { exact: true })).toBeVisible();
      await expect(page.getByText("Plan distribution", { exact: true })).toBeVisible();
    })();

    await step("Navigate to accounts list & verify table renders with a row")(async () => {
      await page.goto(`${BACK_OFFICE_BASE_URL}/accounts`);

      await expect(page.getByRole("table", { name: "Accounts" })).toBeVisible();
      await expect(page.getByRole("columnheader", { name: "Name" })).toBeVisible();
      await expect(page.getByRole("row").nth(1)).toBeVisible();
    })();

    await step("Open first account & verify tenant detail page loads with MRR card and Owners heading")(async () => {
      await page.getByRole("row").nth(1).click();
      await page.getByRole("button", { name: "Open account" }).click();

      const main = page.getByRole("main");
      await expect(main.getByText("MRR")).toBeVisible();
      await expect(main.getByText("Lifetime value")).toBeVisible();
      await expect(main.getByRole("heading", { name: "Owners" })).toBeVisible();
    })();

    await backOfficeContext.close();
  });
});

test.describe("@comprehensive", () => {
  /**
   * Broader back-office surface coverage: drift banner appears when the summary endpoint reports drift,
   * the reconcile-with-Stripe admin action is wired on the account detail header, navigation across the
   * Overview/Users tabs swaps tab panels, the accounts list search filter and column-sort toggle update
   * the URL, and host-scoped auth boundaries reject cross-host requests (security spot-check folded in
   * to keep the file at the one-smoke-plus-one-comprehensive convention).
   */
  test("should surface drift banner, expose reconcile action, navigate tabs, filter and sort accounts, and reject cross-host requests", async ({
    ownerPage,
    browser
  }) => {
    createTestContext(ownerPage);

    const backOfficeContext = await browser.newContext({ baseURL: BACK_OFFICE_BASE_URL, ignoreHTTPSErrors: true });
    const page = await backOfficeContext.newPage();
    createTestContext(page);

    // Stub the drift summary so the banner renders even when no real subscriptions have drift detected.
    await page.route("**/api/back-office/billing-drift/summary", async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        json: { subscriptionsWithDriftCount: 3 }
      });
    });

    await step("Log in as Admin via MockEasyAuth & verify dashboard loads")(async () => {
      await page.goto(`${BACK_OFFICE_BASE_URL}/`);

      await logInAsAdmin(page, `${BACK_OFFICE_BASE_URL}/`);
    })();

    // === DRIFT BANNER ===

    await step("Verify drift banner renders with View accounts CTA when summary reports drift")(async () => {
      const driftAlert = page.getByRole("alert");
      await expect(driftAlert).toBeVisible();
      await expect(driftAlert.getByText("3 accounts have billing drift detected.")).toBeVisible();
      await expect(driftAlert.getByRole("button", { name: "View accounts" })).toBeVisible();
    })();

    // === ACCOUNTS LIST FILTER AND SORT ===

    await step("Navigate to accounts list & verify table renders with Name column header")(async () => {
      await page.goto(`${BACK_OFFICE_BASE_URL}/accounts`);

      await expect(page.getByRole("table", { name: "Accounts" })).toBeVisible();
      await expect(page.getByRole("columnheader", { name: "Name" })).toBeVisible();
      await expect(page.getByRole("row").nth(1)).toBeVisible();
    })();

    await step("Type a non-matching search query & verify URL filters to the search and empty state appears")(
      async () => {
        await page.getByRole("searchbox", { name: "Search" }).fill("zzz-no-match-account-xyz");

        await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/accounts?search=zzz-no-match-account-xyz`);
      }
    )();

    await step("Clear search & verify URL returns to base /accounts")(async () => {
      await page.getByRole("searchbox", { name: "Search" }).fill("");

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/accounts`);
    })();

    await step("Click Name column header & verify sort URL params populate")(async () => {
      await page.getByRole("columnheader", { name: "Name" }).click();

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/accounts?orderBy=Name`);
    })();

    // === TENANT DETAIL: TAB NAVIGATION AND RECONCILE ACTION ===

    await step("Open first account detail & verify Overview tab and account actions menu are visible")(async () => {
      await page.goto(`${BACK_OFFICE_BASE_URL}/accounts`);
      await page.getByRole("row").nth(1).click();
      await page.getByRole("button", { name: "Open account" }).click();

      const main = page.getByRole("main");
      await expect(main.getByRole("tab", { name: "Overview" })).toBeVisible();
      await expect(main.getByRole("tab", { name: "Users" })).toBeVisible();
      await expect(main.getByRole("button", { name: "Account actions" })).toBeVisible();
    })();

    await step("Switch to Users tab & verify user list renders")(async () => {
      const main = page.getByRole("main");
      await main.getByRole("tab", { name: "Users" }).click();

      const usersPanel = main.getByRole("tabpanel", { name: "Users" });
      await expect(usersPanel).toBeVisible();
      await expect(usersPanel.getByRole("row").nth(1)).toBeVisible();
    })();

    await step("Open account actions menu & verify Reconcile with Stripe action is present")(async () => {
      await page.getByRole("button", { name: "Account actions" }).click();
      const menu = page.getByRole("menu");
      await expect(menu).toBeVisible();
      await expect(menu.getByRole("menuitem", { name: "Reconcile with Stripe" })).toBeVisible();

      await page.keyboard.press("Escape");
    })();

    // === SECURITY: HOST-SCOPED AUTH BOUNDARY ===

    await step("GET /api/back-office/me on user-facing host with account session & verify 404 from RequireHost")(
      async () => {
        const accountStorageState = await ownerPage.context().storageState();
        const accountAuthenticatedContext = await request.newContext({
          storageState: accountStorageState,
          ignoreHTTPSErrors: true
        });
        const response = await accountAuthenticatedContext.get(`${BASE_URL}/api/back-office/me`, {
          headers: { Accept: "application/json" },
          maxRedirects: 0
        });

        expect(response.status()).toBe(404);
        await accountAuthenticatedContext.dispose();
      }
    )();

    await backOfficeContext.close();
  });
});

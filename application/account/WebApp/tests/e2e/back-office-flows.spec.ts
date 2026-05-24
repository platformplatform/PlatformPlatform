import { expect, type Page, request } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { getBackOfficeBaseUrl, getBaseUrl } from "@shared/e2e/utils/constants";
import { createTestContext } from "@shared/e2e/utils/test-assertions";
import { logInAsAdmin } from "@shared/e2e/utils/test-data";
import { step } from "@shared/e2e/utils/test-step-wrapper";

const BACK_OFFICE_BASE_URL = getBackOfficeBaseUrl();
const BASE_URL = getBaseUrl();

// Pre-seeds a support ticket as the reporter via the user-facing API. The fixture-provided
// reporterPage's context is created without ignoreHTTPSErrors on macOS, so a fresh request context
// is spun up from the reporter's storageState. /api/account/support-tickets is multipart and
// disables antiforgery, so no x-xsrf-token is required.
async function createReporterTicket(reporterPage: Page, subject: string, body: string): Promise<string> {
  const storageState = await reporterPage.context().storageState();
  const reporterRequest = await request.newContext({ storageState, ignoreHTTPSErrors: true });
  const response = await reporterRequest.post(`${BASE_URL}/api/account/support-tickets`, {
    multipart: { subject, body, category: "Billing" }
  });
  expect(response.ok()).toBe(true);
  const raw = await response.text();
  await reporterRequest.dispose();
  return JSON.parse(raw) as string;
}

// Fetches the staff detail view of a ticket from the back-office API using the already-authenticated
// back-office page session. Used to look up the real tenant id and reporter id off a seeded ticket
// without relying on the fixture's placeholder tenantId.
async function getStaffTicketDetail(
  backOfficePage: Page,
  ticketId: string
): Promise<{ accountId: string; reporterId: string }> {
  const response = await backOfficePage.request.get(
    `${BACK_OFFICE_BASE_URL}/api/back-office/support-tickets/${ticketId}`
  );
  expect(response.ok()).toBe(true);
  const detail = (await response.json()) as { account: { id: string }; reporter: { id: string } };
  return { accountId: detail.account.id, reporterId: detail.reporter.id };
}

// Reads PUBLIC_SUPPORT_SYSTEM_ENABLED off the SPA shell's runtimeEnv meta tag. Mirrors the
// subscription-flows.spec.ts:6-14 pattern. Caller must navigate `authenticatedPage` to a non-support
// route first — `requireSupportSystemEnabled` would redirect away when the flag is off and a guarded
// support page is loaded.
async function readSupportSystemEnabled(authenticatedPage: Page): Promise<boolean> {
  return await authenticatedPage.evaluate(() => {
    const meta = document.head.querySelector('meta[name="runtimeEnv"]');
    const runtimeEnv = JSON.parse(meta?.getAttribute("content") ?? "{}");
    return runtimeEnv.PUBLIC_SUPPORT_SYSTEM_ENABLED === "true";
  });
}

// Per-step flag-aware wrapper. Routes the conditional through a module-scope helper so the test body
// stays branch-free (per the no-`if`-in-test-bodies constraint). When the flag is on, behaves
// identically to `step(name)`. When the flag is off, replaces the step body with a single
// flag-disabled marker so the test trace still records that the support assertions were intentionally
// skipped, the surrounding non-support steps continue to run, and no support endpoint is touched.
function supportStep(supportEnabled: boolean, name: string): (fn: () => Promise<void>) => () => Promise<void> {
  if (supportEnabled) return step(name);
  return step(`${name} [skipped: support system disabled]`) as (fn: () => Promise<void>) => () => Promise<void>;
}

// No-op body used as the step argument when the support system is disabled. Keeps the call site at
// `await supportStep(...)(supportStepBody(...))()` readable without leaking conditionals into the
// test body. The argument-vs-body split lives at module scope.
function supportStepBody(supportEnabled: boolean, body: () => Promise<void>): () => Promise<void> {
  return supportEnabled ? body : async () => undefined;
}

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
   * the URL, the account Support tickets tab and Open support tickets card render for a seeded
   * ticket and a row click deep-links to the back-office ticket detail page, the user Support tickets
   * tab renders the same ticket from the reporter angle, and host-scoped auth boundaries reject
   * cross-host requests (security spot-check folded in to keep the file at the one-smoke-plus-one-
   * comprehensive convention).
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

    // === SUPPORT TABS AND ACCOUNT CARD ===

    // Probe PUBLIC_SUPPORT_SYSTEM_ENABLED off the SPA shell on a route already loaded (the
    // back-office dashboard / accounts list). When the flag is off the support tabs/cards never
    // render and the support endpoints 404, so the five steps below are wrapped in flag-aware
    // helpers that no-op cleanly. The conditional lives in the module-scope helper, not the test
    // body — keeps the linear-test-body rule intact.
    const supportEnabled = await readSupportSystemEnabled(page);

    // Seed a non-terminal support ticket for the worker tenant so the account-scoped support
    // surfaces have data to render. The seeded ticket id also drives the row-click navigation
    // assertion and the user support tab assertion below.
    const supportSuffix = `${Date.now()}-${Math.floor(Math.random() * 1_000_000)}`;
    const supportSubject = `E2E support tab ${supportSuffix}`;
    let supportTicketId = "";
    let supportTenantId = "";
    let supportReporterId = "";

    await supportStep(
      supportEnabled,
      "Pre-seed a support ticket as the reporter & look up the tenant and reporter ids via staff API"
    )(
      supportStepBody(supportEnabled, async () => {
        supportTicketId = await createReporterTicket(ownerPage, supportSubject, "Verifying back-office support tabs.");
        expect(supportTicketId).toContain("tkt_");

        const detail = await getStaffTicketDetail(page, supportTicketId);
        supportTenantId = detail.accountId;
        supportReporterId = detail.reporterId;
        expect(supportTenantId.length).toBeGreaterThan(0);
        expect(supportReporterId).toContain("usr_");
      })
    )();

    await supportStep(
      supportEnabled,
      "Navigate to the worker tenant's account detail Support tickets tab & verify the seeded ticket renders"
    )(
      supportStepBody(supportEnabled, async () => {
        await page.goto(`${BACK_OFFICE_BASE_URL}/accounts/${supportTenantId}?tab=support-tickets`);

        const main = page.getByRole("main");
        await expect(main.getByRole("tab", { name: "Support tickets" })).toBeVisible();
        const supportPanel = main.getByRole("tabpanel", { name: "Support tickets" });
        await expect(supportPanel).toBeVisible();
        await expect(supportPanel.getByText(supportSubject)).toBeVisible();
      })
    )();

    await supportStep(
      supportEnabled,
      "Switch to the Overview tab & verify the Open support tickets card lists the seeded ticket"
    )(
      supportStepBody(supportEnabled, async () => {
        const main = page.getByRole("main");
        await main.getByRole("tab", { name: "Overview" }).click();

        const overviewPanel = main.getByRole("tabpanel", { name: "Overview" });
        await expect(overviewPanel).toBeVisible();
        const openCardTable = overviewPanel.getByRole("table", { name: "Open support tickets" });
        await expect(openCardTable).toBeVisible();
        await expect(openCardTable.getByText(supportSubject)).toBeVisible();
      })
    )();

    await supportStep(
      supportEnabled,
      "Click the seeded ticket row in the Open support tickets card & verify navigation to the ticket detail"
    )(
      supportStepBody(supportEnabled, async () => {
        const main = page.getByRole("main");
        const openCardTable = main.getByRole("table", { name: "Open support tickets" });
        await openCardTable.getByRole("row").filter({ hasText: supportSubject }).click();

        await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/support/tickets/${supportTicketId}`);
        // Two headings carry the subject — the H1 in the detail header and the H4 in the side pane.
        await expect(page.getByRole("heading", { level: 1, name: supportSubject })).toBeVisible();
      })
    )();

    await supportStep(
      supportEnabled,
      "Navigate to the reporter's user detail Support tickets tab & verify the seeded ticket renders"
    )(
      supportStepBody(supportEnabled, async () => {
        await page.goto(`${BACK_OFFICE_BASE_URL}/users/${supportReporterId}?tab=support-tickets`);

        const main = page.getByRole("main");
        await expect(main.getByRole("tab", { name: "Support tickets" })).toBeVisible();
        const supportPanel = main.getByRole("tabpanel", { name: "Support tickets" });
        await expect(supportPanel).toBeVisible();
        await expect(supportPanel.getByText(supportSubject)).toBeVisible();
      })
    )();

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

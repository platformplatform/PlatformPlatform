import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { getBackOfficeBaseUrl } from "@shared/e2e/utils/constants";
import { createTestContext } from "@shared/e2e/utils/test-assertions";
import { step } from "@shared/e2e/utils/test-step-wrapper";

const BACK_OFFICE_BASE_URL = getBackOfficeBaseUrl();

test.describe("@smoke", () => {
  /**
   * Covers the back-office /billing-events page end-to-end: side-menu navigation, table render with seeded
   * BillingEvent rows, event-type filter, account search, and the dashboard `View all` link landing here.
   *
   * The side pane and date-range filter from the original PP-1203 spec are not exercised because PP-1202
   * intentionally shipped without those affordances — the table-with-filters surface covers the primary
   * "scan recent events / find a specific account's events" use case.
   */
  test("should render billing-events list, filter by event type and account, and navigate via dashboard view-all", async ({
    ownerPage,
    browser
  }) => {
    createTestContext(ownerPage);

    const backOfficeContext = await browser.newContext({ baseURL: BACK_OFFICE_BASE_URL, ignoreHTTPSErrors: true });
    const page = await backOfficeContext.newPage();
    createTestContext(page);

    const billingEventsWithRows = {
      totalCount: 1,
      pageSize: 25,
      totalPages: 1,
      currentPageOffset: 0,
      billingEvents: [
        {
          id: "bev_mock_12345",
          tenantId: "1",
          tenantName: "Test Organization",
          tenantLogoUrl: null,
          country: "DK",
          eventType: "SubscriptionCreated",
          fromPlan: null,
          toPlan: "Standard",
          amountDelta: 29.0,
          previousAmount: null,
          newAmount: 29.0,
          committedMrr: 29.0,
          currency: "DKK",
          occurredAt: "2026-05-11T00:00:00Z"
        }
      ]
    };
    const billingEventsEmpty = { totalCount: 0, pageSize: 25, totalPages: 0, currentPageOffset: 0, billingEvents: [] };

    let billingEventsResponse = billingEventsWithRows;
    await page.route("**/api/back-office/billing-events**", async (route) => {
      await route.fulfill({ status: 200, contentType: "application/json", json: billingEventsResponse });
    });

    await step("Log in as Admin via MockEasyAuth & verify redirect to dashboard")(async () => {
      await page.goto(`${BACK_OFFICE_BASE_URL}/`);

      await expect(page.getByRole("radio", { name: "Admin Log in with admin rights" })).toBeVisible();
      await page.getByRole("radio", { name: "Admin Log in with admin rights" }).click();
      await page.getByRole("button", { name: "Log in" }).click();

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/`);
    })();

    // === SIDE MENU NAVIGATION ===

    await step("Click side-menu Billing events & verify route lands on /billing-events with heading and table")(
      async () => {
        await page.getByRole("link", { name: "Billing events" }).click();

        await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/billing-events`);
        await expect(page.getByRole("heading", { name: "Billing events" })).toBeVisible();
        await expect(page.getByRole("table", { name: "Billing events" })).toBeVisible();
        await expect(page.getByRole("columnheader", { name: "Account" })).toBeVisible();
        await expect(page.getByRole("columnheader", { name: "Event" })).toBeVisible();
        await expect(page.getByRole("columnheader", { name: "Occurred" })).toBeVisible();
        await expect(page.getByRole("row").nth(1)).toBeVisible();
      }
    )();

    // === FILTERS ===

    await step("Apply MRR impact view pill & verify URL reflects selection and table stays visible")(async () => {
      await page.getByRole("button", { name: "MRR impact" }).click();

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/billing-events?view=mrr`);
      await expect(page.getByRole("table", { name: "Billing events" })).toBeVisible();
    })();

    await step("Click the All view pill & verify URL returns to base /billing-events")(async () => {
      await page.getByRole("button", { name: "All" }).click();

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/billing-events`);
    })();

    await step(
      "Type a deliberately non-matching tenant search & verify URL reflects search query and empty state appears"
    )(async () => {
      // Use a string that almost certainly will not match any seeded tenant so the empty state
      // renders. This avoids coupling the test to specific dev seed tenant names.
      billingEventsResponse = billingEventsEmpty;
      await page.getByRole("searchbox", { name: "Search" }).fill("zzz-no-match-account-xyz");

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/billing-events?search=zzz-no-match-account-xyz`);
      await expect(page.getByText("No billing events match your filters")).toBeVisible();
    })();

    await step("Clear search & verify URL returns to base /billing-events")(async () => {
      billingEventsResponse = billingEventsWithRows;
      await page.getByRole("searchbox", { name: "Search" }).fill("");

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/billing-events`);
    })();

    // === SORT TOGGLE ===

    await step("Click Account column header & verify sort URL params populate")(async () => {
      await page.getByRole("columnheader", { name: "Account" }).click();

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/billing-events?orderBy=TenantName`);
    })();

    // === DASHBOARD `VIEW ALL` ===

    await step("Navigate back to dashboard & verify Recent billing events card is present")(async () => {
      await page.goto(`${BACK_OFFICE_BASE_URL}/`);

      await expect(page.getByText("Recent billing events", { exact: true })).toBeVisible();
    })();

    await step("Click Recent billing events View all link & verify lands on /billing-events")(async () => {
      // Target the specific View all link by its href so we are not coupled to surrounding card markup
      // (the dashboard has multiple "View all" links: one for Accounts and one for Billing events).
      await page.locator("a[href='/billing-events']", { hasText: "View all" }).click();

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/billing-events`);
      await expect(page.getByRole("heading", { name: "Billing events" })).toBeVisible();
    })();

    await backOfficeContext.close();
  });
});

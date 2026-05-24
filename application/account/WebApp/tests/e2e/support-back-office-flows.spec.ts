import { expect, type Page, request } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { getBackOfficeBaseUrl, getBaseUrl } from "@shared/e2e/utils/constants";
import { createTestContext, expectToastMessage } from "@shared/e2e/utils/test-assertions";
import { logInAsAdmin } from "@shared/e2e/utils/test-data";
import { step } from "@shared/e2e/utils/test-step-wrapper";
import { existsSync, readFileSync } from "node:fs";
import { join, resolve } from "node:path";

const BACK_OFFICE_BASE_URL = getBackOfficeBaseUrl();
const BASE_URL = getBaseUrl();

function getMailpitBaseUrl(): string {
  const repoRoot = resolve(__dirname, "..", "..", "..", "..", "..");
  const portFile = join(repoRoot, ".workspace", "port.txt");
  if (!existsSync(portFile)) return "http://localhost:9005";
  const basePort = Number.parseInt(readFileSync(portFile, "utf8").trim(), 10);
  return `http://localhost:${basePort + 5}`;
}

const MAILPIT_BASE = getMailpitBaseUrl();

// Flag-off skip-guard: navigate ownerPage (always provided) to a non-support route first so the
// reporter-API seed in the test body does not 404 before we can read the runtime env. SystemFeatureFlag
// values are config-driven and identical across the user-facing and back-office SPAs, so reading once
// from ownerPage is sufficient. Mirrors subscription-flows.spec.ts:6-14.
test.beforeEach(async ({ ownerPage }) => {
  await ownerPage.goto("/dashboard");
  const isSupportSystemEnabled = await ownerPage.evaluate(() => {
    const meta = document.head.querySelector('meta[name="runtimeEnv"]');
    const runtimeEnv = JSON.parse(meta?.getAttribute("content") ?? "{}");
    return runtimeEnv.PUBLIC_SUPPORT_SYSTEM_ENABLED === "true";
  });
  test.skip(!isSupportSystemEnabled, "Support system is not enabled (PUBLIC_SUPPORT_SYSTEM_ENABLED=false)");
});

interface MailpitSearchResult {
  messages: { ID: string; Subject: string }[];
}

async function searchMailpit(query: string): Promise<MailpitSearchResult> {
  const response = await fetch(`${MAILPIT_BASE}/api/v1/search?query=${encodeURIComponent(query)}&limit=10`);
  return (await response.json()) as MailpitSearchResult;
}

// Returns the reporter's email by reading the userInfoEnv meta tag the .NET shell injects into the
// authenticated SPA HTML.
async function readReporterEmail(authenticatedPage: Page): Promise<string> {
  const email = await authenticatedPage.evaluate(() => {
    const metaContent = document.head.querySelector('meta[name="userInfoEnv"]')?.getAttribute("content") ?? "{}";
    return (JSON.parse(metaContent) as { email?: string }).email ?? "";
  });
  if (!email.includes("@")) {
    throw new Error(`Could not read reporter email from userInfoEnv meta tag, got: '${email}'`);
  }
  return email;
}

// Pre-seeds a support ticket as the reporter via the user-facing API. The fixture-provided
// reporterPage's context is created without ignoreHTTPSErrors on macOS (the browser trusts the dev
// cert via keychain; Playwright's APIRequest stack does not), so a fresh request context is spun up
// from the reporter's storageState. /api/account/support-tickets is multipart and disables
// antiforgery, so no x-xsrf-token is required.
async function createReporterTicket(
  reporterPage: Page,
  subject: string,
  body: string,
  category = "Billing"
): Promise<string> {
  const storageState = await reporterPage.context().storageState();
  const reporterRequest = await request.newContext({ storageState, ignoreHTTPSErrors: true });
  const response = await reporterRequest.post(`${BASE_URL}/api/account/support-tickets`, {
    multipart: { subject, body, category }
  });
  expect(response.ok()).toBe(true);
  const raw = await response.text();
  await reporterRequest.dispose();
  // The endpoint returns the SupportTicketId as a bare JSON string (e.g., "tkt_01...").
  return JSON.parse(raw) as string;
}

// Posts an internal note (staff-only, no email) via the back-office API in the already-authenticated
// back-office page session. The /internal-note endpoint disables antiforgery.
async function postInternalNote(backOfficePage: Page, ticketId: string, body: string): Promise<void> {
  const response = await backOfficePage.request.post(
    `${BACK_OFFICE_BASE_URL}/api/back-office/support-tickets/${ticketId}/internal-note`,
    { multipart: { body } }
  );
  expect(response.ok()).toBe(true);
}

test.describe("@smoke", () => {
  /**
   * Back-office golden path for the support inbox from staff's perspective:
   * - Reporter pre-seeds a ticket via the end-user API (status=AwaitingAgent after PostUserMessage)
   * - Staff logs into the back-office, the Support sidebar group and Tickets link render
   * - Clicking the Awaiting agent stat tile filters the URL to ?status=AwaitingAgent and shows the
   *   seeded ticket
   * - Clicking the row opens the preview side pane and surfaces selectedTicketId on the URL
   * - Open ticket deep-links to /support/tickets/{id}
   * - Assign-to-me toggles the AssignControls button label and surfaces an Assignee updated toast
   * - Posting a Send & resolve reply transitions the ticket to Resolved (header pill updates) and a
   *   single Re:{subject} email lands in Mailpit addressed to the reporter
   */
  test("should triage, assign, reply, and resolve a ticket from the back-office inbox", async ({
    ownerPage,
    browser
  }) => {
    createTestContext(ownerPage);

    const backOfficeContext = await browser.newContext({ baseURL: BACK_OFFICE_BASE_URL, ignoreHTTPSErrors: true });
    const backOfficePage = await backOfficeContext.newPage();
    const backOfficeContextHandle = createTestContext(backOfficePage);

    const uniqueSuffix = `${Date.now()}-${Math.floor(Math.random() * 1_000_000)}`;
    const subject = `E2E smoke staff inbox ${uniqueSuffix}`;
    let ticketId = "";
    let reporterEmail = "";

    await step("Pre-seed a support ticket as the reporter & verify the API returns a ticket id")(async () => {
      await ownerPage.goto("/support/tickets");
      reporterEmail = await readReporterEmail(ownerPage);

      ticketId = await createReporterTicket(ownerPage, subject, "Investigating an inbox triage scenario.");
      expect(ticketId).toContain("tkt_");
    })();

    await step("Log in to back-office as Admin via MockEasyAuth & verify dashboard heading renders")(async () => {
      await backOfficePage.goto(`${BACK_OFFICE_BASE_URL}/`);

      await logInAsAdmin(backOfficePage, `${BACK_OFFICE_BASE_URL}/`);
      await expect(backOfficePage.getByRole("heading", { name: "Dashboard" })).toBeVisible();
    })();

    await step("Navigate to /support/tickets & verify the Support sidebar entry is visible")(async () => {
      await backOfficePage.goto(`${BACK_OFFICE_BASE_URL}/support/tickets`);

      await expect(backOfficePage.getByRole("heading", { name: "Support tickets" })).toBeVisible();
      await expect(backOfficePage.getByRole("link", { name: "Tickets" })).toBeVisible();
    })();

    await step(
      "Click the Awaiting agent stat tile & verify URL filters to status=AwaitingAgent and the seeded ticket row renders"
    )(async () => {
      // CreateTicketCommand chains PostUserMessage, which transitions the new ticket from the
      // initial New state to AwaitingAgent before it ever lands in the inbox. Filtering by
      // Awaiting agent (not New) is what surfaces the seeded ticket.
      await backOfficePage.getByRole("button", { name: "Awaiting agent" }).first().click();

      await expect(backOfficePage).toHaveURL(`${BACK_OFFICE_BASE_URL}/support/tickets?status=AwaitingAgent`);
      const seededRow = backOfficePage.locator(`tr[data-row-key="${ticketId}"]`);
      await expect(seededRow).toBeVisible();
    })();

    await step("Click the seeded ticket row & verify the preview side pane opens with the ticket subject")(async () => {
      // Filtering via ?status=AwaitingAgent triggers a refetch that swaps the entire <tbody>.
      // Wait for the row's data-row-key attribute to settle to the seeded ticket id before
      // clicking; otherwise Playwright retries on a detached element and exhausts the timeout.
      const seededRow = backOfficePage.locator(`tr[data-row-key="${ticketId}"]`);
      await expect(seededRow).toBeVisible();
      await seededRow.click();

      // The shared SidePane renders as <aside role="region"> with the configured aria-label,
      // not a role="dialog" element.
      const previewPane = backOfficePage.getByRole("region", { name: "Support ticket preview" });
      await expect(previewPane).toBeVisible();
      await expect(previewPane.getByText(subject)).toBeVisible();
    })();

    await step("Click Open ticket in the side pane & verify navigation to the back-office ticket detail page")(
      async () => {
        await backOfficePage.getByRole("button", { name: "Open ticket" }).click();

        await expect(backOfficePage).toHaveURL(`${BACK_OFFICE_BASE_URL}/support/tickets/${ticketId}`);
        // Two headings carry the subject — the H1 in the detail header and the H4 in the side pane.
        await expect(backOfficePage.getByRole("heading", { level: 1, name: subject })).toBeVisible();
      }
    )();

    await step("Click Assign to me & verify Assignee updated toast and the button switches to Assigned to you")(
      async () => {
        await backOfficePage.getByRole("button", { name: "Assign to me" }).first().click();

        await expectToastMessage(backOfficeContextHandle, "Assignee updated");
        await expect(backOfficePage.getByRole("button", { name: "Assigned to you" }).first()).toBeVisible();
      }
    )();

    await step("Compose a public reply, switch primary action to Send & resolve, click & verify Reply sent toast")(
      async () => {
        await backOfficePage
          .getByPlaceholder("Reply to the user… markdown supported")
          .fill("Glad to help — resolving this for you now.");
        await backOfficePage.getByRole("button", { name: "More send options" }).click();
        const sendMenu = backOfficePage.getByRole("menu");
        await expect(sendMenu).toBeVisible();
        await sendMenu.getByRole("menuitem", { name: "Send & resolve" }).dispatchEvent("click");
        await expect(sendMenu).not.toBeVisible();

        await backOfficePage.getByRole("button", { name: "Send & resolve" }).click();

        await expectToastMessage(backOfficeContextHandle, "Reply sent");
        await expect(backOfficePage.getByText("Resolved").first()).toBeVisible();
      }
    )();

    await step("Query Mailpit for the Re:{subject} message addressed to the reporter & verify exactly one delivery")(
      async () => {
        const search = await searchMailpit(`subject:"Re: ${subject}" to:${reporterEmail}`);

        expect(search.messages.length).toBe(1);
        expect(search.messages[0].Subject).toContain(subject);
      }
    )();

    await backOfficeContext.close();
  });
});

test.describe("@comprehensive", () => {
  /**
   * Deeper coverage of the back-office support inbox surface:
   * - Reporter seeds two tickets with a shared uniqueSuffix so search is deterministic across
   *   parallel workers
   * - Search by reporter email returns both seeded tickets
   * - Search by the uniqueSuffix returns both seeded tickets (subject substring match)
   * - An internal note posted via the API leaves the ticket status unchanged, fires no email to the
   *   reporter, renders with the distinct Internal note badge in the back-office, and is NOT visible
   *   from the reporter's ticket detail page
   * - Search by the internal-note body returns the parent ticket (staff search hits internal notes)
   * - Sort by Assignee flips orderBy and sortOrder URL params on subsequent clicks
   * - PageOffset overflow returns 400 with overflow copy
   */
  test("should filter, search, sort, paginate, and surface internal notes without affecting reporter view", async ({
    ownerPage,
    browser
  }) => {
    createTestContext(ownerPage);

    const backOfficeContext = await browser.newContext({ baseURL: BACK_OFFICE_BASE_URL, ignoreHTTPSErrors: true });
    const backOfficePage = await backOfficeContext.newPage();
    createTestContext(backOfficePage);

    const uniqueSuffix = `${Date.now()}-${Math.floor(Math.random() * 1_000_000)}`;
    const subjectAlpha = `E2E inbox alpha ${uniqueSuffix}`;
    const subjectBeta = `E2E inbox beta ${uniqueSuffix}`;
    const internalNoteBody = `Internal triage marker ${uniqueSuffix}`;
    let ticketIdAlpha = "";
    let reporterEmail = "";

    // The beta ticket's *id* is never referenced after seeding — only its *existence* matters, so
    // the internal-note-search step can assert that searching by the alpha-only note body filters
    // beta out of the results. Discard the returned id intentionally.
    await step("Pre-seed two reporter tickets with a shared uniqueSuffix & verify both ids are returned")(async () => {
      await ownerPage.goto("/support/tickets");
      reporterEmail = await readReporterEmail(ownerPage);

      ticketIdAlpha = await createReporterTicket(
        ownerPage,
        subjectAlpha,
        "First inbox scenario for comprehensive run."
      );
      await createReporterTicket(ownerPage, subjectBeta, "Second inbox scenario for comprehensive run.");
      expect(ticketIdAlpha).toContain("tkt_");
    })();

    await step("Log in to back-office as Admin & navigate to /support/tickets & verify the inbox renders")(async () => {
      await backOfficePage.goto(`${BACK_OFFICE_BASE_URL}/support/tickets`);

      await logInAsAdmin(backOfficePage, `${BACK_OFFICE_BASE_URL}/support/tickets`);
      await expect(backOfficePage.getByRole("heading", { name: "Support tickets" })).toBeVisible();
    })();

    // === SEARCH ===

    const searchBox = backOfficePage.getByRole("searchbox", { name: "Search" });

    await step("Search by the reporter email & verify both seeded tickets appear")(async () => {
      await searchBox.fill(reporterEmail);

      await expect(backOfficePage.getByRole("row").filter({ hasText: subjectAlpha })).toBeVisible();
      await expect(backOfficePage.getByRole("row").filter({ hasText: subjectBeta })).toBeVisible();
    })();

    await step("Search by the worker tenant subject suffix & verify only the seeded tickets appear")(async () => {
      await searchBox.fill(uniqueSuffix);

      await expect(backOfficePage.getByRole("row").filter({ hasText: subjectAlpha })).toBeVisible();
      await expect(backOfficePage.getByRole("row").filter({ hasText: subjectBeta })).toBeVisible();
    })();

    await step("Post an internal note on the alpha ticket via the back-office API & verify no error")(async () => {
      await postInternalNote(backOfficePage, ticketIdAlpha, internalNoteBody);
    })();

    await step("Search by the internal note body & verify only the alpha ticket is returned")(async () => {
      await searchBox.fill(internalNoteBody);

      await expect(backOfficePage.getByRole("row").filter({ hasText: subjectAlpha })).toBeVisible();
      await expect(backOfficePage.getByRole("row").filter({ hasText: subjectBeta })).not.toBeVisible();
    })();

    await step("Clear the search & verify the URL drops the search param")(async () => {
      await searchBox.fill("");

      await expect(backOfficePage).toHaveURL(`${BACK_OFFICE_BASE_URL}/support/tickets`);
    })();

    // === SORT (URL FLIP) ===

    await step("Click the Assignee column header & verify the URL switches to orderBy=Assignee")(async () => {
      await backOfficePage.getByRole("columnheader", { name: "Assignee" }).click();

      await expect(backOfficePage).toHaveURL(`${BACK_OFFICE_BASE_URL}/support/tickets?orderBy=Assignee`);
    })();

    await step("Click the Assignee column header again & verify the URL flips to ascending")(async () => {
      await backOfficePage.getByRole("columnheader", { name: "Assignee" }).click();

      await expect(backOfficePage).toHaveURL(
        `${BACK_OFFICE_BASE_URL}/support/tickets?orderBy=Assignee&sortOrder=Ascending`
      );
    })();

    // === INTERNAL NOTE EFFECTS: STATUS UNCHANGED, NO EMAIL, BUBBLE VISIBLE, HIDDEN FROM REPORTER ===

    await step(
      "Open the alpha ticket detail page & verify the internal-note bubble renders with the Internal note badge"
    )(async () => {
      await backOfficePage.goto(`${BACK_OFFICE_BASE_URL}/support/tickets/${ticketIdAlpha}`);

      // Two headings carry the subject — the H1 in the detail header and the H4 in the side pane.
      // Scope to level 1 so the assertion targets the page header explicitly.
      await expect(backOfficePage.getByRole("heading", { level: 1, name: subjectAlpha })).toBeVisible();
      await expect(backOfficePage.getByText(internalNoteBody)).toBeVisible();
      await expect(backOfficePage.getByText("Internal note").first()).toBeVisible();
      await expect(backOfficePage.getByText("Visible only to PlatformPlatform staff").first()).toBeVisible();
      // Status pill must still read Awaiting agent — the ticket transitioned to AwaitingAgent on
      // creation (CreateTicketCommand chains PostUserMessage) and an internal note is not a state
      // transition, so posting one does not move the status.
      await expect(backOfficePage.getByText("Awaiting agent").first()).toBeVisible();
    })();

    await step("Search Mailpit for messages mentioning the internal-note body & verify zero deliveries")(async () => {
      const search = await searchMailpit(`"${internalNoteBody}"`);

      expect(search.messages.length).toBe(0);
    })();

    await step("Open the same ticket on the reporter-facing page & verify the internal note text is not visible")(
      async () => {
        await ownerPage.goto(`/support/tickets/${ticketIdAlpha}`);

        await expect(ownerPage.getByRole("heading", { name: subjectAlpha })).toBeVisible();
        await expect(ownerPage.getByText(internalNoteBody)).not.toBeVisible();
        await expect(ownerPage.getByText("Internal note")).not.toBeVisible();
      }
    )();

    // === PAGE OVERFLOW ===

    await step("Request an overflowing pageOffset via the back-office API & verify 400 with overflow copy")(
      async () => {
        const response = await backOfficePage.request.get(
          `${BACK_OFFICE_BASE_URL}/api/back-office/support-tickets?PageSize=25&PageOffset=999`
        );

        expect(response.status()).toBe(400);
        const body = await response.text();
        expect(body).toContain("page offset");
      }
    )();

    await backOfficeContext.close();
  });
});

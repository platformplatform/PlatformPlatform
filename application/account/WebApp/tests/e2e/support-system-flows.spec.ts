import { expect, type Page, request } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { getBackOfficeBaseUrl, getBaseUrl } from "@shared/e2e/utils/constants";
import { createTestContext, expectNetworkErrors, expectToastMessage } from "@shared/e2e/utils/test-assertions";
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

// Flag-off skip-guard: navigate ownerPage to a non-support route first so a flag-off state does
// not 404 the support endpoints before we can read the runtime env. Mirrors subscription-flows.spec.ts:6-14.
test.beforeEach(async ({ ownerPage }) => {
  await ownerPage.goto("/dashboard");
  const isSupportSystemEnabled = await ownerPage.evaluate(() => {
    const meta = document.head.querySelector('meta[name="runtimeEnv"]');
    const runtimeEnv = JSON.parse(meta?.getAttribute("content") ?? "{}");
    return runtimeEnv.PUBLIC_SUPPORT_SYSTEM_ENABLED === "true";
  });
  test.skip(!isSupportSystemEnabled, "Support system is not enabled (PUBLIC_SUPPORT_SYSTEM_ENABLED=false)");
});

interface MailpitMessage {
  subject: string;
  html: string;
  text: string;
}

// Mailpit's database is shared across all workers, so search by the unique reporter address keeps
// each worker's assertion deterministic without any reset between tests.
async function fetchLatestMailByRecipient(recipient: string): Promise<MailpitMessage> {
  const searchResponse = await fetch(`${MAILPIT_BASE}/api/v1/search?query=to:${encodeURIComponent(recipient)}&limit=1`);
  const searchData = (await searchResponse.json()) as { messages: { ID: string }[] };
  const messageId = searchData.messages[0]?.ID;
  if (!messageId) {
    throw new Error(`No email found in Mailpit for recipient: ${recipient}`);
  }
  const messageResponse = await fetch(`${MAILPIT_BASE}/api/v1/message/${messageId}`);
  const message = (await messageResponse.json()) as { Subject: string; HTML: string; Text: string };
  return { subject: message.Subject, html: message.HTML, text: message.Text };
}

// 1x1 transparent PNG used as the canonical small attachment fixture; constructed inline so the
// test does not depend on any on-disk file shipped alongside the spec.
const PNG_1X1_BASE64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";
const SMALL_PNG_BUFFER = Buffer.from(PNG_1X1_BASE64, "base64");

function buildPngAttachment(fileName: string) {
  return { name: fileName, mimeType: "image/png", buffer: SMALL_PNG_BUFFER };
}

// Posts a staff reply against the back-office API using the back-office page's already-authenticated
// session. The /api/back-office/support-tickets/{id}/reply endpoint is multipart and disables
// antiforgery, so no x-xsrf-token is required (mirrors the .DisableAntiforgery() call site).
async function postStaffReply(
  backOfficePage: Page,
  ticketId: string,
  body: string,
  markAsResolved: boolean
): Promise<void> {
  const response = await backOfficePage.request.post(
    `${BACK_OFFICE_BASE_URL}/api/back-office/support-tickets/${ticketId}/reply`,
    {
      multipart: {
        body,
        markAsResolved: markAsResolved ? "true" : "false"
      }
    }
  );
  expect(response.ok()).toBe(true);
}

// Extracts the ticket id from /support/tickets/{id} or /support/tickets/{id}/close URLs.
function extractTicketIdFromUrl(currentUrl: string): string {
  const match = currentUrl.match(/\/support\/tickets\/([^/?#]+)/);
  if (!match) throw new Error(`Could not extract ticket id from URL: ${currentUrl}`);
  return match[1];
}

test.describe("@smoke", () => {
  /**
   * End-to-end golden path for the in-app support system from the reporter's perspective:
   * - Reporter opens a new ticket via the user-facing /support/tickets/new form
   * - Detail page renders with subject, status pill, and Support sidebar entry
   * - Staff posts an unresolved reply via the back-office API → status flips to AwaitingUser, the
   *   sidebar badge increments to 1 on the user side after a refetch
   * - Mailpit stores the rendered staff-reply email addressed to the reporter
   * - Staff posts a follow-up reply with markAsResolved=true → status flips to Resolved, the
   *   ClosedTicketFooter renders with a "Rate this support" CTA
   * - Reporter submits CSAT (Helpful + comment) on /close → "Thanks for the feedback" renders
   * - Reporter reopens the (now Closed) ticket → status flips back to AwaitingAgent and the
   *   sidebar entry remains visible
   */
  test("should create, reply, reopen, and close a support ticket end-to-end", async ({ ownerPage, browser }) => {
    createTestContext(ownerPage);

    const backOfficeContext = await browser.newContext({ baseURL: BACK_OFFICE_BASE_URL, ignoreHTTPSErrors: true });
    const backOfficePage = await backOfficeContext.newPage();
    createTestContext(backOfficePage);

    const uniqueSuffix = `${Date.now()}-${Math.floor(Math.random() * 1_000_000)}`;
    const subject = `E2E smoke ticket ${uniqueSuffix}`;

    let ticketId = "";
    let reporterEmail = "";

    await step(
      "Open new-ticket form & verify Tell us what's going on heading renders and reporter email is exposed in userInfo"
    )(async () => {
      await ownerPage.goto("/support/tickets/new");

      await expect(ownerPage.getByRole("heading", { name: "Tell us what's going on" })).toBeVisible();
      reporterEmail = await ownerPage.evaluate(() => {
        const metaContent = document.head.querySelector('meta[name="userInfoEnv"]')?.getAttribute("content") ?? "{}";
        return (JSON.parse(metaContent) as { email?: string }).email ?? "";
      });
      expect(reporterEmail).toContain("@");
    })();

    await step("Submit ticket with subject, body, default Billing category & verify detail page renders")(async () => {
      await ownerPage.getByRole("textbox", { name: "Subject" }).fill(subject);
      await ownerPage.getByLabel("What's happening?").fill("My invoice shows the wrong amount for the latest cycle.");
      await ownerPage.getByRole("button", { name: "Send to support" }).click();

      await expect(ownerPage).toHaveURL(/\/support\/tickets\/[^/]+$/);
      await expect(ownerPage.getByRole("heading", { name: subject })).toBeVisible();
      await expect(ownerPage.getByText("Billing", { exact: true }).first()).toBeVisible();
      ticketId = extractTicketIdFromUrl(ownerPage.url());
    })();

    await step("Open Support sidebar entry & verify My tickets link is visible on detail page")(async () => {
      await expect(ownerPage.getByRole("link", { name: "My tickets" })).toBeVisible();
    })();

    await step("Log in to back-office as Admin via MockEasyAuth & verify dashboard heading renders")(async () => {
      await backOfficePage.goto(`${BACK_OFFICE_BASE_URL}/`);

      await logInAsAdmin(backOfficePage, `${BACK_OFFICE_BASE_URL}/`);
      await expect(backOfficePage.getByRole("heading", { name: "Dashboard" })).toBeVisible();
    })();

    await step(
      "Post a staff reply without markAsResolved via back-office API & verify the ticket card shows Awaiting your reply on the user-facing My tickets page"
    )(async () => {
      await postStaffReply(backOfficePage, ticketId, "Thanks for reaching out — taking a look now.", false);

      await ownerPage.goto("/support/tickets");
      await expect(ownerPage.getByRole("heading", { name: "Support tickets" })).toBeVisible();
      const ticketCard = ownerPage.getByRole("link", { name: subject });
      await expect(ticketCard).toBeVisible();
      await expect(ticketCard.getByText("Awaiting your reply")).toBeVisible();
    })();

    await step("Fetch staff-reply email from Mailpit & verify subject and rendered HTML address the reporter")(
      async () => {
        const mail = await fetchLatestMailByRecipient(reporterEmail);

        expect(mail.subject).toContain("Re:");
        expect(mail.subject).toContain(subject);
        expect(mail.html).toContain("replied to your ticket");
        expect(mail.html).toContain("Thanks for reaching out — taking a look now.");
        expect(mail.text).toContain("replied to your ticket");
      }
    )();

    await step(
      "Post a follow-up staff reply with markAsResolved=true & verify Marked as solved pill renders after refetch"
    )(async () => {
      await postStaffReply(backOfficePage, ticketId, "Glad we sorted that out — marking as resolved.", true);

      await ownerPage.goto(`/support/tickets/${ticketId}`);
      await expect(ownerPage.getByRole("heading", { name: subject })).toBeVisible();
      await expect(ownerPage.getByText("Marked as solved").first()).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Reopen ticket" })).toBeVisible();
    })();

    await step("Click Rate this support & verify navigation to /close")(async () => {
      await ownerPage.getByRole("link", { name: "Rate this support" }).click();

      await expect(ownerPage).toHaveURL(`/support/tickets/${ticketId}/close`);
      await expect(ownerPage.getByRole("heading", { name: "Thanks for closing this out" })).toBeVisible();
    })();

    await step("Submit Helpful CSAT with a comment & verify Thanks for the feedback renders")(async () => {
      await ownerPage.getByRole("button", { name: "Helpful" }).click();
      await ownerPage.getByPlaceholder("What worked well?").fill("Quick and clear answer, thank you.");
      await ownerPage.getByRole("button", { name: "Submit feedback" }).click();

      await expect(ownerPage.getByRole("heading", { name: "Thanks for the feedback" })).toBeVisible();
    })();

    await step("Reopen the closed ticket from /close & verify status flips back to Waiting on support")(async () => {
      await ownerPage.getByRole("button", { name: "Reopen" }).click();

      await expect(ownerPage).toHaveURL(`/support/tickets/${ticketId}`);
      await expect(ownerPage.getByText("Waiting on support").first()).toBeVisible();
      await expect(ownerPage.getByRole("link", { name: "My tickets" })).toBeVisible();
    })();

    await backOfficeContext.close();
  });
});

test.describe("@comprehensive", () => {
  /**
   * Deeper coverage of the reporter-facing support surface:
   * - Attachments: a valid .png lands on the ticket, an oversized/disallowed file surfaces a toast,
   *   and the download endpoint enforces Content-Disposition: attachment
   * - CSAT staleness: after reopen + staff re-reply + user re-resolve, the previously-submitted CSAT
   *   is treated as stale and the prompt re-appears
   * - Cross-reporter access: a second user in the same tenant cannot load another user's ticket; the
   *   404 surfaces as the "Unable to load ticket." inline message (no toast)
   * - Anonymous access: an unauthenticated session is redirected to /login
   */
  test("should handle attachments, validation, CSAT staleness, cross-reporter access, and anonymous redirect", async ({
    ownerPage,
    memberPage,
    browser
  }) => {
    const ownerContext = createTestContext(ownerPage);
    const memberContext = createTestContext(memberPage);

    const backOfficeContext = await browser.newContext({ baseURL: BACK_OFFICE_BASE_URL, ignoreHTTPSErrors: true });
    const backOfficePage = await backOfficeContext.newPage();
    createTestContext(backOfficePage);

    const uniqueSuffix = `${Date.now()}-${Math.floor(Math.random() * 1_000_000)}`;
    const subject = `E2E comprehensive ticket ${uniqueSuffix}`;
    const attachmentFileName = `screenshot-${uniqueSuffix}.png`;
    const disallowedFileName = `notes-${uniqueSuffix}.exe`;

    let ticketId = "";

    await step("Log in to back-office as Admin via MockEasyAuth & verify dashboard heading renders")(async () => {
      await backOfficePage.goto(`${BACK_OFFICE_BASE_URL}/`);

      await logInAsAdmin(backOfficePage, `${BACK_OFFICE_BASE_URL}/`);
      await expect(backOfficePage.getByRole("heading", { name: "Dashboard" })).toBeVisible();
    })();

    // === CREATE WITH ATTACHMENTS ===

    await step("Open new-ticket form & attach a 1x1 PNG via the hidden file input & verify chip renders")(async () => {
      await ownerPage.goto("/support/tickets/new");

      await ownerPage.getByRole("textbox", { name: "Subject" }).fill(subject);
      await ownerPage.getByLabel("What's happening?").fill("Attaching a screenshot of what I see.");
      await ownerPage.locator('input[type="file"]').setInputFiles([buildPngAttachment(attachmentFileName)]);

      await expect(ownerPage.getByText(attachmentFileName)).toBeVisible();
    })();

    await step("Attach a disallowed .exe & verify rejection toast surfaces and chip is not added")(async () => {
      await ownerPage
        .locator('input[type="file"]')
        .setInputFiles([
          { name: disallowedFileName, mimeType: "application/octet-stream", buffer: Buffer.from("not a real exe") }
        ]);

      await expectToastMessage(ownerContext, `${disallowedFileName} is not an allowed file type`);
      await expect(ownerPage.getByText(disallowedFileName)).not.toBeVisible();
    })();

    await step("Attach 5 additional valid PNGs (6 total) & verify the overflow toast surfaces and only 5 chips remain")(
      async () => {
        await ownerPage
          .locator('input[type="file"]')
          .setInputFiles([
            buildPngAttachment(`extra-1-${uniqueSuffix}.png`),
            buildPngAttachment(`extra-2-${uniqueSuffix}.png`),
            buildPngAttachment(`extra-3-${uniqueSuffix}.png`),
            buildPngAttachment(`extra-4-${uniqueSuffix}.png`),
            buildPngAttachment(`extra-5-${uniqueSuffix}.png`)
          ]);

        await expectToastMessage(ownerContext, "A ticket can have at most 5 attachments");
        await expect(ownerPage.getByText(`extra-5-${uniqueSuffix}.png`)).not.toBeVisible();
      }
    )();

    await step("Send the ticket & verify the attachment chip renders inside the message bubble on detail page")(
      async () => {
        await ownerPage.getByRole("button", { name: "Send to support" }).click();

        await expect(ownerPage).toHaveURL(/\/support\/tickets\/[^/]+$/);
        await expect(ownerPage.getByRole("heading", { name: subject })).toBeVisible();
        await expect(ownerPage.getByLabel(`Download attachment ${attachmentFileName}`)).toBeVisible();
        ticketId = extractTicketIdFromUrl(ownerPage.url());
      }
    )();

    // === ATTACHMENT DOWNLOAD CONTENT-DISPOSITION ===

    await step(
      "Issue a GET against the attachment URL with the reporter's session & verify Content-Disposition is attachment"
    )(async () => {
      const attachmentLink = ownerPage.getByLabel(`Download attachment ${attachmentFileName}`);
      const attachmentUrl = await attachmentLink.getAttribute("href");
      expect(attachmentUrl).not.toBeNull();

      // The fixture-provided ownerPage's context is created without ignoreHTTPSErrors on macOS
      // (the user-facing baseURL works because the OS trusts the dev cert; Playwright's APIRequest
      // stack does not). Spin up a dedicated APIRequestContext from the owner's storageState so
      // the request reuses the same auth cookies but skips the TLS check.
      const ownerStorageState = await ownerPage.context().storageState();
      const ownerRequestContext = await request.newContext({
        storageState: ownerStorageState,
        ignoreHTTPSErrors: true
      });
      const response = await ownerRequestContext.get(`${BASE_URL}${attachmentUrl}`);
      expect(response.ok()).toBe(true);
      const disposition = response.headers()["content-disposition"] ?? "";
      expect(disposition.toLowerCase()).toContain("attachment");
      expect(disposition).toContain(attachmentFileName);
      await ownerRequestContext.dispose();
    })();

    // === CSAT STALENESS AFTER REOPEN + RE-RESOLVE ===

    await step("Staff resolves the ticket via back-office API & verify Marked as solved pill renders after refetch")(
      async () => {
        await postStaffReply(backOfficePage, ticketId, "Resolving for the first round.", true);

        await ownerPage.goto(`/support/tickets/${ticketId}`);
        await expect(ownerPage.getByText("Marked as solved").first()).toBeVisible();
      }
    )();

    await step("Submit Helpful CSAT on /close & verify Thanks for the feedback renders")(async () => {
      await ownerPage.getByRole("link", { name: "Rate this support" }).click();
      await expect(ownerPage).toHaveURL(`/support/tickets/${ticketId}/close`);

      await ownerPage.getByRole("button", { name: "Helpful" }).click();
      await ownerPage.getByPlaceholder("What worked well?").fill("Initial fix worked.");
      await ownerPage.getByRole("button", { name: "Submit feedback" }).click();

      await expect(ownerPage.getByRole("heading", { name: "Thanks for the feedback" })).toBeVisible();
    })();

    await step(
      "Reopen the closed ticket, post a staff re-reply with markAsResolved=true & verify Rate this support CTA returns"
    )(async () => {
      await ownerPage.getByRole("button", { name: "Reopen" }).click();
      await expect(ownerPage).toHaveURL(`/support/tickets/${ticketId}`);
      await expect(ownerPage.getByText("Waiting on support").first()).toBeVisible();

      await postStaffReply(backOfficePage, ticketId, "Second round — resolving again.", true);

      await ownerPage.goto(`/support/tickets/${ticketId}`);
      await expect(ownerPage.getByText("Marked as solved").first()).toBeVisible();
      await expect(ownerPage.getByRole("link", { name: "Rate this support" })).toBeVisible();
    })();

    await step("Navigate to /close & verify the CSAT prompt re-appears because the prior rating is stale")(async () => {
      await ownerPage.getByRole("link", { name: "Rate this support" }).click();

      await expect(ownerPage).toHaveURL(`/support/tickets/${ticketId}/close`);
      await expect(ownerPage.getByRole("heading", { name: "Thanks for closing this out" })).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Submit feedback" })).toBeVisible();
    })();

    // === CROSS-REPORTER ACCESS NEGATIVE ===

    await step(
      "Navigate as a different tenant user to the owner's ticket URL & verify Unable to load ticket inline message"
    )(async () => {
      await memberPage.goto(`/support/tickets/${ticketId}`);

      await expect(memberPage.getByText("Unable to load ticket.")).toBeVisible();
      await expectNetworkErrors(memberContext, [404]);
    })();

    // === ANONYMOUS REDIRECT ===

    await step("Log out the owner & verify visiting the ticket URL anonymously redirects to /login")(async () => {
      const anonymousContext = await browser.newContext({ ignoreHTTPSErrors: true });
      const anonymousPage = await anonymousContext.newPage();
      createTestContext(anonymousPage);

      await anonymousPage.goto(`/support/tickets/${ticketId}`);

      await expect(anonymousPage).toHaveURL(/\/login/);
      await anonymousContext.close();
    })();

    await backOfficeContext.close();
  });
});

import { expect, request } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { getBackOfficeBaseUrl, getBaseUrl } from "@shared/e2e/utils/constants";
import { createTestContext } from "@shared/e2e/utils/test-assertions";
import { step } from "@shared/e2e/utils/test-step-wrapper";

const BACK_OFFICE_BASE_URL = getBackOfficeBaseUrl();
const BASE_URL = getBaseUrl();

test.describe("@smoke", () => {
  // Verifies MockEasyAuth redirect, DevEasyAuth cookie, identity claims, and SPA shell on the back-office host.
  test("should redirect unauthenticated user to mock easy auth and return identity claims after impersonation", async ({
    browser
  }) => {
    const browserContext = await browser.newContext({ baseURL: BACK_OFFICE_BASE_URL, ignoreHTTPSErrors: true });
    const page = await browserContext.newPage();
    createTestContext(page);

    await step("Navigate to back-office root unauthenticated & verify redirect to mock easy auth login")(async () => {
      const response = await page.request.get(`${BACK_OFFICE_BASE_URL}/`, {
        headers: { Accept: "text/html" },
        maxRedirects: 0
      });

      expect(response.status()).toBe(302);
      expect(response.headers().location).toBe("/.auth/login/aad?post_login_redirect_uri=%2F");
    })();

    await step("Navigate to authenticated back-office endpoint & verify redirect to mock easy auth picker")(
      async () => {
        await page.goto("/api/back-office/me");

        await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/login?returnPath=%2Fapi%2Fback-office%2Fme`);
        await expect(page.getByRole("heading", { name: "BackOffice - Localhost" })).toBeVisible();
        await expect(page.getByRole("radio", { name: "Admin Log in with admin rights" })).toBeVisible();
      }
    )();

    await step("Pick the Admin identity & verify callback redirects back to the protected endpoint")(async () => {
      await page.getByRole("radio", { name: "Admin Log in with admin rights" }).click();
      await page.getByRole("button", { name: "Log in" }).click();

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/api/back-office/me`);
    })();

    await step("Call /api/back-office/me with DevEasyAuth cookie & verify identity payload")(async () => {
      const response = await page.request.get(`${BACK_OFFICE_BASE_URL}/api/back-office/me`, {
        headers: { Accept: "application/json" }
      });

      expect(response.status()).toBe(200);
      const payload = await response.json();
      expect(payload.displayName).toBe("Admin");
      expect(payload.groups).toContain("BackOfficeAdmins");
    })();

    await step(
      "Visit back-office root authenticated & verify SPA shell embeds back-office bundle URL and authenticated userInfo"
    )(async () => {
      const response = await page.request.get(`${BACK_OFFICE_BASE_URL}/`, {
        headers: { Accept: "text/html" }
      });

      expect(response.status()).toBe(200);
      const body = await response.text();
      expect(body).toContain('id="back-office"');
      expect(body).toContain("<title>Back Office</title>");
      expect(body).toContain("back-office.dev.localhost");
      expect(body).not.toContain("/account/static/");
      expect(body).toContain("&quot;isAuthenticated&quot;:true");
    })();

    await browserContext.close();
  });
});

test.describe("@smoke", () => {
  // Verifies back-office endpoints are host-scoped and that account session cookies cannot authorize back-office requests.
  test("should isolate back-office endpoints to the back-office host and reject account-authenticated requests", async ({
    ownerPage
  }) => {
    createTestContext(ownerPage);

    const accountStorageState = await ownerPage.context().storageState();
    const accountAuthenticatedContext = await request.newContext({
      storageState: accountStorageState,
      ignoreHTTPSErrors: true
    });
    const anonymousApiContext = await request.newContext({ ignoreHTTPSErrors: true });

    await step("GET /api/back-office/me on user-facing host with account session & verify 404 from RequireHost")(
      async () => {
        const response = await accountAuthenticatedContext.get(`${BASE_URL}/api/back-office/me`, {
          headers: { Accept: "application/json" },
          maxRedirects: 0
        });

        expect(response.status()).toBe(404);
      }
    )();

    await step(
      "GET /api/back-office/me on back-office host with account session and JSON Accept & verify 401 (BackOfficeIdentity ignores account cookies, and subdomain scoping prevents cross-host attachment)"
    )(async () => {
      const response = await accountAuthenticatedContext.get(`${BACK_OFFICE_BASE_URL}/api/back-office/me`, {
        headers: { Accept: "application/json" },
        maxRedirects: 0
      });

      expect(response.status()).toBe(401);
    })();

    await step(
      "GET /api/back-office/me on back-office host with account session and HTML Accept & verify redirect to mock easy auth login"
    )(async () => {
      const response = await accountAuthenticatedContext.get(`${BACK_OFFICE_BASE_URL}/api/back-office/me`, {
        headers: { Accept: "text/html" },
        maxRedirects: 0
      });

      expect(response.status()).toBe(302);
      expect(response.headers().location).toBe("/.auth/login/aad?post_login_redirect_uri=%2Fapi%2Fback-office%2Fme");
    })();

    await step("GET /api/account/users on user-facing host with no cookie & verify 401 regression")(async () => {
      const response = await anonymousApiContext.get(`${BASE_URL}/api/account/users`, {
        headers: { Accept: "application/json" },
        maxRedirects: 0
      });

      expect(response.status()).toBe(401);
    })();

    await step(
      "GET /api/account/users/me on back-office host with account session & verify 401 (account endpoints serve on any host post-split, but the account session cookie is host-scoped to the user-facing host so cross-host requests fail authentication)"
    )(async () => {
      const response = await accountAuthenticatedContext.get(`${BACK_OFFICE_BASE_URL}/api/account/users/me`, {
        headers: { Accept: "application/json" },
        maxRedirects: 0
      });

      expect(response.status()).toBe(401);
    })();

    await accountAuthenticatedContext.dispose();
    await anonymousApiContext.dispose();
  });
});

test.describe("@smoke", () => {
  // Verifies accounts table, side pane, detail KPI cards, and Users tab render correctly.
  test("should render accounts list, open side pane, and navigate to detail page with tabs", async ({
    ownerPage,
    browser
  }) => {
    createTestContext(ownerPage);

    const backOfficeContext = await browser.newContext({ baseURL: BACK_OFFICE_BASE_URL, ignoreHTTPSErrors: true });
    const page = await backOfficeContext.newPage();
    createTestContext(page);

    await step("Log in as Admin via MockEasyAuth & verify redirect to accounts list")(async () => {
      await page.goto(`${BACK_OFFICE_BASE_URL}/accounts`);

      await expect(page.getByRole("radio", { name: "Admin Log in with admin rights" })).toBeVisible();
      await page.getByRole("radio", { name: "Admin Log in with admin rights" }).click();
      await page.getByRole("button", { name: "Log in" }).click();

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/accounts`);
    })();

    await step("Load accounts page & verify table renders with Name and Status columns and at least one row")(
      async () => {
        await expect(page.getByRole("table", { name: "Accounts" })).toBeVisible();
        await expect(page.getByRole("columnheader", { name: "Name" })).toBeVisible();
        await expect(page.getByRole("columnheader", { name: "Status" })).toBeVisible();
        await expect(page.getByRole("row").nth(1)).toBeVisible();
      }
    )();

    await step("Click first account row & verify side pane opens with Plan & revenue and Owners sections")(async () => {
      await page.getByRole("row").nth(1).click();

      await expect(page.getByRole("region", { name: "Account preview" })).toBeVisible();
      await expect(page.getByText("Plan & revenue")).toBeVisible();
      await expect(page.getByText("Owners")).toBeVisible();
    })();

    await step("Open account detail & verify KPI cards and Owners heading on Overview tab")(async () => {
      await page.getByRole("button", { name: "Open account" }).click();

      const main = page.getByRole("main");

      await expect(main.getByText("MRR")).toBeVisible();
      await expect(main.getByText("Lifetime value")).toBeVisible();
      await expect(main.getByRole("tab", { name: "Users" })).toBeVisible();
      await expect(main.getByRole("heading", { name: "Owners" })).toBeVisible();
    })();

    await step("Switch to Users tab & verify user list renders")(async () => {
      const main = page.getByRole("main");

      await main.getByRole("tab", { name: "Users" }).click();

      const usersPanel = main.getByRole("tabpanel", { name: "Users" });
      await expect(usersPanel).toBeVisible();
      await expect(usersPanel.getByRole("row").nth(1)).toBeVisible();
    })();

    await backOfficeContext.close();
  });
});

test.describe("@smoke", () => {
  // Covers dashboard KPI cards, trends section, global users search, and user detail page.
  test("should render dashboard KPI cards, trends section, and navigate users list to user detail", async ({
    ownerPage,
    browser
  }) => {
    createTestContext(ownerPage);

    const backOfficeContext = await browser.newContext({ baseURL: BACK_OFFICE_BASE_URL, ignoreHTTPSErrors: true });
    const page = await backOfficeContext.newPage();
    createTestContext(page);

    await step("Log in as Admin via MockEasyAuth & verify redirect to dashboard")(async () => {
      await page.goto(`${BACK_OFFICE_BASE_URL}/`);

      await expect(page.getByRole("radio", { name: "Admin Log in with admin rights" })).toBeVisible();
      await page.getByRole("radio", { name: "Admin Log in with admin rights" }).click();
      await page.getByRole("button", { name: "Log in" }).click();

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/`);
    })();

    // === DASHBOARD ===

    await step("Navigate to dashboard & verify KPI tiles, chart cards, list cards, and period toggle are visible")(
      async () => {
        await page.goto(`${BACK_OFFICE_BASE_URL}/`);

        await expect(page.getByRole("heading", { name: "Dashboard" })).toBeVisible();

        await expect(page.getByText("Total accounts")).toBeVisible();
        await expect(page.getByText("Blended MRR")).toBeVisible();
        await expect(page.getByText("Users active")).toBeVisible();
        await expect(page.getByText("Active sessions")).toBeVisible();

        await expect(page.getByText("MRR trend", { exact: true })).toBeVisible();
        await expect(page.getByText("Plan distribution", { exact: true })).toBeVisible();
        await expect(page.getByText("Account growth", { exact: true })).toBeVisible();
        await expect(page.getByText("User logins / day", { exact: true })).toBeVisible();
        await expect(page.getByText("Recent signups", { exact: true })).toBeVisible();
        await expect(page.getByText("Recent billing events", { exact: true })).toBeVisible();

        const periodGroup = page.getByRole("group", { name: "Period" });
        await expect(periodGroup.getByRole("button", { name: "7d" })).toBeVisible();
        await expect(periodGroup.getByRole("button", { name: "30d" })).toBeVisible();
        await expect(periodGroup.getByRole("button", { name: "90d" })).toBeVisible();
      }
    )();

    await step("Switch period to 7d & verify 7d button reflects pressed state")(async () => {
      const periodGroup = page.getByRole("group", { name: "Period" });
      const sevenDaysButton = periodGroup.getByRole("button", { name: "7d" });

      await sevenDaysButton.click();

      await expect(sevenDaysButton).toHaveAttribute("aria-pressed", "true");
    })();

    // === USERS LIST ===

    await step("Navigate to users list & verify table renders with users by default")(async () => {
      await page.goto(`${BACK_OFFICE_BASE_URL}/users`);

      await expect(page.getByRole("table", { name: "Users" })).toBeVisible();
      await expect(page.getByRole("searchbox", { name: "Search" })).toBeVisible();
    })();

    await step("Search for e2e owner first name & verify URL filters to TestOwner row")(async () => {
      await page.getByRole("searchbox", { name: "Search" }).fill("testowner");

      await expect(page).toHaveURL(`${BACK_OFFICE_BASE_URL}/users?search=testowner`);
      await expect(page.getByRole("table", { name: "Users" })).toBeVisible();
      await expect(page.getByRole("columnheader", { name: "User" })).toBeVisible();
      await expect(page.getByRole("row").filter({ hasText: "TestOwner" }).first()).toBeVisible();
    })();

    await step("Click TestOwner row & verify navigation to user detail page with display name and tabs")(async () => {
      await page.getByRole("row").filter({ hasText: "TestOwner" }).first().click();

      await expect(page.getByRole("heading", { level: 1 })).toContainText("TestOwner");
      await expect(page.getByText("Last log-in")).toBeVisible();
      await expect(page.getByRole("tab", { name: "Accounts" })).toBeVisible();
      await expect(page.getByRole("tab", { name: "Logins" })).toBeVisible();
      await expect(page.getByRole("tab", { name: "Sessions" })).toBeVisible();
    })();

    await backOfficeContext.close();
  });
});

import { type Browser, type BrowserContext, type Page, test as base } from "@playwright/test";
import { createAuthStateManager } from "../auth/auth-state-manager";
import type { Tenant, UserRole } from "../types/auth";
import { getVerificationCode } from "../utils/test-data";
import { getSelfContainedSystemPrefix, getWorkerTenant } from "./worker-auth";

// Extend the global interface to include testTenant
declare global {
  interface Window {
    testTenant: Tenant;
  }
}

/**
 * Role-specific page fixtures for authenticated testing
 */
export interface PageAuthFixtures {
  /**
   * Authenticated page instance as tenant owner
   */
  ownerPage: Page;

  /**
   * Authenticated page instance as tenant admin
   */
  adminPage: Page;

  /**
   * Authenticated page instance as tenant member
   */
  memberPage: Page;
}

/**
 * Perform fresh authentication by going through signup/login flow
 */
async function performFreshAuthentication(
  context: BrowserContext,
  role: UserRole,
  tenant: Tenant | undefined,
  authManager: ReturnType<typeof createAuthStateManager>
): Promise<Page> {
  if (!tenant) {
    throw new Error("Tenant data is required for fresh authentication");
  }

  // Create a new page for authentication
  const page = await context.newPage();

  // Get the email for this role
  const email = getEmailForRole(tenant, role);

  // Perform signup flow to create the user
  await page.goto("/");
  await page.getByRole("button", { name: "Get started today" }).first().click();
  await page.getByRole("textbox", { name: "Email" }).fill(email);
  await page.getByRole("button", { name: "Create your account" }).click();
  await page.waitForURL("/signup/verify");

  // Enter verification code
  await page.keyboard.type(getVerificationCode());
  await page.getByRole("button", { name: "Verify" }).click();
  await page.waitForURL("/admin");

  // Complete profile setup
  const firstName = `Test${role}`;
  const lastName = `Worker${authManager.getStateFilePath(role).match(/worker-(\d+)/)?.[1] || "0"}`;

  await page.getByRole("textbox", { name: "First name" }).fill(firstName);
  await page.getByRole("textbox", { name: "Last name" }).fill(lastName);
  await page.getByRole("button", { name: "Save changes" }).click();

  // Wait for successful login and ensure we're on the welcome page
  // Also ensure any profile dialog is closed
  await page.waitForSelector('h1:has-text("Welcome home")', { state: "visible" });

  // Ensure any modal dialogs are closed by waiting for them to disappear
  try {
    await page.waitForSelector('[role="dialog"]', { state: "detached", timeout: 2000 });
  } catch {
    // Dialog might not exist or already be closed, which is fine
  }

  // Save authentication state
  await authManager.saveAuthState(page, role);

  return page;
}

/**
 * Get email for a specific role from tenant data
 */
function getEmailForRole(tenant: Tenant, role: UserRole): string {
  switch (role) {
    case "Owner":
      return tenant.ownerEmail;
    case "Admin":
      return tenant.adminEmail;
    case "Member":
      return tenant.memberEmail;
    default:
      throw new Error(`Unknown role: ${role}`);
  }
}

/**
 * Create an authenticated context and page for a specific user role
 */
async function createAuthenticatedContextAndPage(
  browser: Browser,
  role: UserRole,
  workerIndex: number,
  selfContainedSystemPrefix?: string,
  tenant?: Tenant
): Promise<{ context: BrowserContext; page: Page }> {
  const authManager = createAuthStateManager(workerIndex, selfContainedSystemPrefix);

  // Check if we have valid auth state
  const hasValidAuth = await authManager.hasValidAuthState(role);

  let context: BrowserContext;
  let page: Page;

  if (hasValidAuth) {
    // Create context with existing auth state
    context = await browser.newContext({
      storageState: authManager.getStateFilePath(role)
    });
    page = await context.newPage();

    // Validate that authentication is still working
    const isStillValid = await authManager.validateAuthState(page);
    if (!isStillValid) {
      // Clear invalid auth state and create fresh session
      await authManager.clearAuthState(role);
      await context.close();

      // Create fresh context and perform authentication
      context = await browser.newContext();
      page = await performFreshAuthentication(context, role, tenant, authManager);
    }
  } else {
    // Create fresh context and perform authentication
    context = await browser.newContext();
    page = await performFreshAuthentication(context, role, tenant, authManager);
  }

  return { context, page };
}

/**
 * Extended test with role-specific authenticated page fixtures
 */
export const test = base.extend<PageAuthFixtures>({
  ownerPage: async ({ browser }, use, testInfo) => {
    const workerIndex = testInfo.parallelIndex;
    const systemPrefix = getSelfContainedSystemPrefix();

    // Get tenant for this worker
    const tenant = await getWorkerTenant(workerIndex, systemPrefix);

    // Create authenticated context and page for owner
    const { context, page } = await createAuthenticatedContextAndPage(
      browser,
      "Owner",
      workerIndex,
      systemPrefix,
      tenant
    );

    await use(page);

    // Cleanup - close the context and page
    await context.close();
  },

  adminPage: async ({ browser }, use, testInfo) => {
    const workerIndex = testInfo.parallelIndex;
    const systemPrefix = getSelfContainedSystemPrefix();

    // Get tenant for this worker
    const tenant = await getWorkerTenant(workerIndex, systemPrefix);

    // Create authenticated context and page for admin
    const { context, page } = await createAuthenticatedContextAndPage(
      browser,
      "Admin",
      workerIndex,
      systemPrefix,
      tenant
    );

    await use(page);

    // Cleanup - close the context and page
    await context.close();
  },

  memberPage: async ({ browser }, use, testInfo) => {
    const workerIndex = testInfo.parallelIndex;
    const systemPrefix = getSelfContainedSystemPrefix();

    // Get tenant for this worker
    const tenant = await getWorkerTenant(workerIndex, systemPrefix);

    // Create authenticated context and page for member
    const { context, page } = await createAuthenticatedContextAndPage(
      browser,
      "Member",
      workerIndex,
      systemPrefix,
      tenant
    );

    await use(page);

    // Cleanup - close the context and page
    await context.close();
  }
});

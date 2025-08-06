import { type Browser, type BrowserContext, type Page, test as base, expect } from "@playwright/test";
import { createAuthStateManager } from "@shared/e2e/auth/auth-state-manager";
import { getSelfContainedSystemPrefix, getWorkerTenant } from "@shared/e2e/fixtures/worker-auth";
import type { Tenant, User, UserRole } from "@shared/e2e/types/auth";
import { completeSignupFlow } from "@shared/e2e/utils/test-data";
import { createTestContext, assertNoUnexpectedErrors, type TestContext } from "@shared/e2e/utils/test-assertions";


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

  /**
   * Anonymous (unauthenticated) page with tenant provisioned
   * Useful for testing login/signup flows from a clean state while ensuring users exist
   */
  anonymousPage: { page: Page; tenant: Tenant };
}

/**
 * Perform fresh authentication by going through signup/login flow
 */
async function performFreshAuthentication(
  browserContext: BrowserContext,
  role: UserRole,
  tenant: Tenant | undefined,
  authManager: ReturnType<typeof createAuthStateManager>
): Promise<Page> {
  if (!tenant) {
    throw new Error("Tenant data is required for fresh authentication");
  }

  // Create a new page for authentication
  const page = await browserContext.newPage();

  // Get the user for this role
  const user = getUserForRole(tenant, role);

  // Use the centralized signup flow utility
  const testContext = createTestContext(page);
  await completeSignupFlow(page, expect, user, testContext);

  // Set account name for Owner role to allow user invitations
  if (role === "Owner") {
    await page.goto("/admin/account");
    await expect(page.getByRole("heading", { name: "Account settings" })).toBeVisible();
    await page.getByRole("textbox", { name: "Account name" }).fill("Test Organization");
    await page.getByRole("button", { name: "Save" }).click();
    
    const { expectToastMessage } = await import("../utils/test-assertions.js");
    await expectToastMessage(testContext, "Account name updated successfully");
  }

  // Ensure any modal dialogs are closed by waiting for them to disappear
  try {
    await page.locator('[role="dialog"]').waitFor({ state: "detached", timeout: 2000 });
  } catch {
    // Dialog might not exist or already be closed, which is fine
  }

  // Save authentication state
  await authManager.saveAuthState(page, role);

  return page;
}

/**
 * Get user for a specific role from tenant data
 */
function getUserForRole(tenant: Tenant, role: UserRole): User {
  switch (role) {
    case "Owner":
      return tenant.owner;
    case "Admin":
      return tenant.admin;
    case "Member":
      return tenant.member;
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
  },

  anonymousPage: async ({ browser }, use, testInfo) => {
    const workerIndex = testInfo.parallelIndex;
    const systemPrefix = getSelfContainedSystemPrefix();

    // Get tenant for this worker - ensure users exist for testing existing user flows
    const tenant = await getWorkerTenant(workerIndex, systemPrefix, {
      workerIndex,
      selfContainedSystemPrefix: systemPrefix,
      ensureUsersExist: true
    });

    // Create a fresh, unauthenticated context and page
    const context = await browser.newContext();
    const page = await context.newPage();

    await use({ page, tenant });

    // Cleanup - close the context and page
    await context.close();
  }
});

// Global afterEach hook to automatically run error checking for ALL tests
base.afterEach(({ page }) => {
  if (page) {
    // Retrieve the existing context that was created during the test
    const existingContext = (page as Page & { __testContext?: TestContext }).__testContext;
    if (existingContext) {
      assertNoUnexpectedErrors(existingContext);
    }
  }
});

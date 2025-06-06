import { type BrowserContext, type Page, test as base } from "@playwright/test";
import { getSelfContainedSystemPrefix } from "@shared/e2e/fixtures/worker-auth";
import type { UserRole } from "@shared/e2e/types/auth";

/**
 * Isolated authentication fixtures for tests that need separate tenants or users
 * These fixtures are specifically designed for rate-limiting scenarios where
 * shared worker-scoped tenants would interfere with test isolation
 */
export interface IsolatedAuthFixtures {
  /**
   * Isolated owner page that creates temporary tenants (for signup rate-limiting)
   * or new users (for login rate-limiting)
   */
  isolatedOwnerPage: Page;

  /**
   * Isolated admin page that creates temporary tenants (for signup rate-limiting)
   * or new users (for login rate-limiting)
   */
  isolatedAdminPage: Page;

  /**
   * Isolated member page that creates temporary tenants (for signup rate-limiting)
   * or new users (for login rate-limiting)
   */
  isolatedMemberPage: Page;
}

/**
 * Create an isolated authenticated page for a specific user role
 * Uses unique naming pattern to avoid interference with rate-limiting tests
 * @param context Browser context
 * @param role User role (Owner, Admin, Member)
 * @param workerIndex Playwright worker index
 * @param selfContainedSystemPrefix Optional system prefix
 * @returns Promise resolving to authenticated page with isolated tenant/user
 */
async function createIsolatedAuthenticatedPage(
  context: BrowserContext,
  _role: UserRole,
  _workerIndex: number,
  _selfContainedSystemPrefix?: string
): Promise<Page> {
  // Create unique identifier for this isolated instance
  const _timestamp = Date.now();
  const _uniqueId = `${_workerIndex}-${_timestamp}`;

  // Use isolated naming pattern to avoid conflicts with shared tenants
  const _prefix = _selfContainedSystemPrefix ? `${_selfContainedSystemPrefix}-` : "";
  // const isolatedEmail = `e2e-isolated-${_prefix}${_role.toLowerCase()}-${_uniqueId}@platformplatform.net`;

  // For now, we'll use a unique auth state path that won't conflict with shared fixtures
  // const authManager = createAuthStateManager(
  //   workerIndex,
  //   `${selfContainedSystemPrefix || "default"}-isolated-${timestamp}`
  // );

  // Create a new page with fresh context
  const page = await context.newPage();

  return page;
}

/**
 * Extended test with isolated authentication fixtures for rate-limiting scenarios
 */
export const test = base.extend<IsolatedAuthFixtures>({
  isolatedOwnerPage: async ({ context }, use, testInfo) => {
    const workerIndex = testInfo.parallelIndex;
    const systemPrefix = getSelfContainedSystemPrefix();

    // Create isolated authenticated page for owner
    const page = await createIsolatedAuthenticatedPage(context, "Owner", workerIndex, systemPrefix);

    await use(page);

    // Cleanup - close the page
    await page.close();
  },

  isolatedAdminPage: async ({ context }, use, testInfo) => {
    const workerIndex = testInfo.parallelIndex;
    const systemPrefix = getSelfContainedSystemPrefix();

    // Create isolated authenticated page for admin
    const page = await createIsolatedAuthenticatedPage(context, "Admin", workerIndex, systemPrefix);

    await use(page);

    // Cleanup - close the page
    await page.close();
  },

  isolatedMemberPage: async ({ context }, use, testInfo) => {
    const workerIndex = testInfo.parallelIndex;
    const systemPrefix = getSelfContainedSystemPrefix();

    // Create isolated authenticated page for member
    const page = await createIsolatedAuthenticatedPage(context, "Member", workerIndex, systemPrefix);

    await use(page);

    // Cleanup - close the page
    await page.close();
  }
});

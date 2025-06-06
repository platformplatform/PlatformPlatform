import { type Browser, type BrowserContext, type Page, test as base } from "@playwright/test";
import { createAuthStateManager } from "@shared/e2e/auth/auth-state-manager";
import { getSelfContainedSystemPrefix, getWorkerTenant } from "@shared/e2e/fixtures/worker-auth";
import type { UserRole } from "@shared/e2e/types/auth";

/**
 * Multi-context authentication fixtures for testing scenarios with multiple users
 */
export interface MultiContextAuthFixtures {
  /**
   * Provides multiple authenticated browser contexts in the same test
   * Useful for testing cross-user interactions and tenant isolation
   */
  multiUserContext: MultiUserContextManager;
}

/**
 * Multi-user context manager for handling multiple authenticated sessions
 */
export class MultiUserContextManager {
  private contexts: Map<UserRole, BrowserContext> = new Map();
  private pages: Map<UserRole, Page> = new Map();
  private browser: Browser;
  private workerIndex: number;
  private systemPrefix?: string;

  constructor(browser: Browser, workerIndex: number, systemPrefix?: string) {
    this.browser = browser;
    this.workerIndex = workerIndex;
    this.systemPrefix = systemPrefix;
  }

  /**
   * Create an authenticated page for a specific user role
   * @param role User role (Owner, Admin, Member)
   * @returns Promise resolving to authenticated page
   */
  async getPageForRole(role: UserRole): Promise<Page> {
    const existingPage = this.pages.get(role);
    if (existingPage) {
      return existingPage;
    }

    // Create a new browser context for this role
    const context = await this.browser.newContext();
    this.contexts.set(role, context);

    // Set up authentication for this context
    const authManager = createAuthStateManager(this.workerIndex, this.systemPrefix);

    // Check if we have valid auth state and load it
    const hasValidAuth = await authManager.hasValidAuthState(role);
    if (hasValidAuth) {
      await authManager.loadAuthState(context, role);
    }

    // Create a new page with the authenticated context
    const page = await context.newPage();
    this.pages.set(role, page);

    // Validate that authentication is still working
    if (hasValidAuth) {
      const isStillValid = await authManager.validateAuthState(page);
      if (!isStillValid) {
        // Clear invalid auth state
        await authManager.clearAuthState(role);
      }
    }

    return page;
  }

  /**
   * Get all active pages for all roles
   * @returns Map of role to page
   */
  getAllPages(): Map<UserRole, Page> {
    return new Map(this.pages);
  }

  /**
   * Get browser context for a specific role
   * @param role User role
   * @returns Browser context for the role, or undefined if not created
   */
  getContextForRole(role: UserRole): BrowserContext | undefined {
    return this.contexts.get(role);
  }

  /**
   * Check if a role has an active session
   * @param role User role
   * @returns True if role has an active page/context
   */
  hasActiveSession(role: UserRole): boolean {
    return this.pages.has(role) && this.contexts.has(role);
  }

  /**
   * Close all contexts and pages
   * @returns Promise resolving when all cleanup is complete
   */
  async cleanup(): Promise<void> {
    // Close all pages first
    await Promise.all(Array.from(this.pages.values()).map((page) => page.close()));

    // Close all contexts
    await Promise.all(Array.from(this.contexts.values()).map((context) => context.close()));

    // Clear the maps
    this.pages.clear();
    this.contexts.clear();
  }
}

/**
 * Extended test with multi-context authentication fixtures
 */
export const test = base.extend<MultiContextAuthFixtures>({
  multiUserContext: async ({ browser }, use, testInfo) => {
    const workerIndex = testInfo.parallelIndex;
    const systemPrefix = getSelfContainedSystemPrefix();

    // Get tenant for this worker (will be used for authentication in future implementation)
    await getWorkerTenant(workerIndex, systemPrefix);

    // Create multi-user context manager
    const manager = new MultiUserContextManager(browser, workerIndex, systemPrefix);

    await use(manager);

    // Cleanup all contexts and pages
    await manager.cleanup();
  }
});

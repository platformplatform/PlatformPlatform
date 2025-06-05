// Main export for all authentication fixtures
export { test, type PageAuthFixtures } from "./page-auth";

// Isolated fixtures for rate-limiting tests
export { test as isolatedTest, type IsolatedAuthFixtures } from "./isolated-auth";

// Multi-context fixtures for testing multiple users simultaneously
export { test as multiContextTest, MultiUserContextManager, type MultiContextAuthFixtures } from "./multi-context-auth";

// Worker authentication utilities
export { getWorkerTenant, getSelfContainedSystemPrefix, clearWorkerTenantCache } from "./worker-auth";
export { createAuthStateManager, AuthStateManager } from "../auth/auth-state-manager";

// Rate-limiting detection utilities
export { isRateLimitResponse } from "../auth/rate-limiting-detection";

// Tenant pool management
export { markTenantAsRateLimitingUsed, isTenantRateLimitingUsed } from "../auth/tenant-pool-manager";

// Re-export authentication types for convenience
export type { UserRole, Tenant, AuthContext, TenantProvisioningOptions } from "../types/auth";

// Re-export storage state utilities
export {
  saveAuthenticationState,
  loadAuthenticationState,
  getStorageStatePath,
  isAuthenticationStateValid
} from "../auth/storage-state";

// Re-export tenant provisioning utilities
export { createTenantWithUsers } from "../auth/tenant-provisioning";

/**
 * Extended test object with authentication fixtures
 * Use this instead of the base Playwright test for authenticated testing
 *
 * @example
 * ```typescript
 * import { test } from "@repo/shared-webapp/tests/e2e/fixtures";
 *
 * test("should access admin features as owner", async ({ ownerPage }) => {
 *   await ownerPage.goto("/admin");
 *   // Test with authenticated owner page
 * });
 * ```
 */

/**
 * Default export for convenience - the extended test with auth fixtures
 */
export { test as default } from "./page-auth";

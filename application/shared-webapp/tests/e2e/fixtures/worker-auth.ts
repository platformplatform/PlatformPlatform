import { getStorageStatePath, isAuthenticationStateValid } from "../auth/storage-state";
import { createTenantWithUsers } from "../auth/tenant-provisioning";
import type { Tenant } from "../types/auth";

/**
 * Worker-scoped tenant cache to ensure each worker gets a unique tenant
 */
const workerTenantCache = new Map<string, Tenant>();

/**
 * Get or create a tenant for the current worker
 * This ensures each Playwright worker gets a unique tenant for parallel execution
 * @param workerIndex Playwright worker index from testInfo.parallelIndex
 * @param selfContainedSystemPrefix Optional prefix for system separation
 * @returns Promise resolving to a unique tenant for this worker
 */
export async function getWorkerTenant(workerIndex: number, selfContainedSystemPrefix?: string): Promise<Tenant> {
  const cacheKey = `${workerIndex}-${selfContainedSystemPrefix || "default"}`;

  // Return cached tenant if available
  if (workerTenantCache.has(cacheKey)) {
    const cachedTenant = workerTenantCache.get(cacheKey);
    if (cachedTenant) {
      return cachedTenant;
    }
  }

  // Check if we have valid authentication state for the owner (primary user)
  const ownerStorageStatePath = getStorageStatePath(workerIndex, "owner", selfContainedSystemPrefix);
  const hasValidAuth = await isAuthenticationStateValid(ownerStorageStatePath);

  let tenant: Tenant;

  if (hasValidAuth) {
    // Reuse existing tenant - reconstruct from storage path pattern
    tenant = createTenantWithUsers(workerIndex, selfContainedSystemPrefix);
  } else {
    // Create new tenant with all users
    tenant = createTenantWithUsers(workerIndex, selfContainedSystemPrefix);

    // Actual tenant creation will be implemented when authentication is needed
  }

  // Cache the tenant for this worker
  workerTenantCache.set(cacheKey, tenant);
  return tenant;
}

/**
 * Extract the self-contained system prefix from the current working directory or test context
 * @returns The self-contained system prefix (e.g., "account-management" or "back-office")
 */
export function getSelfContainedSystemPrefix(): string | undefined {
  // Try to extract from current working directory
  const cwd = process.cwd();
  const match = cwd.match(/application\/([^\/]+)\/WebApp/);
  return match ? match[1] : undefined;
}


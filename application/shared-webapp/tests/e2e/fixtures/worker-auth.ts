import { createTenantWithUsers, ensureTenantUsersExist } from "@shared/e2e/auth/tenant-provisioning";
import type { Tenant, TenantProvisioningOptions } from "@shared/e2e/types/auth";

/**
 * Worker-scoped tenant cache to ensure each worker gets a unique tenant
 */
const workerTenantCache = new Map<string, Tenant>();

/**
 * Get or create a tenant for the current worker
 * This ensures each Playwright worker gets a unique tenant for parallel execution
 * @param workerIndex Playwright worker index from testInfo.parallelIndex
 * @param selfContainedSystemPrefix Optional prefix for system separation
 * @param options Optional provisioning options
 * @returns Promise resolving to a unique tenant for this worker
 */
export async function getWorkerTenant(
  workerIndex: number,
  selfContainedSystemPrefix?: string,
  options?: TenantProvisioningOptions
): Promise<Tenant> {
  const cacheKey = `${workerIndex}-${selfContainedSystemPrefix || "default"}`;

  // Return cached tenant if available
  if (workerTenantCache.has(cacheKey)) {
    const cachedTenant = workerTenantCache.get(cacheKey);
    if (cachedTenant) {
      // If we need to ensure users exist, do that now
      if (options?.ensureUsersExist) {
        await ensureTenantUsersExist(cachedTenant);
      }
      return cachedTenant;
    }
  }

  // Always create the tenant object structure
  const tenant = createTenantWithUsers(workerIndex, selfContainedSystemPrefix);

  if (options?.ensureUsersExist) {
    await ensureTenantUsersExist(tenant);
  }

  // Cache the tenant for this worker
  workerTenantCache.set(cacheKey, tenant);
  return tenant;
}

/**
 * Extract the self-contained system prefix from the current working directory or test context
 * @returns The self-contained system prefix (e.g., "account" or "back-office")
 */
export function getSelfContainedSystemPrefix(): string | undefined {
  // Try to extract from current working directory
  const cwd = process.cwd();
  const match = cwd.match(/application\/([^/]+)\/WebApp/);
  return match ? match[1] : undefined;
}

import { expect } from "@playwright/test";
import type { Tenant, User } from "../types/auth";

/**
 * Create a tenant with owner, admin, and member users
 * @param workerIndex Playwright worker index for unique tenant identification
 * @param selfContainedSystemPrefix Optional prefix to separate tenant pools between systems
 * @returns Tenant object with user information
 */
export function createTenantWithUsers(workerIndex: number, selfContainedSystemPrefix?: string): Tenant {
  const prefix = selfContainedSystemPrefix ? `${selfContainedSystemPrefix}-` : "";
  const tenantName = `${prefix}e2e-tenant-${workerIndex}`;

  // Generate unique emails for each role
  const ownerEmail = `e2e-${prefix}owner-${workerIndex}@platformplatform.net`;
  const adminEmail = `e2e-${prefix}admin-${workerIndex}@platformplatform.net`;
  const memberEmail = `e2e-${prefix}member-${workerIndex}@platformplatform.net`;

  // Return tenant structure - actual signup will be implemented when needed
  const tenantId = `tenant-${workerIndex}`;

  return {
    tenantId,
    tenantName,
    owner,
    admin,
    member
  };
}

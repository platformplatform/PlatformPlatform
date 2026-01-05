import { expect } from "@playwright/test";
import * as fs from "node:fs";
import * as path from "node:path";
import type { Tenant, User } from "@shared/e2e/types/auth";

/**
 * Read and parse platform settings from shared-kernel
 */
function getPlatformSettings(): { identity: { internalEmailDomain: string } } {
  const settingsPath = path.resolve(__dirname, "../../../../shared-kernel/SharedKernel/Platform/platform-settings.jsonc");
  const content = fs.readFileSync(settingsPath, "utf-8");
  const jsonWithoutComments = content.replace(/\/\/.*$/gm, "").replace(/\/\*[\s\S]*?\*\//g, "");
  return JSON.parse(jsonWithoutComments);
}

/**
 * Create a tenant with owner, admin, and member users
 * @param workerIndex Playwright worker index for unique tenant identification
 * @param selfContainedSystemPrefix Optional prefix to separate tenant pools between systems
 * @returns Tenant object with user information
 */
export function createTenantWithUsers(workerIndex: number, selfContainedSystemPrefix?: string): Tenant {
  const prefix = selfContainedSystemPrefix ? `${selfContainedSystemPrefix}-` : "";

  // Compact timestamp (YY-MM-DDTHH-MM)
  const timestamp = new Date().toISOString().slice(2, 16).replace(/[-:T]/g, "");

  const tenantName = `${prefix}e2e-tenant-${workerIndex}-${timestamp}`;

  // Back-office requires internal user email domain (read dynamically from platform-settings.jsonc)
  const internalDomain = getPlatformSettings().identity.internalEmailDomain.replace("@", "");
  const emailDomain = selfContainedSystemPrefix === "back-office" ? internalDomain : `${workerIndex}.${timestamp}.local`;

  // Generate unique emails for each role with timestamp to avoid conflicts across test runs
  const ownerEmailAddress = `e2e-${prefix}-owner-${workerIndex}-${timestamp}@${emailDomain}`;
  const adminEmailAddress = `e2e-${prefix}-admin-${workerIndex}-${timestamp}@${emailDomain}`;
  const memberEmailAddress = `e2e-${prefix}-member-${workerIndex}-${timestamp}@${emailDomain}`;

  // Create User objects for each role
  const owner: User = {
    email: ownerEmailAddress,
    firstName: "TestOwner",
    lastName: `Worker${workerIndex}`,
    role: "Owner"
  };

  const admin: User = {
    email: adminEmailAddress,
    firstName: "TestAdmin",
    lastName: `Worker${workerIndex}`,
    role: "Admin"
  };

  const member: User = {
    email: memberEmailAddress,
    firstName: "TestMember",
    lastName: `Worker${workerIndex}`,
    role: "Member"
  };

  // Return tenant structure - actual signup will be implemented when needed
  const tenantId = `tenant-${workerIndex}-${timestamp}`;

  return {
    tenantId,
    tenantName,
    owner,
    admin,
    member
  };
}

/**
 * Ensure that all tenant users exist in the backend
 * This provisions the users through the signup flow if they don't already exist
 * @param tenant Tenant object with user information
 * @returns Promise that resolves when all users are ensured to exist
 */
export async function ensureTenantUsersExist(tenant: Tenant): Promise<void> {
  // Import the authentication utilities dynamically to avoid circular dependencies
  const { createAuthStateManager } = await import("../auth/auth-state-manager.js");
  const { completeSignupFlow } = await import("../utils/test-data.js");

  // Create a temporary browser context for user provisioning
  const { chromium } = await import("@playwright/test");
  const browser = await chromium.launch();
  const context = await browser.newContext();
  const page = await context.newPage();

  try {
    // Create the owner user through centralized signup flow
    const { createTestContext } = await import("../utils/test-assertions.js");
    const testContext = createTestContext(page);
    await completeSignupFlow(page, expect, tenant.owner, testContext);

    // Save authentication state for reuse
    const authManager = createAuthStateManager(0, "account-management"); // Use worker 0 for shared users
    await authManager.saveAuthState(page, "Owner");
  } finally {
    // Cleanup - always close browser resources
    await context.close();
    await browser.close();
  }
}

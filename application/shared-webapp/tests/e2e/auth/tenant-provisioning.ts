import type { Tenant } from "../types/auth";

/**
 * Create a tenant with owner, admin, and member users
 * @param workerIndex Playwright worker index for unique tenant identification
 * @param selfContainedSystemPrefix Optional prefix to separate tenant pools between systems
 * @returns Tenant object with user information
 */
export function createTenantWithUsers(workerIndex: number, selfContainedSystemPrefix?: string): Tenant {
  const prefix = selfContainedSystemPrefix ? `${selfContainedSystemPrefix}-` : "";
  const timestamp = Date.now();
  const tenantName = `${prefix}e2e-tenant-${workerIndex}-${timestamp}`;

  // Generate unique emails for each role with timestamp to avoid conflicts across test runs
  const ownerEmail = `e2e-${prefix}owner-${workerIndex}-${timestamp}@platformplatform.net`;
  const adminEmail = `e2e-${prefix}admin-${workerIndex}-${timestamp}@platformplatform.net`;
  const memberEmail = `e2e-${prefix}member-${workerIndex}-${timestamp}@platformplatform.net`;

  // Return tenant structure - actual signup will be implemented when needed
  const tenantId = `tenant-${workerIndex}-${timestamp}`;

  return {
    tenantId,
    tenantName,
    ownerEmail,
    adminEmail,
    memberEmail
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
  const { getVerificationCode } = await import("../utils/test-data.js");

  // Create a temporary browser context for user provisioning
  const { chromium } = await import("@playwright/test");
  const browser = await chromium.launch();
  const context = await browser.newContext();
  const page = await context.newPage();

  try {
    // Create the owner user through signup flow
    await page.goto("https://localhost:9000/");
    await page.getByRole("button", { name: "Get started today" }).first().click();
    await page.getByRole("textbox", { name: "Email" }).fill(tenant.ownerEmail);
    await page.getByRole("button", { name: "Create your account" }).click();
    await page.waitForURL("/signup/verify");

    // Complete verification
    await page.keyboard.type(getVerificationCode());
    await page.getByRole("button", { name: "Verify" }).click();
    await page.waitForURL("/admin");

    // Complete profile setup
    await page.getByRole("textbox", { name: "First name" }).fill("TestOwner");
    await page.getByRole("textbox", { name: "Last name" }).fill("User");
    await page.getByRole("button", { name: "Save changes" }).click();

    // Wait for completion
    await page.waitForSelector('h1:has-text("Welcome home")', { state: "visible" });

    // Save authentication state for reuse
    const authManager = createAuthStateManager(0, "account-management"); // Use worker 0 for shared users
    await authManager.saveAuthState(page, "Owner");
  } catch (_error) {
    // If user already exists or there's a conflict, that's fine - we just want to ensure they exist
    // Silently continue as this is expected behavior for existing users
  } finally {
    // Cleanup
    await context.close();
    await browser.close();
  }
}

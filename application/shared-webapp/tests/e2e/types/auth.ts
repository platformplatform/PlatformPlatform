import type { BrowserContext } from "@playwright/test";

/**
 * Authentication types and interfaces for E2E testing
 */

/**
 * User roles available in the system - matching UserInfoEnv from environment.d.ts
 */
export type UserRole = "Owner" | "Admin" | "Member";

/**
 * User interface containing all user information for E2E testing
 */
export interface User {
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
}

/**
 * Tenant interface containing all user information for E2E testing
 */
export interface Tenant {
  tenantId: string;
  tenantName: string;
  owner: User;
  admin: User;
  member: User;
}

/**
 * Authentication context for a specific user session
 */
export interface AuthContext {
  userRole: UserRole;
  email: string;
  storageStatePath: string;
}

/**
 * Configuration options for tenant provisioning
 */
export interface TenantProvisioningOptions {
  workerIndex: number;
  selfContainedSystemPrefix?: string;
  isolated?: boolean;
}

/**
 * Storage state file information
 */
export interface StorageStateInfo {
  filePath: string;
  isValid: boolean;
  lastModified?: Date;
}

/**
 * Multi-user context for testing multiple authenticated sessions
 */
export interface MultiUserSession {
  userRole: UserRole;
  email: string;
  context: BrowserContext;
}

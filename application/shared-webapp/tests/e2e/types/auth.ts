
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
 * Configuration options for tenant provisioning
 */
export interface TenantProvisioningOptions {
  workerIndex: number;
  selfContainedSystemPrefix?: string;
  isolated?: boolean;
}



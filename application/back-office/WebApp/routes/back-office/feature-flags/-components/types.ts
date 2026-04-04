export type FeatureFlagScope = "System" | "Tenant" | "User";

export interface FeatureFlagInfo {
  key: string;
  scope: FeatureFlagScope;
  adminLevel: string;
  description: string;
  isAbTestEligible: boolean;
  configurableByTenant: boolean;
  configurableByUser: boolean;
  enabledAt: string | null;
  disabledAt: string | null;
  bucketStart: number | null;
  bucketEnd: number | null;
  rolloutPercentage: number | null;
  isActive: boolean;
}

export interface GetFeatureFlagsResponse {
  flags: FeatureFlagInfo[];
}

export interface FlagTenantInfo {
  tenantId: string;
  tenantName: string;
  isEnabled: boolean;
  source: "manual_override" | "ab_rollout" | "default";
}

export interface GetFlagTenantsResponse {
  tenants: FlagTenantInfo[];
}

export interface FlagUserInfo {
  userId: string;
  email: string;
  tenantName: string;
  isEnabled: boolean;
  source: "manual_override" | "default";
}

export interface GetFlagUsersResponse {
  users: FlagUserInfo[];
}

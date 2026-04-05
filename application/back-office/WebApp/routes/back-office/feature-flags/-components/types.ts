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
  createdAt: string | null;
  requiredPlan: string | null;
}

export interface GetFeatureFlagsResponse {
  flags: FeatureFlagInfo[];
}

export interface FeatureFlagTenantInfo {
  tenantId: string;
  tenantName: string;
  plan: string;
  isEnabled: boolean;
  source: "manual_override" | "ab_rollout" | "plan" | "default";
  rolloutBucket: number;
}

export interface GetFeatureFlagTenantsResponse {
  tenants: FeatureFlagTenantInfo[];
}

export interface FeatureFlagUserInfo {
  userId: string;
  tenantId: string;
  email: string;
  tenantName: string;
  isEnabled: boolean;
  source: "manual_override" | "ab_rollout" | "plan" | "default";
  rolloutBucket: number;
}

export interface GetFeatureFlagUsersResponse {
  users: FeatureFlagUserInfo[];
}

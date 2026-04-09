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
  rolloutBucketStart: number | null;
  rolloutBucketEnd: number | null;
  rolloutPercentage: number | null;
  isActive: boolean;
  createdAt: string | null;
  requiredSubscriptionPlan: string | null;
}

export interface GetFeatureFlagsResponse {
  flags: FeatureFlagInfo[];
}

export interface FeatureFlagTenantInfo {
  tenantId: string;
  tenantName: string;
  subscriptionPlan: string;
  isEnabled: boolean;
  source: "ManualOverride" | "AbRollout" | "Default";
  rolloutBucket: number | null;
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
  source: "ManualOverride" | "AbRollout" | "Default";
  rolloutBucket: number | null;
}

export interface GetFeatureFlagUsersResponse {
  users: FeatureFlagUserInfo[];
}

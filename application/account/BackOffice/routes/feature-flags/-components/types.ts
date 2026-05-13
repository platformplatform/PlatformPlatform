import type { components } from "@/shared/lib/api/client";

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
  orphanedAt: string | null;
  isKillSwitchEnabled: boolean;
}

export interface GetFeatureFlagsResponse {
  flags: FeatureFlagInfo[];
}

export type FeatureFlagTenantInfo = components["schemas"]["FeatureFlagTenantInfo"];

export type FeatureFlagUserInfo = components["schemas"]["FeatureFlagUserInfo"];

export type FeatureFlagSourceLiteral = "manual_override" | "ab_rollout" | "plan" | "default";

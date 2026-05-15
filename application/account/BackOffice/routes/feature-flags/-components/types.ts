import type { components } from "@/shared/lib/api/client";

export type FeatureFlagInfo = components["schemas"]["FeatureFlagInfo"];

export type FeatureFlagScope = components["schemas"]["FeatureFlagScope"];

export type GetFeatureFlagsResponse = components["schemas"]["GetFeatureFlagsResponse"];

export type FeatureFlagTenantInfo = components["schemas"]["FeatureFlagTenantInfo"];

export type FeatureFlagUserInfo = components["schemas"]["FeatureFlagUserInfo"];

export type FeatureFlagSourceLiteral = "manual_override" | "ab_rollout" | "plan" | "default";

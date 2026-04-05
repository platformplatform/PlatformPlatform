import { eq, useLiveQuery } from "@tanstack/react-db";

import type { FeatureFlagRow } from "../sync/types";

import { featureFlagCollection } from "../sync/collections";
import { getFlag } from "./registry";

type FeatureFlagResult = { enabled: boolean; isLoading: boolean };

const DISABLED: FeatureFlagResult = { enabled: false, isLoading: false };
const ENABLED: FeatureFlagResult = { enabled: true, isLoading: false };
const LOADING: FeatureFlagResult = { enabled: false, isLoading: true };

function isRowActive(row: FeatureFlagRow): boolean {
  if (row.enabledAt == null) return false;
  if (row.disabledAt == null) return true;
  return new Date(row.enabledAt) > new Date(row.disabledAt);
}

function isInRolloutBucketRange(rolloutBucket: number, rolloutBucketStart: number, rolloutBucketEnd: number): boolean {
  if (rolloutBucketStart <= rolloutBucketEnd) {
    return rolloutBucket >= rolloutBucketStart && rolloutBucket <= rolloutBucketEnd;
  }
  // Wrap-around range (e.g., 90-10 means 90-99 and 0-10)
  return rolloutBucket >= rolloutBucketStart || rolloutBucket <= rolloutBucketEnd;
}

export function useFeatureFlag(flagKey: string): FeatureFlagResult {
  const definition = getFlag(flagKey);
  const userInfo = import.meta.user_info_env;
  const isAuthenticated = userInfo.isAuthenticated;
  const isSystemFlag = definition?.scope === "system";

  // Query feature flag rows for database-scoped flags
  const { data: rows } = useLiveQuery(
    (q) =>
      definition && !isSystemFlag && isAuthenticated
        ? q
            .from({ featureFlags: featureFlagCollection })
            .where(({ featureFlags }) => eq(featureFlags.featureFlagKey, flagKey))
            .select(({ featureFlags }) => ({
              id: featureFlags.id,
              featureFlagKey: featureFlags.featureFlagKey,
              tenantId: featureFlags.tenantId,
              userId: featureFlags.userId,
              enabledAt: featureFlags.enabledAt,
              disabledAt: featureFlags.disabledAt,
              rolloutBucketStart: featureFlags.rolloutBucketStart,
              rolloutBucketEnd: featureFlags.rolloutBucketEnd,
              configurableByTenant: featureFlags.configurableByTenant,
              configurableByUser: featureFlags.configurableByUser
            }))
        : undefined,
    [flagKey, isSystemFlag, isAuthenticated]
  );

  // Unknown flag
  if (!definition) return DISABLED;

  // System-scoped flags: read from env vars
  if (definition.scope === "system") {
    const envVar = definition.envVar as keyof RuntimeEnv;
    return import.meta.runtime_env[envVar] === "true" ? ENABLED : DISABLED;
  }

  // Unauthenticated context: non-system flags are disabled
  if (!isAuthenticated) return DISABLED;

  // Electric sync not yet delivered
  if (rows === undefined) return LOADING;

  // Find base row (no tenant/user override)
  const baseRow = rows.find((r) => r.tenantId == null && r.userId == null);
  if (!baseRow || !isRowActive(baseRow)) return DISABLED;

  // Check for most specific override: user override first
  if (userInfo.id && userInfo.tenantId) {
    const userOverride = rows.find((r) => r.tenantId === userInfo.tenantId && r.userId === userInfo.id);
    if (userOverride) return isRowActive(userOverride) ? ENABLED : DISABLED;
  }

  // Then tenant override
  if (userInfo.tenantId) {
    const tenantOverride = rows.find((r) => r.tenantId === userInfo.tenantId && r.userId == null);
    if (tenantOverride) return isRowActive(tenantOverride) ? ENABLED : DISABLED;
  }

  // No overrides -- check A/B rollout on base row
  if (baseRow.rolloutBucketStart != null && baseRow.rolloutBucketEnd != null) {
    const rolloutBucketStart = parseInt(baseRow.rolloutBucketStart);
    const rolloutBucketEnd = parseInt(baseRow.rolloutBucketEnd);
    const rolloutBucket = definition.scope === "user" ? userInfo.userRolloutBucket : userInfo.tenantRolloutBucket;
    if (rolloutBucket == null) return DISABLED;
    return isInRolloutBucketRange(rolloutBucket, rolloutBucketStart, rolloutBucketEnd) ? ENABLED : DISABLED;
  }

  return ENABLED;
}

import { eq, useLiveQuery } from "@tanstack/react-db";

import { featureFlagCollection } from "../sync/collections";
import { getFlag } from "./featureFlagDefinitions";

type FeatureFlagResult = { enabled: boolean; isLoading: boolean };

const DISABLED: FeatureFlagResult = { enabled: false, isLoading: false };
const ENABLED: FeatureFlagResult = { enabled: true, isLoading: false };
const LOADING: FeatureFlagResult = { enabled: false, isLoading: true };

function isRowActive(row: { enabledAt: string | null; disabledAt: string | null }): boolean {
  if (row.enabledAt == null) return false;
  if (row.disabledAt == null) return true;
  return new Date(row.enabledAt) > new Date(row.disabledAt);
}

export function useFeatureFlag(flagKey: string): FeatureFlagResult {
  const definition = getFlag(flagKey);
  const userInfo = import.meta.user_info_env;
  const isAuthenticated = userInfo.isAuthenticated;
  const isSystemFlag = definition?.type === "system";

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
              configurableByTenant: featureFlags.configurableByTenant,
              configurableByUser: featureFlags.configurableByUser
            }))
        : undefined,
    [flagKey, isSystemFlag, isAuthenticated]
  );

  // Unknown flag
  if (!definition) return DISABLED;

  // System-scoped flags: read from env vars
  if (definition.type === "system") {
    return import.meta.runtime_env[definition.runtimeEnvKey] === "true" ? ENABLED : DISABLED;
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

  return ENABLED;
}

import { getFlag } from "./registry";

type FeatureFlagResult = { enabled: boolean; isLoading: boolean };

const DISABLED: FeatureFlagResult = { enabled: false, isLoading: false };
const ENABLED: FeatureFlagResult = { enabled: true, isLoading: false };

export function useFeatureFlag(flagKey: string): FeatureFlagResult {
  const definition = getFlag(flagKey);
  if (!definition) return DISABLED;

  if (definition.scope === "system") {
    const envVar = definition.envVar as keyof RuntimeEnv;
    return import.meta.runtime_env[envVar] === "true" ? ENABLED : DISABLED;
  }

  const userInfo = import.meta.user_info_env;
  if (!userInfo.isAuthenticated) return DISABLED;

  const enabledFlags = userInfo.featureFlags ?? [];
  return enabledFlags.includes(flagKey) ? ENABLED : DISABLED;
}

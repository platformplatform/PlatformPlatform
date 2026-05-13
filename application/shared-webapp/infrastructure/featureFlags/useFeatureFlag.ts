import { getFlag } from "@repo/ui/featureFlags/registry.generated";

import { useUserInfo } from "../auth/hooks";

type FeatureFlagResult = { enabled: boolean; isLoading: boolean };

const DISABLED: FeatureFlagResult = { enabled: false, isLoading: false };
const ENABLED: FeatureFlagResult = { enabled: true, isLoading: false };

export function useFeatureFlag(flagKey: string): FeatureFlagResult {
  // Read on every render so re-renders triggered by AuthenticationProvider state updates pick up
  // the new flag set without a page reload.
  const userInfo = useUserInfo();

  const definition = getFlag(flagKey);
  if (!definition) return DISABLED;

  if (definition.scope === "system") {
    const envVar = definition.envVar as keyof RuntimeEnv;
    return import.meta.runtime_env[envVar] === "true" ? ENABLED : DISABLED;
  }

  if (!userInfo?.isAuthenticated) return DISABLED;

  const enabledFlags = userInfo.featureFlags ?? [];
  return enabledFlags.includes(flagKey) ? ENABLED : DISABLED;
}

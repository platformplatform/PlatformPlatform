import { type FeatureFlagKey, getFlag } from "@repo/ui/featureFlags/registry.generated";

import { useUserInfo } from "../auth/hooks";

type FeatureFlagResult = { enabled: boolean; isLoading: boolean };

const DISABLED: FeatureFlagResult = { enabled: false, isLoading: false };
const ENABLED: FeatureFlagResult = { enabled: true, isLoading: false };

// `flagKey: FeatureFlagKey` is the codegen-emitted union of every key in FeatureFlags.cs. Passing a
// string that isn't a current backend flag is a TS compile error, so deleting or renaming a flag
// surfaces every dead callsite at build time instead of silently returning DISABLED at runtime.
export function useFeatureFlag(flagKey: FeatureFlagKey): FeatureFlagResult {
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

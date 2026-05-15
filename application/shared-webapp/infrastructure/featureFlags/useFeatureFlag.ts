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

  if (definition.scope === "system") {
    // `RuntimeEnv` is hand-maintained in shared-webapp/build/environment.d.ts, but
    // `definition.envVar` is sourced from the C# `FrontendEnvVar` field via codegen. Guard at
    // runtime so a system flag whose env var hasn't been declared on `RuntimeEnv` returns DISABLED
    // explicitly instead of silently reading `undefined` through an unchecked cast.
    const { envVar } = definition;
    if (!(envVar in import.meta.runtime_env)) return DISABLED;
    return import.meta.runtime_env[envVar as keyof RuntimeEnv] === "true" ? ENABLED : DISABLED;
  }

  if (!userInfo?.isAuthenticated) return DISABLED;

  const enabledFlags = userInfo.featureFlags ?? [];
  return enabledFlags.includes(flagKey) ? ENABLED : DISABLED;
}

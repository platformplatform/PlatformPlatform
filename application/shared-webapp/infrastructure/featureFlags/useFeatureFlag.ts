import { type FeatureFlagKey, getFlag } from "@repo/ui/featureFlags/registry.generated";

import { useUserInfo } from "../auth/hooks";
import { getLatestUserFeatureFlags } from "./userFeatureFlagsHeader";

type FeatureFlagResult = { enabled: boolean };

const DISABLED: FeatureFlagResult = { enabled: false };
const ENABLED: FeatureFlagResult = { enabled: true };

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

// Synchronous flag check for non-React callers (TanStack Router `beforeLoad` guards, fetch
// interceptors, etc.). Reads the live header-dispatched flag set when an authenticated response
// has already resolved in this session, and falls back to the bootstrap meta tag only on first
// load. Without the live-read step, in-session toggles (owner self-service, admin override) would
// diverge from the `useFeatureFlag` hook until a full page reload.
export function isFeatureFlagEnabled(flagKey: FeatureFlagKey): boolean {
  const definition = getFlag(flagKey);

  if (definition.scope === "system") {
    const { envVar } = definition;
    if (!(envVar in import.meta.runtime_env)) return false;
    return import.meta.runtime_env[envVar as keyof RuntimeEnv] === "true";
  }

  const liveFlags = getLatestUserFeatureFlags();
  if (liveFlags !== null) return liveFlags.includes(flagKey);

  const userInfo = import.meta.user_info_env;
  if (!userInfo.isAuthenticated) return false;
  const bootstrapFlags = userInfo.featureFlags ?? [];
  return bootstrapFlags.includes(flagKey);
}

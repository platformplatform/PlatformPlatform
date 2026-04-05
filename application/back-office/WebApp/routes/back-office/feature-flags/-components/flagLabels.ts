import { getFlag } from "@repo/infrastructure/featureFlags/featureFlagDefinitions";

function formatFeatureFlagKey(flagKey: string): string {
  const formatted = flagKey.replace(/-/g, " ");
  return formatted.charAt(0).toUpperCase() + formatted.slice(1);
}

export function getFeatureFlagName(flagKey: string): string {
  return getFlag(flagKey)?.name ?? formatFeatureFlagKey(flagKey);
}

export function getFeatureFlagDescription(flagKey: string): string {
  return getFlag(flagKey)?.description ?? "";
}

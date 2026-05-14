// AUTO-GENERATED FROM application/shared-kernel/SharedKernel/FeatureFlags/FeatureFlags.cs.
// Regenerate with `dotnet run --project developer-cli -- build --backend`. Do not edit by hand.
//
// The English copy here mirrors the Label and Description fields on each FeatureFlagDefinition.
// Lingui extracts these strings at frontend build time into shared-webapp/ui/translations/locale/*.po
// for translators to localize.
//
// Helpers accept `string` (not the strict FeatureFlagKey union) because flag keys arrive here
// from API responses too — values that the type system can't pin to the current set. Unknown keys
// fall back to a humanized form so historical telemetry and stale tenant overrides still display
// readably. Strict-typing for hard-coded keys happens at the `useFeatureFlag` hook, not here.

import { t } from "@lingui/core/macro";

interface FeatureFlagLabel {
  name: string;
  description: string;
}

function getKnownFeatureFlagLabels(): Record<string, FeatureFlagLabel> {
  return {
    "google-oauth": {
      name: t`Google OAuth`,
      description: t`Sign in with Google using OpenID Connect`
    },
    "subscriptions": {
      name: t`Subscriptions`,
      description: t`Stripe-powered subscription billing and plan management`
    },
    "beta-features": {
      name: t`Beta features`,
      description: t`Early access to experimental features before general availability`
    },
    "sso": {
      name: t`Single sign-on`,
      description: t`Allow users to authenticate using enterprise identity providers`
    },
    "account-overview": {
      name: t`Account overview page`,
      description: t`Show the account overview dashboard with user statistics at /account. When disabled, signed-in users go straight to the users list.`
    },
    "compact-view": {
      name: t`Compact view`,
      description: t`Reduce spacing between UI elements for a denser layout`
    },
    "experimental-ui": {
      name: t`Experimental UI`,
      description: t`Try out experimental user interface components`
    }
  };
}

function formatFeatureFlagKey(flagKey: string): string {
  const formatted = flagKey.replace(/-/g, " ");
  return formatted.charAt(0).toUpperCase() + formatted.slice(1);
}

export function getFeatureFlagLabel(flagKey: string): FeatureFlagLabel {
  const known = getKnownFeatureFlagLabels()[flagKey];
  if (known) return known;
  const name = formatFeatureFlagKey(flagKey);
  return { name, description: name };
}

export function getFeatureFlagName(flagKey: string): string {
  return getFeatureFlagLabel(flagKey).name;
}

export function getFeatureFlagDescription(flagKey: string): string {
  return getFeatureFlagLabel(flagKey).description;
}

export function getFeatureFlagSourceLabel(source: string): string {
  switch (source) {
    case "manual_override":
      return t`Manual override`;
    case "ab_rollout":
      return t`A/B rollout`;
    case "plan":
      return t`Plan`;
    case "default":
      return t`Default`;
    default:
      return source;
  }
}

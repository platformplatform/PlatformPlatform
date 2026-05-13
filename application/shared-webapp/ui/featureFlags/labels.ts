import { t } from "@lingui/core/macro";

interface FeatureFlagLabel {
  name: string;
  description: string;
}

// The English copy here mirrors `FeatureFlags.cs` (Label and Description fields). Backend is the
// source of truth for which flags exist and what they're called; this file exists because Lingui
// needs the strings present at extraction time so translators can localize them.
function getKnownFeatureFlagLabels(): Record<string, FeatureFlagLabel> {
  return {
    "google-oauth": {
      name: t`Google OAuth`,
      description: t`Sign in with Google using OpenID Connect`
    },
    subscriptions: {
      name: t`Subscriptions`,
      description: t`Stripe-powered subscription billing and plan management`
    },
    "beta-features": {
      name: t`Beta features`,
      description: t`Early access to experimental features before general availability`
    },
    sso: {
      name: t`Single sign-on`,
      description: t`Allow users to authenticate using enterprise identity providers`
    },
    "custom-branding": {
      name: t`Custom branding`,
      description: t`Customize the login page with your organization's logo and colors`
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

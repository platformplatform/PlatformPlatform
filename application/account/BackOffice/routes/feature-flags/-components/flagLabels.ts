import { t } from "@lingui/core/macro";

interface FlagLabel {
  name: string;
  description: string;
}

function getKnownFlagLabels(): Record<string, FlagLabel> {
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

function formatFlagKey(flagKey: string): string {
  const formatted = flagKey.replace(/-/g, " ");
  return formatted.charAt(0).toUpperCase() + formatted.slice(1);
}

export function getFlagName(flagKey: string): string {
  return getKnownFlagLabels()[flagKey]?.name ?? formatFlagKey(flagKey);
}

export function getFlagDescription(flagKey: string): string {
  return getKnownFlagLabels()[flagKey]?.description ?? "";
}

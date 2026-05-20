// AUTO-GENERATED FROM application/shared-kernel/SharedKernel/FeatureFlags/FeatureFlags.cs.
// Regenerate with `dotnet run --project developer-cli -- build --backend`. Do not edit by hand.
//
// Carries the runtime metadata that `useFeatureFlag` needs to evaluate a flag client-side. System
// flags additionally carry the frontend env-var name so the hook can read from import.meta.runtime_env.
//
// FeatureFlagKey is the union of every key defined in FeatureFlags.cs. Hook + helper signatures
// accept this union instead of `string`, so deleting or renaming a backend flag turns every
// `useFeatureFlag(deletedKey)` and `getFeatureFlagLabel(deletedKey)` callsite into a TS compile
// error after the next backend build regenerates this file.

export type FeatureFlagKey = "google-oauth" | "subscriptions" | "support-system" | "beta-features" | "sso" | "account-overview" | "compact-view" | "experimental-ui";

type FeatureFlagScope = "system" | "tenant" | "user";
type FeatureFlagAdminLevel = "systemAdmin" | "tenantOwner" | "user";

type BaseFeatureFlagDefinition = {
  key: FeatureFlagKey;
  scope: FeatureFlagScope;
  adminLevel: FeatureFlagAdminLevel;
  parentDependency: FeatureFlagKey | null;
  description: string;
};

type SystemFeatureFlagDefinition = BaseFeatureFlagDefinition & {
  scope: "system";
  envVar: string;
};

type DatabaseFeatureFlagDefinition = BaseFeatureFlagDefinition & {
  scope: "tenant" | "user";
};

export type FeatureFlagDefinition = SystemFeatureFlagDefinition | DatabaseFeatureFlagDefinition;

const featureFlagRegistry: Record<FeatureFlagKey, FeatureFlagDefinition> = {
    "google-oauth": {
      key: "google-oauth",
      scope: "system",
      adminLevel: "systemAdmin",
      parentDependency: null,
      description: "Sign in with Google using OpenID Connect",
      envVar: "PUBLIC_GOOGLE_OAUTH_ENABLED"
    },
    "subscriptions": {
      key: "subscriptions",
      scope: "system",
      adminLevel: "systemAdmin",
      parentDependency: null,
      description: "Stripe-powered subscription billing and plan management",
      envVar: "PUBLIC_SUBSCRIPTION_ENABLED"
    },
    "support-system": {
      key: "support-system",
      scope: "system",
      adminLevel: "systemAdmin",
      parentDependency: null,
      description: "In-app support ticket creation, inbox, and back-office support tabs",
      envVar: "PUBLIC_SUPPORT_SYSTEM_ENABLED"
    },
    "beta-features": {
      key: "beta-features",
      scope: "tenant",
      adminLevel: "systemAdmin",
      parentDependency: null,
      description: "Early access to experimental features before general availability"
    },
    "sso": {
      key: "sso",
      scope: "tenant",
      adminLevel: "systemAdmin",
      parentDependency: null,
      description: "Allow users to authenticate using enterprise identity providers"
    },
    "account-overview": {
      key: "account-overview",
      scope: "tenant",
      adminLevel: "tenantOwner",
      parentDependency: null,
      description: "Show the account overview dashboard with user statistics at /account. When disabled, signed-in users go straight to the users list."
    },
    "compact-view": {
      key: "compact-view",
      scope: "user",
      adminLevel: "user",
      parentDependency: null,
      description: "Reduce spacing between UI elements for a denser layout"
    },
    "experimental-ui": {
      key: "experimental-ui",
      scope: "user",
      adminLevel: "user",
      parentDependency: null,
      description: "Try out experimental user interface components"
    }
};

export function getFlag(key: FeatureFlagKey): FeatureFlagDefinition {
  return featureFlagRegistry[key];
}

export function getAllFlags(): FeatureFlagDefinition[] {
  return Object.values(featureFlagRegistry);
}

export { featureFlagRegistry };

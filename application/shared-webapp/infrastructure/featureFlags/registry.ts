type FeatureFlagScope = "system" | "tenant" | "user";
type FeatureFlagAdminLevel = "systemAdmin" | "tenantOwner" | "user";

type BaseFeatureFlagDefinition = {
  key: string;
  scope: FeatureFlagScope;
  adminLevel: FeatureFlagAdminLevel;
  parentDependency: string | null;
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

const featureFlagRegistry: Record<string, FeatureFlagDefinition> = {
  "beta-features": {
    key: "beta-features",
    scope: "tenant",
    adminLevel: "systemAdmin",
    parentDependency: null,
    description: "Enables beta features for tenants"
  },
  sso: {
    key: "sso",
    scope: "tenant",
    adminLevel: "systemAdmin",
    parentDependency: null,
    description: "Enables single sign-on for tenants"
  },
  "custom-branding": {
    key: "custom-branding",
    scope: "tenant",
    adminLevel: "tenantOwner",
    parentDependency: null,
    description: "Enables custom branding options for tenants"
  },
  "compact-view": {
    key: "compact-view",
    scope: "user",
    adminLevel: "user",
    parentDependency: null,
    description: "Enables compact view in the user interface"
  },
  "google-oauth": {
    key: "google-oauth",
    scope: "system",
    adminLevel: "systemAdmin",
    parentDependency: null,
    description: "Enables Google OAuth authentication",
    envVar: "PUBLIC_GOOGLE_OAUTH_ENABLED"
  },
  subscriptions: {
    key: "subscriptions",
    scope: "system",
    adminLevel: "systemAdmin",
    parentDependency: null,
    description: "Enables subscription and billing features",
    envVar: "PUBLIC_SUBSCRIPTION_ENABLED"
  }
};

export function getFlag(key: string): FeatureFlagDefinition | undefined {
  return featureFlagRegistry[key];
}

export function getAllFlags(): FeatureFlagDefinition[] {
  return Object.values(featureFlagRegistry);
}

export { featureFlagRegistry };

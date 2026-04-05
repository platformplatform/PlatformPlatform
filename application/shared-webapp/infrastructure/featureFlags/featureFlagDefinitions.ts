import featureFlags from "../../../feature-flags.json";

export type FeatureFlagType = "system" | "subscriptionPlan" | "tenant" | "user";

type FeatureFlagJsonEntry = {
  featureFlagKey: string;
  name: string;
  type: FeatureFlagType;
  description: string;
  systemConfigKey?: string;
  systemConfigExpectedValue?: string;
  isAbTestEligible?: boolean;
  configurableByTenant?: boolean;
  configurableByUser?: boolean;
  requiredSubscriptionPlan?: string;
  adminLevel?: string;
};

type SystemFeatureFlagDefinition = {
  key: string;
  name: string;
  type: "system";
  description: string;
  runtimeEnvKey: keyof RuntimeEnv;
};

type DatabaseFeatureFlagDefinition = {
  key: string;
  name: string;
  type: "subscriptionPlan" | "tenant" | "user";
  description: string;
};

export type FeatureFlagDefinition = SystemFeatureFlagDefinition | DatabaseFeatureFlagDefinition;

// Maps system flag keys to their corresponding frontend runtime environment variable
const systemFlagEnvKeys: Record<string, keyof RuntimeEnv> = {
  "google-oauth": "PUBLIC_GOOGLE_OAUTH_ENABLED",
  subscriptions: "PUBLIC_SUBSCRIPTION_ENABLED"
};

function toDefinition(entry: FeatureFlagJsonEntry): FeatureFlagDefinition | null {
  if (entry.type === "system") {
    const runtimeEnvKey = systemFlagEnvKeys[entry.featureFlagKey];
    if (!runtimeEnvKey) {
      console.warn(`System feature flag "${entry.featureFlagKey}" has no runtimeEnvKey mapping`);
      return null;
    }
    return {
      key: entry.featureFlagKey,
      name: entry.name,
      type: "system",
      description: entry.description,
      runtimeEnvKey
    };
  }
  return { key: entry.featureFlagKey, name: entry.name, type: entry.type, description: entry.description };
}

const definitionsByKey = new Map<string, FeatureFlagDefinition>();
for (const entry of featureFlags as FeatureFlagJsonEntry[]) {
  const definition = toDefinition(entry);
  if (definition) {
    definitionsByKey.set(entry.featureFlagKey, definition);
  }
}

export function getFlag(key: string): FeatureFlagDefinition | undefined {
  return definitionsByKey.get(key);
}

export function getAllFlags(): FeatureFlagDefinition[] {
  return [...definitionsByKey.values()];
}

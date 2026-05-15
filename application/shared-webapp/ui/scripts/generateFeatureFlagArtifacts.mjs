#!/usr/bin/env node
/**
 * Generates two TS artifacts from application/shared-webapp/ui/featureFlags/featureFlags.generated.json:
 *
 *   - labels.generated.ts   — Lingui t`...` macros for every flag's Label and Description.
 *                              Frontend Lingui extraction reads this and writes the strings into
 *                              shared-webapp/ui/translations/locale/*.po for translators to localize.
 *
 *   - registry.generated.ts — Runtime registry consumed by `useFeatureFlag`. Carries the metadata
 *                              the React hook needs to evaluate a flag (scope discrimination, the
 *                              frontend env var name for system flags, etc.).
 *
 * The JSON manifest is emitted by the GenerateFeatureFlagsManifest MSBuild target in
 * application/account/Api/Account.Api.csproj. Run `build --backend` to regenerate both the manifest
 * and these artifacts.
 *
 * Both generated files match the "**\/*.generated.*" glob in application/.oxfmtrc.json, so the
 * formatter leaves them alone. Hand-edits to either file would be overwritten on the next build.
 */
import { readFileSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const featureFlagsDirectory = resolve(scriptDirectory, "../featureFlags");
const manifestPath = resolve(featureFlagsDirectory, "featureFlags.generated.json");
const labelsOutputPath = resolve(featureFlagsDirectory, "labels.generated.ts");
const registryOutputPath = resolve(featureFlagsDirectory, "registry.generated.ts");

const flags = JSON.parse(readFileSync(manifestPath, "utf-8"));

function escapeTemplateLiteral(value) {
    return value.replace(/\\/g, "\\\\").replace(/`/g, "\\`").replace(/\$\{/g, "\\${");
}

function jsonString(value) {
    return JSON.stringify(value);
}

function scopeLiteral(scope) {
    return jsonString(scope.toLowerCase());
}

function adminLevelLiteral(level) {
    // Backend enum uses PascalCase (SystemAdmin, TenantOwner, User); the TS contract is camelCase.
    const map = { SystemAdmin: "systemAdmin", TenantOwner: "tenantOwner", User: "user" };
    if (!(level in map)) throw new Error(`Unknown AdminLevel: ${level}`);
    return jsonString(map[level]);
}

// ---------- labels.generated.ts ----------

const labelEntries = flags
    .map((flag) => {
        const key = jsonString(flag.key);
        const label = escapeTemplateLiteral(flag.label);
        const description = escapeTemplateLiteral(flag.description);
        return `    ${key}: {\n      name: t\`${label}\`,\n      description: t\`${description}\`\n    }`;
    })
    .join(",\n");

const labelsContent = `// AUTO-GENERATED FROM application/shared-kernel/SharedKernel/FeatureFlags/FeatureFlags.cs.
// Regenerate with \`dotnet run --project developer-cli -- build --backend\`. Do not edit by hand.
//
// The English copy here mirrors the Label and Description fields on each FeatureFlagDefinition.
// Lingui extracts these strings at frontend build time into shared-webapp/ui/translations/locale/*.po
// for translators to localize.
//
// Helpers accept \`string\` (not the strict FeatureFlagKey union) because flag keys arrive here
// from API responses too — values that the type system can't pin to the current set. Unknown keys
// fall back to a humanized form so historical telemetry and stale tenant overrides still display
// readably. Strict-typing for hard-coded keys happens at the \`useFeatureFlag\` hook, not here.

import { t } from "@lingui/core/macro";

interface FeatureFlagLabel {
  name: string;
  description: string;
}

function getKnownFeatureFlagLabels(): Record<string, FeatureFlagLabel> {
  return {
${labelEntries}
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
      return t\`Manual override\`;
    case "ab_rollout":
      return t\`A/B rollout\`;
    case "plan":
      return t\`Plan\`;
    case "default":
      return t\`Default\`;
    default:
      return source;
  }
}
`;

writeFileSync(labelsOutputPath, labelsContent);

// ---------- registry.generated.ts ----------

const registryEntries = flags
    .map((flag) => {
        const fields = [
            `      key: ${jsonString(flag.key)}`,
            `      scope: ${scopeLiteral(flag.scope)}`,
            `      adminLevel: ${adminLevelLiteral(flag.adminLevel)}`,
            `      parentDependency: ${flag.parentDependency === null ? "null" : jsonString(flag.parentDependency)}`,
            `      description: ${jsonString(flag.description)}`
        ];
        if (flag.scope === "System") {
            if (flag.frontendEnvVar == null) {
                throw new Error(`System flag '${flag.key}' is missing FrontendEnvVar; set it in FeatureFlags.cs.`);
            }
            fields.push(`      envVar: ${jsonString(flag.frontendEnvVar)}`);
        }
        return `    ${jsonString(flag.key)}: {\n${fields.join(",\n")}\n    }`;
    })
    .join(",\n");

const keyUnion = flags.map((flag) => jsonString(flag.key)).join(" | ");

const registryContent = `// AUTO-GENERATED FROM application/shared-kernel/SharedKernel/FeatureFlags/FeatureFlags.cs.
// Regenerate with \`dotnet run --project developer-cli -- build --backend\`. Do not edit by hand.
//
// Carries the runtime metadata that \`useFeatureFlag\` needs to evaluate a flag client-side. System
// flags additionally carry the frontend env-var name so the hook can read from import.meta.runtime_env.
//
// FeatureFlagKey is the union of every key defined in FeatureFlags.cs. Hook + helper signatures
// accept this union instead of \`string\`, so deleting or renaming a backend flag turns every
// \`useFeatureFlag(deletedKey)\` and \`getFeatureFlagLabel(deletedKey)\` callsite into a TS compile
// error after the next backend build regenerates this file.

export type FeatureFlagKey = ${keyUnion};

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
${registryEntries}
};

export function getFlag(key: FeatureFlagKey): FeatureFlagDefinition {
  return featureFlagRegistry[key];
}

export function getAllFlags(): FeatureFlagDefinition[] {
  return Object.values(featureFlagRegistry);
}

export { featureFlagRegistry };
`;

writeFileSync(registryOutputPath, registryContent);

console.log(
    `generateFeatureFlagArtifacts: wrote ${flags.length} flags to labels.generated.ts and registry.generated.ts`
);

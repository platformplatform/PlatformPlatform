import { t } from "@lingui/core/macro";

interface FeatureFlagLabel {
  name: string;
  description: string;
}

function getKnownFlagLabels(): Record<string, FeatureFlagLabel> {
  return {
    "custom-branding": {
      name: t`Custom branding`,
      description: t`Enable branded login page`
    },
    "compact-view": {
      name: t`Compact view`,
      description: t`Use a more compact layout`
    },
    "experimental-ui": {
      name: t`Experimental UI`,
      description: t`Try experimental UI components`
    }
  };
}

function formatFlagKey(flagKey: string): string {
  const formatted = flagKey.replace(/-/g, " ");
  return formatted.charAt(0).toUpperCase() + formatted.slice(1);
}

export function getFeatureFlagLabel(flagKey: string): FeatureFlagLabel {
  const known = getKnownFlagLabels()[flagKey];
  if (known) {
    return known;
  }
  const name = formatFlagKey(flagKey);
  return { name, description: name };
}

import { t } from "@lingui/core/macro";

interface FeatureFlagLabel {
  name: string;
  description: string;
}

function getTranslatedLabels(): Record<string, FeatureFlagLabel> {
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
    },
    "beta-features": {
      name: t`Beta features`,
      description: t`Early access to experimental features`
    }
  };
}

function formatFeatureFlagKey(flagKey: string): string {
  const formatted = flagKey.replace(/-/g, " ");
  return formatted.charAt(0).toUpperCase() + formatted.slice(1);
}

export function getFeatureFlagLabel(flagKey: string): FeatureFlagLabel {
  const translated = getTranslatedLabels()[flagKey];
  if (translated) {
    return translated;
  }
  const name = formatFeatureFlagKey(flagKey);
  return { name, description: name };
}

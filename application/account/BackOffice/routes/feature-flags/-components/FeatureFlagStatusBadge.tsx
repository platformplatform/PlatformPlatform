import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";

import type { FeatureFlagInfo } from "./types";

export function FeatureFlagStatusBadge({ featureFlag }: Readonly<{ featureFlag: FeatureFlagInfo }>) {
  if (featureFlag.deletedAt) {
    return (
      <Badge variant="destructive">
        <Trans>Deleted</Trans>
      </Badge>
    );
  }
  if (featureFlag.orphanedAt) {
    return (
      <Badge variant="destructive">
        <Trans>Removed</Trans>
      </Badge>
    );
  }
  if (featureFlag.isStableModule) {
    return (
      <Badge variant="default">
        <Trans>Always on</Trans>
      </Badge>
    );
  }
  return (
    <Badge variant={featureFlag.isActive ? "default" : "outline"}>
      {featureFlag.isActive ? <Trans>Active</Trans> : <Trans>Inactive</Trans>}
    </Badge>
  );
}

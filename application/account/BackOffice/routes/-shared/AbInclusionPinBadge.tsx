import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { CircleCheckIcon, CircleSlashIcon } from "lucide-react";

import { AbInclusionPin } from "@/shared/lib/api/client";

interface AbInclusionPinBadgeProps {
  pin: AbInclusionPin | null;
}

// Shows the entity-global A/B inclusion pin (AlwaysOn / NeverOn) when set. Null pin renders nothing
// so the header stays clean for the common case where the pin has never been touched.
export function AbInclusionPinBadge({ pin }: Readonly<AbInclusionPinBadgeProps>) {
  if (pin === null) return null;

  if (pin === AbInclusionPin.AlwaysOn) {
    return (
      <Badge variant="outline" className="gap-1 border-success/30 text-success">
        <CircleCheckIcon className="size-3" aria-hidden={true} />
        <Trans>First in feature flag rollouts</Trans>
      </Badge>
    );
  }

  return (
    <Badge variant="outline" className="gap-1 border-warning/30 text-warning">
      <CircleSlashIcon className="size-3" aria-hidden={true} />
      <Trans>Last in feature flag rollouts</Trans>
    </Badge>
  );
}

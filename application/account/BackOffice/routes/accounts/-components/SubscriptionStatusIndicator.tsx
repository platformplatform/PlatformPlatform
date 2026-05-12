import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { XCircleIcon } from "lucide-react";

import { TenantState } from "@/shared/lib/api/client";

export function SubscriptionStatusIndicator({
  state
}: Readonly<{
  state: TenantState | undefined;
}>) {
  if (state === TenantState.Suspended) {
    return (
      <Badge variant="destructive" className="w-fit gap-1">
        <XCircleIcon className="size-3" />
        <Trans>Suspended</Trans>
      </Badge>
    );
  }
  return null;
}

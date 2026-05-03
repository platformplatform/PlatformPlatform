import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { CalendarClockIcon, XCircleIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { PlannedSubscriptionChange, TenantState } from "@/shared/lib/api/client";
import { getSubscriptionPlanLabel } from "@/shared/lib/api/labels";

export function SubscriptionStatusIndicator({
  plannedChange,
  state,
  scheduledPlan
}: Readonly<{
  plannedChange: PlannedSubscriptionChange | null;
  state: TenantState | undefined;
  scheduledPlan: components["schemas"]["SubscriptionPlan"] | null;
}>) {
  if (state === TenantState.Suspended) {
    return (
      <Badge variant="outline" className="w-fit gap-1 border-destructive/30 text-destructive">
        <XCircleIcon className="size-3" />
        <Trans>Suspended</Trans>
      </Badge>
    );
  }
  if (plannedChange === PlannedSubscriptionChange.Cancellation) {
    return (
      <Badge variant="outline" className="w-fit gap-1 border-destructive/30 text-destructive">
        <XCircleIcon className="size-3" />
        <Trans>Cancellation at period end</Trans>
      </Badge>
    );
  }
  if (plannedChange === PlannedSubscriptionChange.ScheduledPlanChange) {
    return (
      <Badge variant="outline" className="w-fit gap-1">
        <CalendarClockIcon className="size-3" />
        {scheduledPlan ? (
          <Trans>Switching to {getSubscriptionPlanLabel(scheduledPlan)}</Trans>
        ) : (
          <Trans>Scheduled plan change</Trans>
        )}
      </Badge>
    );
  }
  return null;
}

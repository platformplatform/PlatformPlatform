import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { CalendarClockIcon, CheckCircle2Icon, MinusCircleIcon, XCircleIcon } from "lucide-react";

import { PlannedSubscriptionChange, SubscriptionPlan } from "@/shared/lib/api/client";

interface TenantStatusBadgeProps {
  plan: SubscriptionPlan;
  plannedChange: PlannedSubscriptionChange | null | undefined;
  hasEverSubscribed: boolean;
}

export function TenantStatusBadge({ plan, plannedChange, hasEverSubscribed }: Readonly<TenantStatusBadgeProps>) {
  if (plannedChange === PlannedSubscriptionChange.Cancellation) {
    return (
      <Badge variant="destructive" className="gap-1">
        <XCircleIcon className="size-3" />
        <Trans>Canceling</Trans>
      </Badge>
    );
  }
  if (plannedChange === PlannedSubscriptionChange.ScheduledPlanChange) {
    return (
      <Badge variant="warning" className="gap-1">
        <CalendarClockIcon className="size-3" />
        <Trans>Downgrading</Trans>
      </Badge>
    );
  }
  if (plan !== SubscriptionPlan.Basis) {
    return (
      <Badge variant="outline" className="gap-1 border-emerald-500/40 text-emerald-700 dark:text-emerald-300">
        <CheckCircle2Icon className="size-3" />
        <Trans>Active</Trans>
      </Badge>
    );
  }
  if (hasEverSubscribed) {
    return (
      <Badge variant="outline" className="gap-1 text-muted-foreground">
        <MinusCircleIcon className="size-3" />
        <Trans>Canceled</Trans>
      </Badge>
    );
  }
  return (
    <Badge variant="outline" className="text-muted-foreground">
      <Trans>Free</Trans>
    </Badge>
  );
}

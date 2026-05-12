import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { Link } from "@tanstack/react-router";
import { ScaleIcon } from "lucide-react";

import { api } from "@/shared/lib/api/client";

/**
 * Global banner that fires when the dashboard's KPI MRR (forward MRR from subscriptions) and the
 * trend-latest MRR (sum of latest BillingEvent NewAmount per subscription) disagree. They should
 * match in a healthy system; divergence indicates either an event-emission bug, direct DB mutation
 * without an event, or a regression in one of the handlers.
 */
export function MrrMismatchBanner() {
  const userInfo = useUserInfo();
  const { data } = api.useQuery(
    "get",
    "/api/back-office/billing-drift/mrr-consistency-summary",
    {},
    { enabled: userInfo?.isAuthenticated === true, refetchInterval: 60_000 }
  );

  if (!data || !data.currency || data.kpiMonthlyRecurringRevenue === data.trendLatestMonthlyRecurringRevenue) {
    return null;
  }

  const currency = data.currency;
  return (
    <div
      role="status"
      aria-live="polite"
      className="flex h-12 items-center gap-3 border-b border-warning/50 bg-warning px-4 text-sm"
    >
      <ScaleIcon className="size-4 shrink-0 text-warning-foreground" aria-hidden={true} />
      <span className="flex-1 text-warning-foreground">
        <Trans>
          Dashboard MRR mismatch: KPI shows {formatCurrency(data.kpiMonthlyRecurringRevenue, currency)}, trend latest
          shows {formatCurrency(data.trendLatestMonthlyRecurringRevenue, currency)}.
        </Trans>
      </span>
      <Button size="sm" nativeButton={false} render={<Link to="/billing-events" />}>
        <Trans>View billing events</Trans>
      </Button>
    </div>
  );
}

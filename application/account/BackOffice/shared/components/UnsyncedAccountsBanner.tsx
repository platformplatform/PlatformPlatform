import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { Link } from "@tanstack/react-router";
import { CloudOffIcon } from "lucide-react";

import { api } from "@/shared/lib/api/client";

/**
 * Global banner that surfaces paid subscriptions that have never been synced into the BillingEvent log.
 * The dashboard's MRR trend is computed from BillingEvents, so unsynced subscriptions silently under-count
 * the trend (the KPI tile and the trend would diverge). The banner is invisible when every paid
 * subscription has at least one BillingEvent row.
 */
export function UnsyncedAccountsBanner() {
  const userInfo = useUserInfo();
  const { data } = api.useQuery(
    "get",
    "/api/back-office/billing-drift/unsynced-summary",
    {},
    { enabled: userInfo?.isAuthenticated === true, refetchInterval: 60_000 }
  );

  const count = data?.unsyncedSubscriptionsCount ?? 0;
  if (count === 0) {
    return null;
  }

  return (
    <div className="flex h-12 items-center gap-3 border-b border-warning/50 bg-warning px-4 text-sm">
      <CloudOffIcon className="size-4 shrink-0 text-warning-foreground" aria-hidden={true} />
      <span className="flex-1 text-warning-foreground">
        <Trans>{count} accounts have not been synced yet — MRR trend is incomplete.</Trans>
      </span>
      <Button size="sm" nativeButton={false} render={<Link to="/accounts" />}>
        <Trans>View accounts</Trans>
      </Button>
    </div>
  );
}

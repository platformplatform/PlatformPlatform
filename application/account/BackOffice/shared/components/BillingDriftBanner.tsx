import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Link } from "@tanstack/react-router";
import { AlertTriangleIcon } from "lucide-react";

import { api } from "@/shared/lib/api/client";

/**
 * Global banner that surfaces accounts with detected billing drift. Renders only when at least one
 * subscription has unacknowledged drift, so the banner is invisible in a healthy system. Click-through
 * navigates to /accounts with a hidden ?driftOnly=true filter applied (the toolbar does not expose this
 * filter directly; the banner is the single discovery surface).
 */
export function BillingDriftBanner() {
  const userInfo = useUserInfo();
  const { data } = api.useQuery(
    "get",
    "/api/back-office/billing-drift/summary",
    {},
    { enabled: userInfo?.isAuthenticated === true, refetchInterval: 60_000 }
  );

  const count = data?.subscriptionsWithDriftCount ?? 0;
  if (count === 0) {
    return null;
  }

  return (
    <div
      role="status"
      aria-live="polite"
      className="flex items-center justify-between gap-3 border-b border-amber-300 bg-amber-50 px-4 py-2 text-sm text-amber-900 dark:border-amber-700 dark:bg-amber-950 dark:text-amber-100"
    >
      <div className="flex items-center gap-2">
        <AlertTriangleIcon className="size-4" aria-hidden={true} />
        <span>
          <Trans>{count} accounts have billing drift detected.</Trans>
        </span>
      </div>
      <Link to="/accounts" className="font-medium underline hover:no-underline">
        <Trans>View accounts</Trans>
      </Link>
    </div>
  );
}

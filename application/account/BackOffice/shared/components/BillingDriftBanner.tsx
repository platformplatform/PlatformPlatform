import { plural } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { Link } from "@tanstack/react-router";
import { AlertTriangleIcon } from "lucide-react";

import { api } from "@/shared/lib/api/client";

/**
 * Global banner that surfaces accounts with detected billing drift. Renders only when at least one
 * subscription has unacknowledged drift, so the banner is invisible in a healthy system. Click-through
 * navigates to /accounts.
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
      role="alert"
      aria-live="assertive"
      className="flex h-12 items-center gap-3 border-b border-warning/50 bg-warning px-4 text-sm"
    >
      <AlertTriangleIcon className="size-4 shrink-0 text-warning-foreground" aria-hidden={true} />
      <span className="flex-1 text-warning-foreground">
        {plural(count, {
          one: "# account has billing drift detected.",
          other: "# accounts have billing drift detected."
        })}
      </span>
      <Button size="sm" nativeButton={false} render={<Link to="/accounts" search={{ driftDetected: true }} />}>
        <Trans>View accounts</Trans>
      </Button>
    </div>
  );
}

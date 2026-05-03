import { Trans } from "@lingui/react/macro";
import { Skeleton } from "@repo/ui/components/Skeleton";

import type { components } from "@/shared/lib/api/client";

export function SidePaneUsersRow({
  detailReady,
  userCounts,
  isLoading
}: Readonly<{
  detailReady: boolean;
  userCounts: components["schemas"]["TenantUserCountsResponse"] | undefined;
  isLoading: boolean;
}>) {
  if (!detailReady || isLoading) {
    return (
      <div className="flex flex-col gap-2">
        <Skeleton className="h-4 w-32" />
        <Skeleton className="h-1.5 w-full" />
      </div>
    );
  }
  if (!userCounts) {
    return <span className="text-sm text-muted-foreground">-</span>;
  }
  const { totalUsers, activeUsers, pendingUsers } = userCounts;
  const inactiveUsers = Math.max(0, totalUsers - activeUsers - pendingUsers);

  const activePercent = totalUsers === 0 ? 0 : (activeUsers / totalUsers) * 100;
  const inactivePercent = totalUsers === 0 ? 0 : (inactiveUsers / totalUsers) * 100;
  const pendingPercent = totalUsers === 0 ? 0 : (pendingUsers / totalUsers) * 100;

  return (
    <div className="flex flex-col gap-2">
      <span className="text-sm text-muted-foreground tabular-nums">
        <Trans>
          <span className="text-base font-semibold text-foreground">{totalUsers}</span> total
        </Trans>
        {" · "}
        <Trans>{activeUsers} active</Trans>
        {" · "}
        <Trans>{inactiveUsers} inactive</Trans>
        {" · "}
        <Trans>{pendingUsers} pending</Trans>
      </span>
      <div className="flex h-1.5 w-full gap-0.5 overflow-hidden rounded-full bg-muted">
        {activePercent > 0 && <div className="h-full rounded-full bg-success" style={{ width: `${activePercent}%` }} />}
        {inactivePercent > 0 && (
          <div className="h-full rounded-full bg-warning" style={{ width: `${inactivePercent}%` }} />
        )}
        {pendingPercent > 0 && (
          <div className="h-full rounded-full bg-muted-foreground/40" style={{ width: `${pendingPercent}%` }} />
        )}
      </div>
    </div>
  );
}

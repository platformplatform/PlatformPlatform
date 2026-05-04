import type { ReactNode } from "react";

import { plural, t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Card } from "@repo/ui/components/Card";
import { Skeleton } from "@repo/ui/components/Skeleton";

import type { components } from "@/shared/lib/api/client";

import { SmartDateTime } from "@/shared/components/SmartDateTime";
import { api } from "@/shared/lib/api/client";

type BackOfficeUserDetailResponse = components["schemas"]["BackOfficeUserDetailResponse"];

interface UserKpiCardsProps {
  user: BackOfficeUserDetailResponse | undefined;
  userId: string;
  isLoading: boolean;
}

export function UserKpiCards({ user, userId, isLoading }: Readonly<UserKpiCardsProps>) {
  const sessionsQuery = api.useQuery("get", "/api/back-office/users/{id}/sessions", {
    params: { path: { id: userId } }
  });
  const sessionsLoading = sessionsQuery.isLoading;
  const totalSessions = sessionsQuery.data?.totalCount;
  const tenantCount = user?.tenantMemberships.length ?? 0;

  return (
    <div className="grid grid-cols-[repeat(auto-fit,minmax(13rem,1fr))] gap-4">
      <KpiCard
        label={t`Sessions`}
        loading={sessionsLoading}
        subtitle={totalSessions !== undefined ? <Trans>All-time</Trans> : undefined}
      >
        <span className="text-2xl font-semibold tabular-nums">{totalSessions !== undefined ? totalSessions : "-"}</span>
      </KpiCard>

      <KpiCard
        label={t`Accounts`}
        loading={isLoading}
        subtitle={
          user
            ? plural(tenantCount, {
                one: "# membership",
                other: "# memberships"
              })
            : undefined
        }
      >
        <span className="text-2xl font-semibold tabular-nums">{user ? tenantCount : "-"}</span>
      </KpiCard>

      <KpiCard
        label={t`Last log-in`}
        loading={isLoading}
        subtitle={user?.lastSeenAt ? <Trans>Most recent activity</Trans> : <Trans>Never logged in</Trans>}
      >
        <span className="text-base font-semibold">
          {user?.lastSeenAt ? <SmartDateTime date={user.lastSeenAt} /> : "-"}
        </span>
      </KpiCard>
    </div>
  );
}

function KpiCard({
  label,
  loading,
  subtitle,
  children
}: Readonly<{
  label: string;
  loading: boolean;
  subtitle?: ReactNode;
  children: ReactNode;
}>) {
  return (
    <Card className="gap-2 rounded-lg p-4 shadow-none">
      <span className="text-xs font-semibold tracking-wider text-muted-foreground uppercase">{label}</span>
      {loading ? (
        <>
          <Skeleton className="h-7 w-24" />
          <Skeleton className="h-4 w-20" />
        </>
      ) : (
        <>
          {children}
          {subtitle && <span className="text-xs text-muted-foreground">{subtitle}</span>}
        </>
      )}
    </Card>
  );
}

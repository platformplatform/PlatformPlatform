import type { ReactNode } from "react";

import { plural, t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Card } from "@repo/ui/components/Card";
import { LinkCard } from "@repo/ui/components/LinkCard";
import { Skeleton } from "@repo/ui/components/Skeleton";

import type { components } from "@/shared/lib/api/client";

import { SmartDateTime } from "@/shared/components/SmartDateTime";
import { api, SupportTicketStatus } from "@/shared/lib/api/client";

type BackOfficeUserDetailResponse = components["schemas"]["BackOfficeUserDetailResponse"];

const isSupportSystemEnabled = import.meta.runtime_env.PUBLIC_SUPPORT_SYSTEM_ENABLED === "true";

interface UserActivityTilesProps {
  user: BackOfficeUserDetailResponse | undefined;
  userId: string;
  isLoading: boolean;
}

export function UserActivityTiles({ user, userId, isLoading }: Readonly<UserActivityTilesProps>) {
  const sessionsQuery = api.useQuery("get", "/api/back-office/users/{id}/sessions", {
    params: { path: { id: userId } }
  });
  const sessionsLoading = sessionsQuery.isLoading;
  const totalSessions = sessionsQuery.data?.totalCount;
  const tenantCount = user?.tenantMemberships.length ?? 0;

  // PageSize is requested at the validator-enforced cap of 100 so the chip count and the tab list
  // share one TanStack Query cache entry. For users with more than 100 tickets the chip undercounts
  // until a dedicated count endpoint exists; the v1 surface intentionally accepts that trade-off.
  // Skip the request entirely when the support system is gated off; the tile is hidden anyway.
  const supportTicketsQuery = api.useQuery(
    "get",
    "/api/back-office/support-tickets",
    { params: { query: { ReporterId: userId, PageSize: 100 } } },
    { enabled: isSupportSystemEnabled }
  );
  const supportTicketsLoading = supportTicketsQuery.isLoading;
  const openTicketCount = supportTicketsQuery.data?.tickets.filter(
    (ticket) => ticket.status !== SupportTicketStatus.Resolved && ticket.status !== SupportTicketStatus.Closed
  ).length;
  const totalTicketCount = supportTicketsQuery.data?.totalCount;

  return (
    <div className="grid grid-cols-[repeat(auto-fit,minmax(13rem,1fr))] gap-4">
      <ActivityTile
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
        linkTo={user ? "overview" : undefined}
        userId={userId}
      >
        <span className="text-2xl font-semibold tabular-nums">{user ? tenantCount : "-"}</span>
      </ActivityTile>

      <ActivityTile
        label={t`Last log-in`}
        loading={isLoading}
        subtitle={user?.lastSeenAt ? <Trans>Most recent activity</Trans> : <Trans>Never logged in</Trans>}
        linkTo={user?.lastSeenAt ? "logins" : undefined}
        userId={userId}
      >
        <span className="text-base font-semibold">
          {user?.lastSeenAt ? <SmartDateTime date={user.lastSeenAt} withTime={true} /> : "-"}
        </span>
      </ActivityTile>

      <ActivityTile
        label={t`Sessions`}
        loading={sessionsLoading}
        subtitle={totalSessions !== undefined ? <Trans>All-time</Trans> : undefined}
        linkTo={totalSessions !== undefined && totalSessions > 0 ? "sessions" : undefined}
        userId={userId}
      >
        <span className="text-2xl font-semibold tabular-nums">{totalSessions !== undefined ? totalSessions : "-"}</span>
      </ActivityTile>

      {isSupportSystemEnabled && (
        <ActivityTile
          label={t`Support tickets`}
          loading={supportTicketsLoading}
          subtitle={totalTicketCount !== undefined ? <Trans>{totalTicketCount} total</Trans> : undefined}
          linkTo={totalTicketCount !== undefined && totalTicketCount > 0 ? "support-tickets" : undefined}
          userId={userId}
        >
          <span className="text-2xl font-semibold tabular-nums">
            {openTicketCount !== undefined ? openTicketCount : "-"}
          </span>
        </ActivityTile>
      )}
    </div>
  );
}

function ActivityTile({
  label,
  loading,
  subtitle,
  children,
  linkTo,
  userId
}: Readonly<{
  label: string;
  loading: boolean;
  subtitle?: ReactNode;
  children: ReactNode;
  linkTo?: "overview" | "logins" | "sessions" | "support-tickets";
  userId?: string;
}>) {
  const content = (
    <>
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
    </>
  );

  if (linkTo && userId) {
    return (
      <LinkCard
        to="/users/$userId"
        params={{ userId }}
        search={{ tab: linkTo === "overview" ? undefined : linkTo }}
        className="gap-2 rounded-lg p-4 shadow-none"
      >
        {content}
      </LinkCard>
    );
  }

  return <Card className="gap-2 rounded-lg p-4 shadow-none">{content}</Card>;
}

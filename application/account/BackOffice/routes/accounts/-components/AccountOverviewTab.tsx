import { Trans } from "@lingui/react/macro";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { getInitials } from "@repo/utils/string/getInitials";

import type { components } from "@/shared/lib/api/client";

import { api, UserRole } from "@/shared/lib/api/client";

type TenantDetailResponse = components["schemas"]["TenantDetailResponse"];
type TenantUserSummary = components["schemas"]["TenantUserSummary"];

interface AccountOverviewTabProps {
  tenant: TenantDetailResponse | undefined;
  tenantId: string;
  isLoading: boolean;
}

export function AccountOverviewTab({ tenant, tenantId, isLoading }: Readonly<AccountOverviewTabProps>) {
  const formatDate = useFormatDate();

  const activityQuery = api.useQuery("get", "/api/back-office/tenants/{id}/activity", {
    params: { path: { id: tenantId } }
  });

  const ownersQuery = api.useQuery("get", "/api/back-office/tenants/{id}/users", {
    params: { path: { id: tenantId }, query: { Role: UserRole.Owner, PageSize: 100 } }
  });

  const owners = ownersQuery.data?.users ?? [];
  const events = activityQuery.data?.events ?? [];

  return (
    <div className="grid grid-cols-1 gap-6 md:grid-cols-3">
      <section className="md:col-span-2">
        <h3 className="mb-3 text-base font-semibold">
          <Trans>Activity</Trans>
        </h3>
        {activityQuery.isLoading ? (
          <ActivitySkeleton />
        ) : events.length === 0 ? (
          <Empty className="border">
            <EmptyHeader>
              <EmptyTitle>
                <Trans>No activity yet</Trans>
              </EmptyTitle>
              <EmptyDescription>
                <Trans>No activity recorded yet.</Trans>
              </EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : (
          <ol className="flex flex-col gap-3">
            {events.map((event, index) => (
              <li
                key={`${event.timestamp}-${index}`}
                className="flex items-start gap-3 rounded-md border border-border bg-card p-3"
              >
                <div className="mt-1 size-2 shrink-0 rounded-full bg-primary" aria-hidden={true} />
                <div className="flex min-w-0 flex-1 flex-col">
                  <span className="font-medium text-foreground">{event.name}</span>
                  {event.description && <span className="text-sm text-muted-foreground">{event.description}</span>}
                  <span className="text-xs text-muted-foreground">{formatDate(event.timestamp, true)}</span>
                </div>
              </li>
            ))}
          </ol>
        )}
      </section>

      <section>
        <h3 className="mb-3 text-base font-semibold">
          <Trans>Owners</Trans>
        </h3>
        {ownersQuery.isLoading || isLoading || !tenant ? (
          <OwnersSkeleton />
        ) : owners.length === 0 ? (
          <Empty className="border">
            <EmptyHeader>
              <EmptyTitle>
                <Trans>No owners</Trans>
              </EmptyTitle>
              <EmptyDescription>
                <Trans>No owners on this account.</Trans>
              </EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : (
          <div className="flex flex-col gap-2">
            {owners.map((owner) => (
              <OwnerRow key={owner.id} owner={owner} />
            ))}
          </div>
        )}
      </section>
    </div>
  );
}

function OwnerRow({ owner }: Readonly<{ owner: TenantUserSummary }>) {
  const displayName =
    owner.firstName || owner.lastName ? `${owner.firstName ?? ""} ${owner.lastName ?? ""}`.trim() : owner.email;

  return (
    <div className="flex items-center gap-3 rounded-md border border-border bg-card p-3">
      <Avatar size="sm">
        <AvatarImage src={owner.avatarUrl ?? undefined} alt="" />
        <AvatarFallback>
          {getInitials(owner.firstName ?? undefined, owner.lastName ?? undefined, owner.email)}
        </AvatarFallback>
      </Avatar>
      <div className="flex min-w-0 flex-1 flex-col">
        <span className="truncate text-sm font-medium">{displayName}</span>
        <span className="truncate text-xs text-muted-foreground">{owner.email}</span>
      </div>
      {!owner.emailConfirmed && (
        <Badge variant="outline" className="shrink-0">
          <Trans>Pending</Trans>
        </Badge>
      )}
    </div>
  );
}

function ActivitySkeleton() {
  return (
    <div className="flex flex-col gap-3">
      {Array.from({ length: 4 }).map((_, index) => (
        <Skeleton key={`activity-skeleton-${index}`} className="h-16 w-full" />
      ))}
    </div>
  );
}

function OwnersSkeleton() {
  return (
    <div className="flex flex-col gap-2">
      {Array.from({ length: 3 }).map((_, index) => (
        <Skeleton key={`owners-skeleton-${index}`} className="h-14 w-full" />
      ))}
    </div>
  );
}

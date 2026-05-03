import { Trans } from "@lingui/react/macro";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Card } from "@repo/ui/components/Card";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
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
  const ownersQuery = api.useQuery("get", "/api/back-office/tenants/{id}/users", {
    params: { path: { id: tenantId }, query: { Role: UserRole.Owner, PageSize: 100 } }
  });

  const owners = ownersQuery.data?.users ?? [];

  return (
    <section>
      <h4 className="mb-3">
        <Trans>Owners</Trans>
      </h4>
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
        <div className="grid auto-rows-fr grid-cols-1 gap-2 md:grid-cols-2">
          {owners.map((owner) => (
            <OwnerRow key={owner.id} owner={owner} />
          ))}
        </div>
      )}
    </section>
  );
}

function OwnerRow({ owner }: Readonly<{ owner: TenantUserSummary }>) {
  const displayName =
    owner.firstName || owner.lastName ? `${owner.firstName ?? ""} ${owner.lastName ?? ""}`.trim() : owner.email;

  return (
    <Card className="h-full flex-row items-center gap-3 rounded-md p-3 py-3 shadow-none">
      <Avatar size="lg">
        <AvatarImage src={owner.avatarUrl ?? undefined} alt="" />
        <AvatarFallback>
          {getInitials(owner.firstName ?? undefined, owner.lastName ?? undefined, owner.email)}
        </AvatarFallback>
      </Avatar>
      <div className="flex min-w-0 flex-1 flex-col justify-center leading-tight">
        <span className="truncate text-sm font-medium">{displayName}</span>
        {owner.title && <span className="truncate text-xs text-muted-foreground">{owner.title}</span>}
        <span className="truncate text-xs text-muted-foreground">{owner.email}</span>
      </div>
      {!owner.emailConfirmed && (
        <Badge variant="outline" className="shrink-0">
          <Trans>Pending</Trans>
        </Badge>
      )}
    </Card>
  );
}

function OwnersSkeleton() {
  return (
    <div className="grid grid-cols-1 gap-2 md:grid-cols-2">
      {Array.from({ length: 2 }).map((_, index) => (
        <Skeleton key={`owners-skeleton-${index}`} className="h-16 w-full" />
      ))}
    </div>
  );
}

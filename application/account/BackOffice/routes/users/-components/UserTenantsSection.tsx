import { plural } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Card } from "@repo/ui/components/Card";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { getCountryFlagEmoji, getCountryName } from "@repo/ui/utils/countryFlag";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { Link } from "@tanstack/react-router";
import { Building2Icon, ChevronRightIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { TenantStatusBadge } from "@/routes/accounts/-components/TenantStatusBadge";
import { getSubscriptionPlanLabel, getUserRoleLabel } from "@/shared/lib/api/labels";
import { getSubscriptionPlanBadgeClass } from "@/shared/lib/planBadge";

import { UserSectionHeader } from "./UserSectionHeader";

type BackOfficeUserDetailResponse = components["schemas"]["BackOfficeUserDetailResponse"];
type Membership = BackOfficeUserDetailResponse["tenantMemberships"][number];

interface UserTenantsSectionProps {
  user: BackOfficeUserDetailResponse | undefined;
}

export function UserTenantsSection({ user }: Readonly<UserTenantsSectionProps>) {
  const membershipCount = user?.tenantMemberships.length ?? 0;
  return (
    <section className="flex flex-col gap-3">
      <UserSectionHeader
        icon={Building2Icon}
        title={<Trans>Accounts</Trans>}
        description={
          user
            ? plural(membershipCount, {
                one: "# membership",
                other: "# memberships"
              })
            : null
        }
      />
      {!user ? (
        <Skeleton className="h-24 w-full" />
      ) : user.tenantMemberships.length === 0 ? (
        <Empty className="border bg-card">
          <EmptyHeader>
            <EmptyTitle>
              <Trans>No account memberships</Trans>
            </EmptyTitle>
            <EmptyDescription>
              <Trans>This user is not a member of any account.</Trans>
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <div className="grid auto-rows-fr grid-cols-1 gap-2 xl:grid-cols-2">
          {user.tenantMemberships.map((membership) => (
            <MembershipCard key={membership.tenantId} membership={membership} />
          ))}
        </div>
      )}
    </section>
  );
}

function MembershipCard({ membership }: Readonly<{ membership: Membership }>) {
  const { i18n } = useLingui();
  const mrr =
    membership.monthlyRecurringRevenue !== null && membership.currency !== null
      ? formatCurrency(membership.monthlyRecurringRevenue, membership.currency)
      : null;
  return (
    <Card className="h-full overflow-hidden p-0 shadow-none">
      <Link
        to="/accounts/$tenantId"
        params={{ tenantId: membership.tenantId }}
        className="flex h-full items-center gap-4 p-4 hover:bg-accent active:bg-accent"
      >
        <TenantLogo logoUrl={membership.tenantLogoUrl} tenantName={membership.tenantName} size="lg" />
        <div className="flex min-w-0 flex-1 flex-col gap-1">
          <div className="flex items-center gap-2">
            <span className="truncate font-medium">{membership.tenantName}</span>
          </div>
          {membership.country && (
            <span className="flex items-center gap-1 text-sm text-muted-foreground">
              <span aria-hidden="true">{getCountryFlagEmoji(membership.country)}</span>
              <span>{getCountryName(membership.country, i18n.locale)}</span>
            </span>
          )}
          <div className="flex flex-wrap items-center gap-2">
            <Badge className={`w-fit ${getSubscriptionPlanBadgeClass(membership.plan)}`}>
              {getSubscriptionPlanLabel(membership.plan)}
            </Badge>
            <TenantStatusBadge
              plan={membership.plan}
              plannedChange={membership.plannedChange}
              hasEverSubscribed={membership.hasEverSubscribed}
            />
            <Badge variant="outline">{getUserRoleLabel(membership.role)}</Badge>
            {!membership.emailConfirmed && (
              <Badge variant="outline" className="border-amber-500/30 text-amber-600">
                <Trans>Email pending</Trans>
              </Badge>
            )}
          </div>
        </div>
        {mrr && (
          <span className="flex shrink-0 flex-col items-end leading-tight text-muted-foreground tabular-nums">
            <span className="text-sm">{mrr}</span>
            <span className="text-xs">
              <Trans>/ month</Trans>
            </span>
          </span>
        )}
        <ChevronRightIcon className="size-4 shrink-0 text-muted-foreground" aria-hidden="true" />
      </Link>
    </Card>
  );
}

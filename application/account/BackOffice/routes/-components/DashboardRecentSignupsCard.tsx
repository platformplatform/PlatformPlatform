import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { Link } from "@tanstack/react-router";
import { ArrowRightIcon, BuildingIcon } from "lucide-react";

import { SmartDateTime } from "@/shared/components/SmartDateTime";
import { api } from "@/shared/lib/api/client";
import { getSubscriptionPlanLabel } from "@/shared/lib/api/labels";
import { getCountryFlagEmoji, getCountryName } from "@repo/ui/utils/countryFlag";

import { DashboardCardShell } from "./DashboardCardShell";

export function DashboardRecentSignupsCard() {
  const { i18n } = useLingui();
  const { data, isLoading } = api.useQuery("get", "/api/back-office/dashboard/recent-signups", {
    params: { query: { Limit: 6 } }
  });

  const signups = data?.signups ?? [];

  return (
    <DashboardCardShell
      title={<Trans>Recent signups</Trans>}
      action={
        <Link to="/accounts" className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <Trans>View all</Trans>
          <ArrowRightIcon className="size-3.5" aria-hidden="true" />
        </Link>
      }
    >
      {isLoading ? (
        <div className="flex flex-col gap-3">
          {[0, 1, 2, 3, 4, 5].map((index) => (
            <Skeleton key={index} className="h-12 w-full" />
          ))}
        </div>
      ) : signups.length === 0 ? (
        <Empty className="border bg-card">
          <EmptyHeader>
            <BuildingIcon className="size-6 text-muted-foreground" aria-hidden="true" />
            <EmptyTitle>
              <Trans>No recent signups</Trans>
            </EmptyTitle>
            <EmptyDescription>
              <Trans>New accounts will appear here as they sign up.</Trans>
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <ul className="flex flex-col">
          {signups.map((signup) => (
            <li key={signup.tenantId} className="border-b last:border-b-0">
              <Link
                to="/accounts/$tenantId"
                params={{ tenantId: String(signup.tenantId) }}
                className="-mx-2 flex items-center gap-3 rounded-md px-2 py-2.5 hover:bg-accent active:bg-accent"
              >
                <TenantLogo logoUrl={signup.tenantLogoUrl} tenantName={signup.name} size="md" className="size-10" />
                <div className="flex flex-1 flex-col">
                  <span className="text-sm font-medium">{signup.name}</span>
                  <span className="flex items-center gap-1.5 text-xs text-muted-foreground">
                    {signup.country && (
                      <>
                        <span aria-hidden="true">{getCountryFlagEmoji(signup.country)}</span>
                        <span>{getCountryName(signup.country, i18n.locale)}</span>
                        <span aria-hidden="true">·</span>
                      </>
                    )}
                    <Badge variant="secondary" className="text-xs">
                      {getSubscriptionPlanLabel(signup.plan)}
                    </Badge>
                  </span>
                </div>
                <SmartDateTime date={signup.createdAt} className="text-xs text-muted-foreground" />
              </Link>
            </li>
          ))}
        </ul>
      )}
    </DashboardCardShell>
  );
}

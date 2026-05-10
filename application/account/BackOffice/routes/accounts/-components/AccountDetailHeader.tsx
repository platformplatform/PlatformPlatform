import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { getCountryFlagEmoji, getCountryName } from "@repo/ui/utils/countryFlag";
import { CalendarIcon, HashIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { PlannedSubscriptionChange, TenantState } from "@/shared/lib/api/client";
import { getSubscriptionPlanLabel, getTenantStateLabel } from "@/shared/lib/api/labels";
import { getSubscriptionPlanBadgeClass } from "@/shared/lib/planBadge";

import { AccountActionsMenu } from "./AccountActionsMenu";
import { TenantStatusBadge } from "./TenantStatusBadge";

type TenantDetailResponse = components["schemas"]["TenantDetailResponse"];

interface AccountDetailHeaderProps {
  tenant: TenantDetailResponse | undefined;
  tenantId: string;
  isLoading: boolean;
}

export function AccountDetailHeader({ tenant, tenantId, isLoading }: Readonly<AccountDetailHeaderProps>) {
  const formatDate = useFormatDate();
  const { i18n } = useLingui();

  return (
    <div className="flex items-center gap-4">
      <TenantLogo logoUrl={tenant?.logoUrl} tenantName={tenant?.name ?? ""} size="lg" />
      <div className="flex min-w-0 flex-1 flex-col justify-center gap-1 self-center">
        {isLoading || !tenant ? (
          <>
            <Skeleton className="h-7 w-64" />
            <Skeleton className="h-4 w-48" />
          </>
        ) : (
          <>
            <div className="flex flex-wrap items-center gap-2">
              <h1 className="m-0 min-w-0 truncate leading-tight">{tenant.name}</h1>
              <div className="hidden flex-wrap items-center gap-2 md:flex">
                <Badge className={getSubscriptionPlanBadgeClass(tenant.plan)}>
                  {getSubscriptionPlanLabel(tenant.plan)}
                </Badge>
                {tenant.state !== TenantState.Active && <TenantStatePill state={tenant.state} />}
                <TenantStatusBadge
                  plan={tenant.plan}
                  plannedChange={derivePlannedChange(tenant)}
                  hasEverSubscribed={tenant.hasEverSubscribed}
                />
              </div>
            </div>
            <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-muted-foreground">
              <div className="flex flex-wrap items-center gap-2 md:hidden">
                {tenant.state !== TenantState.Active && <TenantStatePill state={tenant.state} />}
                <TenantStatusBadge
                  plan={tenant.plan}
                  plannedChange={derivePlannedChange(tenant)}
                  hasEverSubscribed={tenant.hasEverSubscribed}
                />
              </div>
              {tenant.billingAddress?.country && (
                <span className="inline-flex items-center gap-1.5">
                  <span aria-hidden={true}>{getCountryFlagEmoji(tenant.billingAddress.country)}</span>
                  <span className="hidden md:inline">{getCountryName(tenant.billingAddress.country, i18n.locale)}</span>
                </span>
              )}
              <span className="inline-flex items-center gap-1.5">
                <CalendarIcon className="size-3.5" aria-hidden={true} />
                <Trans>
                  Signed up <span className="md:hidden">{formatDate(tenant.createdAt, false, false, true)}</span>
                  <span className="hidden md:inline">{formatDate(tenant.createdAt)}</span>
                </Trans>
              </span>
              <span className="inline-flex items-center gap-1.5 font-mono">
                <HashIcon className="size-3.5" aria-hidden={true} />
                <span>{tenantId}</span>
              </span>
            </div>
          </>
        )}
      </div>
      <AccountActionsMenu tenantId={tenantId} stripeCustomerUrl={tenant?.stripeCustomerUrl} />
    </div>
  );
}

function derivePlannedChange(tenant: TenantDetailResponse): PlannedSubscriptionChange | null {
  if (tenant.cancelAtPeriodEnd) {
    return PlannedSubscriptionChange.Cancellation;
  }
  if (tenant.scheduledPlan !== null) {
    return PlannedSubscriptionChange.ScheduledPlanChange;
  }
  return null;
}

function TenantStatePill({ state }: Readonly<{ state: TenantState }>) {
  return (
    <Badge variant="outline" className="border-destructive/30 text-destructive">
      {getTenantStateLabel(state)}
    </Badge>
  );
}

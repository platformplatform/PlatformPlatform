import { t } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { getCountryFlagEmoji, getCountryName } from "@repo/ui/utils/countryFlag";
import { Link } from "@tanstack/react-router";
import { ArrowLeftIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { PlannedSubscriptionChange, TenantState } from "@/shared/lib/api/client";
import { getSubscriptionPlanLabel, getTenantStateLabel } from "@/shared/lib/api/labels";
import { getSubscriptionPlanBadgeClass } from "@/shared/lib/planBadge";

import { SyncWithStripeButton } from "./SyncWithStripeButton";
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
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between gap-2">
        <Button
          variant="ghost"
          size="sm"
          className="-ml-2 gap-1.5"
          nativeButton={false}
          render={<Link to="/accounts" aria-label={t`Back to accounts`} />}
        >
          <ArrowLeftIcon className="size-4" />
          <Trans>Accounts</Trans>
        </Button>
        <SyncWithStripeButton tenantId={tenantId} />
      </div>

      <div className="flex flex-wrap items-center gap-4">
        <TenantLogo logoUrl={tenant?.logoUrl} tenantName={tenant?.name ?? ""} size="lg" className="size-20" />
        <div className="flex min-w-0 flex-col justify-center gap-1 self-center">
          {isLoading || !tenant ? (
            <>
              <Skeleton className="h-7 w-64" />
              <Skeleton className="h-4 w-48" />
            </>
          ) : (
            <>
              <div className="flex flex-wrap items-center gap-2">
                <h1 className="min-w-0 truncate">{tenant.name}</h1>
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
              <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-muted-foreground">
                {tenant.billingAddress?.country && (
                  <span className="inline-flex items-center gap-1.5">
                    <span aria-hidden={true}>{getCountryFlagEmoji(tenant.billingAddress.country)}</span>
                    <span>{getCountryName(tenant.billingAddress.country, i18n.locale)}</span>
                  </span>
                )}
                <span>
                  <Trans>Created {formatDate(tenant.createdAt)}</Trans>
                </span>
              </div>
            </>
          )}
        </div>
      </div>
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

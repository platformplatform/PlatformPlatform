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
import { ArrowLeftIcon, CalendarClockIcon, XCircleIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { TenantState } from "@/shared/lib/api/client";
import { getSubscriptionPlanLabel, getTenantStateLabel } from "@/shared/lib/api/labels";
import { getSubscriptionPlanBadgeClass } from "@/shared/lib/planBadge";

type TenantDetailResponse = components["schemas"]["TenantDetailResponse"];

interface AccountDetailHeaderProps {
  tenant: TenantDetailResponse | undefined;
  isLoading: boolean;
}

export function AccountDetailHeader({ tenant, isLoading }: Readonly<AccountDetailHeaderProps>) {
  const formatDate = useFormatDate();
  const { i18n } = useLingui();

  return (
    <div className="flex flex-col gap-4">
      <div>
        <Button
          variant="ghost"
          size="sm"
          className="-ml-2 gap-1.5"
          render={<Link to="/accounts" aria-label={t`Back to accounts`} />}
        >
          <ArrowLeftIcon className="size-4" />
          <Trans>Accounts</Trans>
        </Button>
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
                <TenantStatePill state={tenant.state} />
                {tenant.cancelAtPeriodEnd ? (
                  <Badge variant="destructive" className="gap-1">
                    <XCircleIcon className="size-3" />
                    <Trans>Canceling</Trans>
                  </Badge>
                ) : tenant.scheduledPlan !== null ? (
                  <Badge variant="warning" className="gap-1">
                    <CalendarClockIcon className="size-3" />
                    <Trans>Downgrading</Trans>
                  </Badge>
                ) : null}
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

function TenantStatePill({ state }: Readonly<{ state: TenantState }>) {
  const variantClass =
    state === TenantState.Active
      ? "border-emerald-500/30 text-emerald-700 dark:text-emerald-300"
      : "border-destructive/30 text-destructive";
  return (
    <Badge variant="outline" className={variantClass}>
      {getTenantStateLabel(state)}
    </Badge>
  );
}

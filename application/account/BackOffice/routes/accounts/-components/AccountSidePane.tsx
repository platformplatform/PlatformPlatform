import { t } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { SidePane, SidePaneBody, SidePaneFooter, SidePaneHeader } from "@repo/ui/components/SidePane";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { useDebounce } from "@repo/ui/hooks/useDebounce";
import { getCountryFlagEmoji, getCountryName } from "@repo/ui/utils/countryFlag";
import { useNavigate } from "@tanstack/react-router";
import { ArrowRightIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { api } from "@/shared/lib/api/client";
import { getSubscriptionPlanLabel } from "@/shared/lib/api/labels";
import { getSubscriptionPlanBadgeClass } from "@/shared/lib/planBadge";

import { AccountSidePaneSections } from "./AccountSidePaneSections";
import { TenantStatusBadge } from "./TenantStatusBadge";

type TenantSummary = components["schemas"]["TenantSummary"];

interface AccountSidePaneProps {
  tenant: TenantSummary | null;
  isOpen: boolean;
  onClose: () => void;
}

const DETAIL_DEBOUNCE_MS = 200;

export function AccountSidePane({ tenant, isOpen, onClose }: Readonly<AccountSidePaneProps>) {
  const navigate = useNavigate();
  const { i18n } = useLingui();

  const tenantId = tenant?.id;
  const debouncedTenantId = useDebounce(tenantId, DETAIL_DEBOUNCE_MS);
  const detailReady = Boolean(debouncedTenantId) && debouncedTenantId === tenantId;

  const detailQuery = api.useQuery(
    "get",
    "/api/back-office/tenants/{id}",
    { params: { path: { id: debouncedTenantId ?? "" } } },
    { enabled: detailReady }
  );

  const detail = detailQuery.data;

  const handleOpen = () => {
    if (!tenant) {
      return;
    }
    navigate({ to: "/accounts/$tenantId", params: { tenantId: tenant.id } });
  };

  return (
    <SidePane
      isOpen={isOpen}
      onOpenChange={(open) => !open && onClose()}
      trackingTitle="Account preview"
      trackingKey={tenant?.id}
      aria-label={t`Account preview`}
    >
      <SidePaneHeader closeButtonLabel={t`Close account preview`} className="h-auto py-4">
        {tenant ? (
          <div className="flex min-w-0 flex-1 translate-y-[0.875rem] items-center gap-3 pr-10">
            <TenantLogo logoUrl={detail?.logoUrl} tenantName={tenant.name} size="lg" />
            <div className="flex min-w-0 flex-1 flex-col gap-1 leading-tight">
              <span className="min-w-0 truncate">{tenant.name}</span>
              <span className="inline-flex items-center gap-2 text-xs font-normal text-muted-foreground">
                <Badge className={`${getSubscriptionPlanBadgeClass(tenant.plan)}`}>
                  {getSubscriptionPlanLabel(tenant.plan)}
                </Badge>
                <TenantStatusBadge
                  plan={tenant.plan}
                  plannedChange={tenant.plannedChange}
                  hasEverSubscribed={tenant.hasEverSubscribed}
                />
                {tenant.country && (
                  <span className="inline-flex items-center gap-1">
                    <span aria-hidden={true}>{getCountryFlagEmoji(tenant.country)}</span>
                    <span className="truncate">{getCountryName(tenant.country, i18n.locale)}</span>
                  </span>
                )}
              </span>
            </div>
          </div>
        ) : (
          <Trans>Account</Trans>
        )}
      </SidePaneHeader>

      <SidePaneBody className="pt-7">
        {tenant && (
          <AccountSidePaneSections
            tenant={tenant}
            detail={detail ?? null}
            detailLoading={!detailReady || detailQuery.isLoading}
            debouncedTenantId={debouncedTenantId ?? ""}
            detailReady={detailReady}
          />
        )}
      </SidePaneBody>

      <SidePaneFooter>
        <Button onClick={handleOpen} className="w-full justify-center" disabled={!tenant}>
          <Trans>Open account</Trans>
          <ArrowRightIcon className="size-4" />
        </Button>
      </SidePaneFooter>
    </SidePane>
  );
}

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { SidePane, SidePaneBody, SidePaneFooter, SidePaneHeader } from "@repo/ui/components/SidePane";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { useDebounce } from "@repo/ui/hooks/useDebounce";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { useNavigate } from "@tanstack/react-router";
import { ArrowRightIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { api } from "@/shared/lib/api/client";
import { getCountryFlagEmoji } from "@repo/ui/utils/countryFlag";

import { AccountSidePaneSections } from "./AccountSidePaneSections";

type TenantSummary = components["schemas"]["TenantSummary"];

interface AccountSidePaneProps {
  tenant: TenantSummary | null;
  isOpen: boolean;
  onClose: () => void;
}

const DETAIL_DEBOUNCE_MS = 2000;

export function AccountSidePane({ tenant, isOpen, onClose }: Readonly<AccountSidePaneProps>) {
  const navigate = useNavigate();
  const formatDate = useFormatDate();

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
      <SidePaneHeader className="h-auto py-3" closeButtonLabel={t`Close account preview`}>
        {tenant ? (
          <div className="flex min-w-0 items-center gap-3 pr-10">
            <TenantLogo logoUrl={detail?.logoUrl} tenantName={tenant.name} size="lg" />
            <div className="flex min-w-0 flex-col gap-0.5">
              <span className="truncate font-semibold">{tenant.name}</span>
              <span className="flex items-center gap-2 text-sm font-normal text-muted-foreground">
                <span>{formatDate(tenant.createdAt)}</span>
                {tenant.country && (
                  <span aria-label={tenant.country} className="leading-none">
                    {getCountryFlagEmoji(tenant.country)}
                  </span>
                )}
              </span>
            </div>
          </div>
        ) : (
          <Trans>Account</Trans>
        )}
      </SidePaneHeader>

      <SidePaneBody>
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

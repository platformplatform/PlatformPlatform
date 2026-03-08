import type { useUserInfo } from "@repo/infrastructure/auth/hooks";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { trackInteraction } from "@repo/infrastructure/applicationInsights/ApplicationInsightsProvider";
import {
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger
} from "@repo/ui/components/DropdownMenu";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { ArrowRightLeftIcon, Check } from "lucide-react";

import type { TenantInfo } from "../common/tenantUtils";

interface TenantSwitcherProps {
  sortedTenants: TenantInfo[];
  currentTenantId: string | undefined;
  isLoadingTenants: boolean;
  userInfo: NonNullable<ReturnType<typeof useUserInfo>>;
  onTenantSwitch: (tenant: TenantInfo) => void;
}

export function TenantSwitcher({
  sortedTenants,
  currentTenantId,
  isLoadingTenants,
  userInfo,
  onTenantSwitch
}: Readonly<TenantSwitcherProps>) {
  if (sortedTenants.length <= 1) {
    return null;
  }

  return (
    <DropdownMenuSub
      onOpenChange={(open) => {
        if (open) {
          trackInteraction("Switch account", "menu", "Open");
        }
      }}
    >
      <DropdownMenuSubTrigger aria-label={t`Switch account`}>
        <ArrowRightLeftIcon className="size-5" />
        <Trans>Switch account</Trans>
      </DropdownMenuSubTrigger>
      <DropdownMenuSubContent className="w-fit min-w-56">
        <DropdownMenuGroup>
          <DropdownMenuLabel>
            <Trans>Select account</Trans>
          </DropdownMenuLabel>
        </DropdownMenuGroup>
        <DropdownMenuSeparator />
        {isLoadingTenants ? (
          <DropdownMenuGroup>
            <DropdownMenuLabel>
              <Trans>Loading...</Trans>
            </DropdownMenuLabel>
          </DropdownMenuGroup>
        ) : (
          sortedTenants.map((tenant) => (
            <DropdownMenuItem key={tenant.tenantId} onClick={() => onTenantSwitch(tenant)}>
              <TenantLogo logoUrl={tenant.logoUrl} tenantName={tenant.tenantName || ""} />
              <div className="flex flex-1 items-center justify-between gap-2">
                <div className="flex flex-col">
                  <span className="whitespace-nowrap">{tenant.tenantName || t`Unnamed account`}</span>
                  <span className="text-xs whitespace-nowrap text-muted-foreground">{userInfo?.email}</span>
                </div>
                <Check className={`ml-2 size-4 shrink-0 ${tenant.tenantId === currentTenantId ? "" : "invisible"}`} />
              </div>
            </DropdownMenuItem>
          ))
        )}
      </DropdownMenuSubContent>
    </DropdownMenuSub>
  );
}

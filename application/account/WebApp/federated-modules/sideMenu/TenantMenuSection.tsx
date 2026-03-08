import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { hasPermission } from "@repo/infrastructure/auth/routeGuards";
import { Button } from "@repo/ui/components/Button";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { ArrowRightLeftIcon, ChevronDownIcon, CircleUserIcon, CreditCardIcon, HomeIcon, UsersIcon } from "lucide-react";
import { useState } from "react";

import { sortTenants, type TenantInfo } from "../common/tenantUtils";
import { menuItemBaseClassName, menuItemClassName } from "./menuUtils";

export function TenantMenuSection({
  tenants,
  onOpenTenantSwitcher,
  pathname,
  navigateTo
}: {
  tenants: TenantInfo[];
  onOpenTenantSwitcher: () => void;
  pathname: string;
  navigateTo: (path: string) => void;
}) {
  const userInfo = useUserInfo();
  const [isTenantExpanded, setIsTenantExpanded] = useState(pathname.startsWith("/account"));

  const currentTenantId = userInfo?.tenantId;
  const canAccessAccountSettings = hasPermission({ allowedRoles: ["Owner", "Admin"] });
  const hasMultipleTenants = sortTenants(tenants).length > 1;
  const hasSubscription = userInfo?.role === "Owner" && import.meta.runtime_env.PUBLIC_SUBSCRIPTION_ENABLED === "true";
  const hasTenantActions = hasMultipleTenants || canAccessAccountSettings || hasSubscription;
  const currentTenant = tenants.find((tenant) => tenant.tenantId === currentTenantId);
  const currentTenantName = currentTenant?.tenantName || userInfo?.tenantName || "PlatformPlatform";
  const currentTenantNameForLogo = currentTenant?.tenantName || userInfo?.tenantName || "";
  const currentTenantLogoUrl = currentTenant ? currentTenant.logoUrl : userInfo?.tenantLogoUrl;

  if (!hasTenantActions) return null;

  return (
    <>
      <Button
        variant="ghost"
        onClick={() => setIsTenantExpanded(!isTenantExpanded)}
        className="flex h-14 w-full items-center justify-start gap-3 rounded-md py-2 pr-3 pl-2 text-sm font-normal hover:bg-hover-background active:bg-hover-background"
        aria-expanded={isTenantExpanded}
      >
        <div className="flex size-8 shrink-0 items-center justify-center">
          <TenantLogo logoUrl={currentTenantLogoUrl} tenantName={currentTenantNameForLogo} />
        </div>
        <div className="min-w-0 flex-1 overflow-hidden text-left font-medium text-ellipsis whitespace-nowrap text-foreground">
          {currentTenantName}
        </div>
        <ChevronDownIcon
          className={`size-4 shrink-0 text-muted-foreground transition-transform duration-150 ${isTenantExpanded ? "rotate-180" : ""}`}
        />
      </Button>

      {isTenantExpanded && (
        <div className="flex flex-col">
          {hasMultipleTenants && (
            <Button
              variant="ghost"
              onClick={onOpenTenantSwitcher}
              className={`${menuItemBaseClassName} font-normal text-muted-foreground`}
              aria-label={t`Switch account`}
            >
              <div className="flex size-6 shrink-0 items-center justify-center">
                <ArrowRightLeftIcon className="size-5 stroke-current" />
              </div>
              <Trans>Switch account</Trans>
            </Button>
          )}
          {canAccessAccountSettings && (
            <>
              <Button
                variant="ghost"
                onClick={() => navigateTo("/account")}
                className={menuItemClassName(pathname, "/account")}
                aria-label={t`Account overview`}
              >
                <div className="flex size-6 shrink-0 items-center justify-center">
                  <HomeIcon className="size-5 stroke-current" />
                </div>
                <Trans>Overview</Trans>
              </Button>
              <Button
                variant="ghost"
                onClick={() => navigateTo("/account/settings")}
                className={menuItemClassName(pathname, "/account/settings")}
                aria-label={t`Account settings`}
              >
                <div className="flex size-6 shrink-0 items-center justify-center">
                  <CircleUserIcon className="size-5 stroke-current" />
                </div>
                <Trans>Settings</Trans>
              </Button>
              <Button
                variant="ghost"
                onClick={() => navigateTo("/account/users")}
                className={menuItemClassName(pathname, "/account/users", true)}
                aria-label={t`Users`}
              >
                <div className="flex size-6 shrink-0 items-center justify-center">
                  <UsersIcon className="size-5 stroke-current" />
                </div>
                <Trans>Users</Trans>
              </Button>
            </>
          )}
          {hasSubscription && (
            <Button
              variant="ghost"
              onClick={() => navigateTo("/account/billing")}
              className={menuItemClassName(pathname, "/account/billing", true)}
              aria-label={t`Billing`}
            >
              <div className="flex size-6 shrink-0 items-center justify-center">
                <CreditCardIcon className="size-5 stroke-current" />
              </div>
              <Trans>Billing</Trans>
            </Button>
          )}
        </div>
      )}
    </>
  );
}

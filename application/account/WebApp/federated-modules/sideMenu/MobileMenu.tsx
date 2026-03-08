import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { trackInteraction, useTrackOpen } from "@repo/infrastructure/applicationInsights/ApplicationInsightsProvider";
import { authSyncService, type TenantSwitchedMessage } from "@repo/infrastructure/auth/AuthSyncService";
import { loggedInPath } from "@repo/infrastructure/auth/constants";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { MenuButton, overlayContext, SideMenuSeparator } from "@repo/ui/components/SideMenu";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { LayoutDashboardIcon, MessageCircleQuestion } from "lucide-react";
import { useContext, useEffect, useState } from "react";

import { SupportDialog } from "../common/SupportDialog";
import { SwitchingAccountLoader } from "../common/SwitchingAccountLoader";
import { fetchTenants, switchTenantApi, type TenantInfo } from "../common/tenantUtils";
import { MobileMenuContent } from "./MobileMenuContent";
import { TenantSwitcherDrawer } from "./TenantSwitcherDrawer";

export function MobileMenuDialogs() {
  const userInfo = useUserInfo();
  const [isTenantSwitcherOpen, setIsTenantSwitcherOpen] = useState(false);
  const [isSwitching, setIsSwitching] = useState(false);
  const [isSupportDialogOpen, setIsSupportDialogOpen] = useState(false);
  const [tenants, setTenants] = useState<TenantInfo[]>([]);

  const currentTenantId = userInfo?.tenantId;

  useEffect(() => {
    const handleOpenSupport = () => setIsSupportDialogOpen(true);
    const handleOpenTenantSwitcher = (event: CustomEvent<{ tenants: TenantInfo[] }>) => {
      setTenants(event.detail.tenants);
      setIsTenantSwitcherOpen(true);
    };

    window.addEventListener("open-support-dialog", handleOpenSupport);
    window.addEventListener("open-tenant-switcher", handleOpenTenantSwitcher as EventListener);
    return () => {
      window.removeEventListener("open-support-dialog", handleOpenSupport);
      window.removeEventListener("open-tenant-switcher", handleOpenTenantSwitcher as EventListener);
    };
  }, []);

  const handleTenantSwitch = async (tenant: TenantInfo) => {
    if (tenant.tenantId === currentTenantId || tenant.isNew) {
      return;
    }

    trackInteraction("Switch account", "interaction");
    setIsSwitching(true);
    setIsTenantSwitcherOpen(false);

    try {
      localStorage.setItem("preferred-tenant", tenant.tenantId);
      if (tenant.tenantName) {
        localStorage.setItem(`tenant-name-${tenant.tenantId}`, tenant.tenantName);
      }

      await switchTenantApi(tenant.tenantId);

      if (userInfo?.tenantId && userInfo?.id) {
        const message: Omit<TenantSwitchedMessage, "timestamp"> = {
          type: "TENANT_SWITCHED",
          newTenantId: tenant.tenantId,
          previousTenantId: userInfo.tenantId,
          tenantName: tenant.tenantName || t`Unnamed account`,
          userId: userInfo.id
        };
        authSyncService.broadcast(message);
      }

      const targetPath = window.location.pathname === "/" ? loggedInPath : window.location.pathname;
      window.location.href = targetPath;
    } catch {
      setIsSwitching(false);
    }
  };

  return (
    <>
      <TenantSwitcherDrawer
        isOpen={isTenantSwitcherOpen}
        onOpenChange={setIsTenantSwitcherOpen}
        tenants={tenants}
        currentTenantId={currentTenantId}
        onTenantSwitch={handleTenantSwitch}
      />
      {isSwitching && <SwitchingAccountLoader />}
      <SupportDialog isOpen={isSupportDialogOpen} onOpenChange={setIsSupportDialogOpen} />
    </>
  );
}

export interface MobileMenuProps {
  onNavigate?: (path: string) => void;
}

export default function MobileMenu({ onNavigate }: Readonly<MobileMenuProps>) {
  const userInfo = useUserInfo();
  const overlayCtx = useContext(overlayContext);
  const [tenants, setTenants] = useState<TenantInfo[]>([]);

  useTrackOpen("Mobile menu", "menu");

  useEffect(() => {
    if (userInfo?.isAuthenticated) {
      fetchTenants()
        .then((response) => setTenants(response.tenants || []))
        .catch(() => setTenants([]));
    }
  }, [userInfo?.isAuthenticated]);

  const handleOpenSupportDialog = () => {
    window.dispatchEvent(new CustomEvent("open-support-dialog"));
    setTimeout(() => {
      if (overlayCtx?.isOpen) {
        overlayCtx.close();
      }
    }, 100);
  };

  const handleOpenTenantSwitcher = () => {
    trackInteraction("Switch account", "menu", "Open");
    window.dispatchEvent(new CustomEvent("open-tenant-switcher", { detail: { tenants } }));
    setTimeout(() => {
      if (overlayCtx?.isOpen) {
        overlayCtx.close();
      }
    }, 100);
  };

  const currentTenant = tenants.find((tenant) => tenant.tenantId === userInfo?.tenantId);
  const currentTenantName = currentTenant?.tenantName || userInfo?.tenantName || "PlatformPlatform";
  const currentTenantNameForLogo = currentTenant?.tenantName || userInfo?.tenantName || "";
  const currentTenantLogoUrl = currentTenant ? currentTenant.logoUrl : userInfo?.tenantLogoUrl;

  return (
    <div className="flex h-full flex-col">
      {userInfo?.isAuthenticated && (
        <div className="-mx-3 -mt-5 mb-2 flex items-center justify-center gap-3 bg-muted px-3 py-2.5 dark:bg-transparent">
          <TenantLogo logoUrl={currentTenantLogoUrl} tenantName={currentTenantNameForLogo} size="sm" />
          <h5 className="mb-0 min-w-0 overflow-hidden font-normal text-ellipsis whitespace-nowrap">
            {currentTenantName}
          </h5>
        </div>
      )}
      <div className="flex-1 overflow-x-hidden overflow-y-auto px-1 py-1">
        <MobileMenuContent tenants={tenants} onOpenTenantSwitcher={handleOpenTenantSwitcher} />

        <div className="my-3 border-b border-border" />

        <div className="flex flex-col">
          <SideMenuSeparator>
            <Trans>Navigation</Trans>
          </SideMenuSeparator>
          <MenuButton
            icon={LayoutDashboardIcon}
            label={t`Dashboard`}
            href="/dashboard"
            federatedNavigation={true}
            onNavigate={onNavigate}
          />
        </div>
      </div>

      <div className="absolute bottom-3 left-3 z-10 supports-[bottom:max(0px)]:bottom-[max(0.5rem,calc(env(safe-area-inset-bottom)-0.5rem))]">
        <Tooltip>
          <TooltipTrigger
            render={
              <Button
                variant="ghost"
                size="icon"
                aria-label={t`Contact support`}
                className="size-14 rounded-full border border-border bg-background/80 shadow-lg backdrop-blur-sm hover:bg-background/90 active:bg-muted"
                onClick={handleOpenSupportDialog}
              />
            }
          >
            <MessageCircleQuestion className="size-7 text-foreground" />
          </TooltipTrigger>
          <TooltipContent side="right">
            <Trans>Contact support</Trans>
          </TooltipContent>
        </Tooltip>
      </div>
    </div>
  );
}

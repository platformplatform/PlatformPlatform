import { t } from "@lingui/core/macro";
import { useTrackOpen } from "@repo/infrastructure/applicationInsights/ApplicationInsightsProvider";
import { authSyncService } from "@repo/infrastructure/auth/AuthSyncService";
import { loginPath } from "@repo/infrastructure/auth/constants";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { hasPermission } from "@repo/infrastructure/auth/routeGuards";
import { createLoginUrlWithReturnPath } from "@repo/infrastructure/auth/util";
import { DropdownMenu, DropdownMenuContent, DropdownMenuTrigger } from "@repo/ui/components/DropdownMenu";
import { collapsedContext, overlayContext } from "@repo/ui/components/SideMenu";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { getRootFontSize, SIDE_MENU_DEFAULT_WIDTH_REM } from "@repo/ui/utils/responsive";
import { useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { ChevronsUpDownIcon } from "lucide-react";
import { useContext, useEffect, useState } from "react";

import { MainNavigationContext } from "@/shared/hooks/useMainNavigation";

import { SupportDialog } from "../common/SupportDialog";
import { SwitchingAccountLoader } from "../common/SwitchingAccountLoader";
import { logoutApi } from "../common/tenantUtils";
import { MobileMenuDialogs } from "../sideMenu/MobileMenu";
import { UserMenuDropdownContent } from "./UserMenuDropdownContent";
import { useSidebarWidth } from "./useSidebarWidth";
import { useUserMenuTenants } from "./useUserMenuTenants";

interface UserMenuProps {
  isCollapsed?: boolean;
}

export default function UserMenu({ isCollapsed: isCollapsedProp }: Readonly<UserMenuProps>) {
  const userInfo = useUserInfo();
  const isCollapsedContext = useContext(collapsedContext);
  const isCollapsed = isCollapsedProp ?? isCollapsedContext;
  const overlayCtx = useContext(overlayContext);
  const navigateToMain = useContext(MainNavigationContext);
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isSupportDialogOpen, setIsSupportDialogOpen] = useState(false);

  const sidebarWidth = useSidebarWidth(isCollapsed);
  const canAccessAccountSettings = hasPermission({ allowedRoles: ["Owner", "Admin"] });

  useTrackOpen("User menu", "menu", isMenuOpen);

  const { sortedTenants, currentTenant, currentTenantId, isLoadingTenants, isSwitching, handleTenantSwitch } =
    useUserMenuTenants(isMenuOpen, userInfo);

  useEffect(() => {
    window.dispatchEvent(new CustomEvent("tenant-menu-toggle", { detail: { isOpen: isMenuOpen } }));
  }, [isMenuOpen]);

  if (!userInfo?.isAuthenticated) {
    return null;
  }

  const currentTenantName = currentTenant?.tenantName || userInfo.tenantName || "PlatformPlatform";
  const currentTenantNameForLogo = currentTenant?.tenantName || userInfo.tenantName || "";
  const currentTenantLogoUrl = currentTenant ? currentTenant.logoUrl : userInfo.tenantLogoUrl;
  const isAccountContext = navigateToMain !== null;

  const closeMenuAndOverlay = () => {
    setIsMenuOpen(false);
    if (overlayCtx?.isOpen) {
      overlayCtx.close();
    }
  };

  const handleNavigateBackToApp = () => {
    closeMenuAndOverlay();
    if (navigateToMain) {
      navigateToMain("/dashboard");
    }
  };

  const handleShowSupport = () => {
    closeMenuAndOverlay();
    setTimeout(() => {
      setIsSupportDialogOpen(true);
    }, 100);
  };

  const handleNavigateToAccountSettings = () => {
    closeMenuAndOverlay();
    navigate({ to: "/account/settings" });
  };

  const handleNavigateToPreferences = () => {
    closeMenuAndOverlay();
    navigate({ to: "/user/preferences" });
  };

  const handleNavigateToProfile = () => {
    if (overlayCtx?.isOpen) {
      overlayCtx.close();
    }
    navigate({ to: "/user/profile" });
  };

  const handleLogout = async () => {
    await queryClient.cancelQueries();
    queryClient.clear();

    try {
      await logoutApi();

      authSyncService.broadcast({
        type: "USER_LOGGED_OUT",
        userId: userInfo?.id || ""
      });

      // Use window.location.href for logout to ensure a full page reload,
      // clearing all React state and preventing stale queries
      window.location.href = createLoginUrlWithReturnPath(loginPath);
    } catch {
      window.location.href = createLoginUrlWithReturnPath(loginPath);
    }
  };

  const triggerClassName = `relative flex h-11 cursor-pointer items-center gap-0 overflow-visible rounded-md border-0 py-2 font-normal text-sm outline-ring hover:bg-hover-background focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 ${
    isCollapsed ? "ml-[0.375rem] w-11 justify-center" : "w-full pr-2 pl-3"
  } ${isMenuOpen ? "bg-hover-background" : ""}`;

  return (
    <div className="relative w-full px-3">
      <DropdownMenu open={isMenuOpen} onOpenChange={setIsMenuOpen}>
        <DropdownMenuTrigger disabled={isSwitching} className={triggerClassName} aria-label={t`User menu`}>
          <div className="flex size-8 shrink-0 items-center justify-center">
            <TenantLogo logoUrl={currentTenantLogoUrl} tenantName={currentTenantNameForLogo} />
          </div>
          {!isCollapsed && (
            <>
              <div className="ml-3 flex-1 overflow-hidden text-left font-medium text-ellipsis whitespace-nowrap text-foreground">
                {currentTenantName}
              </div>
              <ChevronsUpDownIcon className="ml-2 size-3.5 shrink-0 text-foreground opacity-70" />
            </>
          )}
        </DropdownMenuTrigger>
        <DropdownMenuContent
          align="start"
          side={isCollapsed ? "right" : "bottom"}
          className="w-auto bg-popover"
          style={{ minWidth: `${Math.max(SIDE_MENU_DEFAULT_WIDTH_REM, sidebarWidth / getRootFontSize()) - 1.5}rem` }}
        >
          <UserMenuDropdownContent
            userInfo={userInfo}
            isAccountContext={isAccountContext}
            canAccessAccountSettings={canAccessAccountSettings}
            sortedTenants={sortedTenants}
            currentTenantId={currentTenantId}
            isLoadingTenants={isLoadingTenants}
            onNavigateBackToApp={handleNavigateBackToApp}
            onNavigateToProfile={handleNavigateToProfile}
            onNavigateToPreferences={handleNavigateToPreferences}
            onNavigateToAccountSettings={handleNavigateToAccountSettings}
            onLogout={handleLogout}
            onShowSupport={handleShowSupport}
            onTenantSwitch={handleTenantSwitch}
          />
        </DropdownMenuContent>
      </DropdownMenu>

      <SupportDialog isOpen={isSupportDialogOpen} onOpenChange={setIsSupportDialogOpen} />
      {isSwitching && <SwitchingAccountLoader />}
      <MobileMenuDialogs />
    </div>
  );
}

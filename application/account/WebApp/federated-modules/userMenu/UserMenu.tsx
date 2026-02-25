import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { authSyncService, type TenantSwitchedMessage } from "@repo/infrastructure/auth/AuthSyncService";
import { loggedInPath, loginPath } from "@repo/infrastructure/auth/constants";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { hasPermission } from "@repo/infrastructure/auth/routeGuards";
import { createLoginUrlWithReturnPath } from "@repo/infrastructure/auth/util";
import { enhancedFetch } from "@repo/infrastructure/http/httpClient";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
import { collapsedContext, overlayContext } from "@repo/ui/components/SideMenu";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { getRootFontSize, getSideMenuCollapsedWidth, SIDE_MENU_DEFAULT_WIDTH_REM } from "@repo/ui/utils/responsive";
import { useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import {
  ArrowLeftIcon,
  ArrowRightLeftIcon,
  Check,
  ChevronsUpDownIcon,
  LogOutIcon,
  MailQuestion,
  PencilIcon,
  SettingsIcon,
  SlidersHorizontalIcon
} from "lucide-react";
import { useContext, useEffect, useState } from "react";
import { MainNavigationContext } from "@/shared/hooks/useMainNavigation";
import { SupportDialog } from "../common/SupportDialog";
import { SwitchingAccountLoader } from "../common/SwitchingAccountLoader";

interface TenantInfo {
  tenantId: string;
  tenantName: string | null;
  logoUrl: string | null;
  isNew: boolean;
  userId: string;
}

interface TenantsResponse {
  tenants: TenantInfo[];
}

interface UserMenuProps {
  isCollapsed?: boolean;
}

function sortTenants(tenants: TenantInfo[]): TenantInfo[] {
  return [...tenants].sort((a, b) => {
    if (!a.tenantName && b.tenantName) {
      return 1;
    }
    if (a.tenantName && !b.tenantName) {
      return -1;
    }
    const nameA = a.tenantName || "";
    const nameB = b.tenantName || "";
    return nameA.localeCompare(nameB);
  });
}

function useSidebarWidth(isCollapsed: boolean) {
  const [sidebarWidth, setSidebarWidth] = useState(() => {
    if (isCollapsed) {
      return getSideMenuCollapsedWidth();
    }
    const stored = localStorage.getItem("side-menu-size");
    const widthRem = stored ? Number.parseFloat(stored) : SIDE_MENU_DEFAULT_WIDTH_REM;
    return widthRem * getRootFontSize();
  });

  useEffect(() => {
    const handleResize = (event: CustomEvent<{ widthRem: number }>) => {
      setSidebarWidth(event.detail.widthRem * getRootFontSize());
    };

    const handleToggle = (event: CustomEvent<{ isCollapsed: boolean }>) => {
      if (event.detail.isCollapsed) {
        setSidebarWidth(getSideMenuCollapsedWidth());
      } else {
        const stored = localStorage.getItem("side-menu-size");
        const widthRem = stored ? Number.parseFloat(stored) : SIDE_MENU_DEFAULT_WIDTH_REM;
        setSidebarWidth(widthRem * getRootFontSize());
      }
    };

    window.addEventListener("side-menu-resize", handleResize as EventListener);
    window.addEventListener("side-menu-toggle", handleToggle as EventListener);

    return () => {
      window.removeEventListener("side-menu-resize", handleResize as EventListener);
      window.removeEventListener("side-menu-toggle", handleToggle as EventListener);
    };
  }, []);

  useEffect(() => {
    if (isCollapsed) {
      setSidebarWidth(getSideMenuCollapsedWidth());
    } else {
      const stored = localStorage.getItem("side-menu-size");
      const widthRem = stored ? Number.parseFloat(stored) : SIDE_MENU_DEFAULT_WIDTH_REM;
      setSidebarWidth(widthRem * getRootFontSize());
    }
  }, [isCollapsed]);

  return sidebarWidth;
}

async function fetchTenants(): Promise<TenantsResponse> {
  const response = await enhancedFetch("/api/account/tenants");
  return response.json();
}

async function switchTenantApi(tenantId: string): Promise<void> {
  await enhancedFetch("/api/account/authentication/switch-tenant", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ tenantId })
  });
}

async function logoutApi(): Promise<void> {
  await enhancedFetch("/api/account/authentication/logout", {
    method: "POST"
  });
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
  const [isSwitching, setIsSwitching] = useState(false);
  const [isSupportDialogOpen, setIsSupportDialogOpen] = useState(false);
  const [tenants, setTenants] = useState<TenantInfo[]>([]);
  const [isLoadingTenants, setIsLoadingTenants] = useState(false);
  const [isProfileCardHighlighted, setIsProfileCardHighlighted] = useState(false);

  const sidebarWidth = useSidebarWidth(isCollapsed);
  const canAccessAccountSettings = hasPermission({ allowedRoles: ["Owner", "Admin"] });

  useEffect(() => {
    window.dispatchEvent(new CustomEvent("tenant-menu-toggle", { detail: { isOpen: isMenuOpen } }));
    if (!isMenuOpen) {
      setIsProfileCardHighlighted(false);
    }
  }, [isMenuOpen]);

  useEffect(() => {
    if (isMenuOpen && userInfo?.isAuthenticated) {
      setIsLoadingTenants(true);
      fetchTenants()
        .then((response) => {
          setTenants(response.tenants || []);
        })
        .catch(() => {
          setTenants([]);
        })
        .finally(() => {
          setIsLoadingTenants(false);
        });
    }
  }, [isMenuOpen, userInfo?.isAuthenticated]);

  useEffect(() => {
    const handleTenantUpdated = () => {
      if (userInfo?.isAuthenticated) {
        fetchTenants()
          .then((response) => {
            setTenants(response.tenants || []);
          })
          .catch(() => {});
      }
    };

    window.addEventListener("tenant-updated", handleTenantUpdated);
    return () => window.removeEventListener("tenant-updated", handleTenantUpdated);
  }, [userInfo?.isAuthenticated]);

  if (!userInfo?.isAuthenticated) {
    return null;
  }

  const currentTenantId = userInfo.tenantId;
  const acceptedTenants = tenants.filter((t) => !t.isNew);
  const sortedTenants = sortTenants(acceptedTenants);
  const currentTenant = tenants.find((t) => t.tenantId === currentTenantId);
  const currentTenantName = currentTenant?.tenantName || userInfo.tenantName || "PlatformPlatform";
  const currentTenantNameForLogo = currentTenant?.tenantName || userInfo.tenantName || "";
  const currentTenantLogoUrl = currentTenant ? currentTenant.logoUrl : userInfo.tenantLogoUrl;
  const isAccountContext = navigateToMain !== null;

  const handleNavigateBackToApp = () => {
    setIsMenuOpen(false);
    if (overlayCtx?.isOpen) {
      overlayCtx.close();
    }
    if (navigateToMain) {
      navigateToMain("/dashboard");
    }
  };

  const handleTenantSwitch = async (tenant: TenantInfo) => {
    if (tenant.tenantId === currentTenantId) {
      return;
    }

    setIsSwitching(true);
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

  const handleShowSupport = () => {
    setIsMenuOpen(false);
    if (overlayCtx?.isOpen) {
      overlayCtx.close();
    }
    setTimeout(() => {
      setIsSupportDialogOpen(true);
    }, 100);
  };

  const handleNavigateToAccountSettings = () => {
    setIsMenuOpen(false);
    if (overlayCtx?.isOpen) {
      overlayCtx.close();
    }
    navigate({ to: "/account/settings" });
  };

  const handleNavigateToPreferences = () => {
    setIsMenuOpen(false);
    if (overlayCtx?.isOpen) {
      overlayCtx.close();
    }
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
              <div className="ml-3 flex-1 overflow-hidden text-ellipsis whitespace-nowrap text-left font-medium text-foreground">
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
          {isAccountContext && (
            <>
              <DropdownMenuItem onClick={handleNavigateBackToApp} aria-label={t`Back to app`}>
                <ArrowLeftIcon className="size-5" />
                <Trans>Back to app</Trans>
              </DropdownMenuItem>
              <DropdownMenuSeparator />
            </>
          )}
          <DropdownMenuGroup>
            <DropdownMenuItem
              onClick={handleNavigateToProfile}
              className="flex flex-col items-center gap-1 px-4 py-3"
              aria-label={t`Edit user profile`}
              onMouseEnter={() => setIsProfileCardHighlighted(true)}
              onMouseLeave={() => setIsProfileCardHighlighted(false)}
            >
              <div className="relative">
                <Avatar className="size-16">
                  <AvatarImage src={userInfo.avatarUrl ?? undefined} />
                  <AvatarFallback className="text-xl">{userInfo.initials ?? ""}</AvatarFallback>
                </Avatar>
                <div
                  className="[&_*]:!text-inherit pointer-events-none absolute -right-0.5 -bottom-0.5 flex size-5 items-center justify-center rounded-full border border-border"
                  style={{
                    backgroundColor: isProfileCardHighlighted ? "var(--color-primary)" : "var(--color-popover)",
                    color: isProfileCardHighlighted
                      ? "var(--color-primary-foreground)"
                      : "var(--color-muted-foreground)"
                  }}
                >
                  <PencilIcon className="size-2.5" strokeWidth={3} />
                </div>
              </div>
              <span className="font-medium">{userInfo.fullName}</span>
              <span className="text-muted-foreground text-sm group-focus/dropdown-menu-item:hidden">
                {userInfo.email}
              </span>
              <span className="hidden text-sm group-focus/dropdown-menu-item:inline">
                <Trans>Edit profile</Trans>
              </span>
            </DropdownMenuItem>
          </DropdownMenuGroup>

          <DropdownMenuItem onClick={handleNavigateToPreferences} aria-label={t`Change user preferences`}>
            <SlidersHorizontalIcon className="size-5" />
            <Trans>Preferences</Trans>
          </DropdownMenuItem>

          <DropdownMenuItem onClick={handleLogout} aria-label={t`Log out`}>
            <LogOutIcon className="size-5" />
            <Trans>Log out</Trans>
          </DropdownMenuItem>

          {(canAccessAccountSettings || sortedTenants.length > 1) && <DropdownMenuSeparator />}

          {(canAccessAccountSettings || sortedTenants.length > 1) && (
            <DropdownMenuGroup>
              {sortedTenants.length > 1 && (
                <DropdownMenuSub>
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
                        <DropdownMenuItem key={tenant.tenantId} onClick={() => handleTenantSwitch(tenant)}>
                          <TenantLogo logoUrl={tenant.logoUrl} tenantName={tenant.tenantName || ""} />
                          <div className="flex flex-1 items-center justify-between gap-2">
                            <div className="flex flex-col">
                              <span className="whitespace-nowrap">{tenant.tenantName || t`Unnamed account`}</span>
                              <span className="whitespace-nowrap text-muted-foreground text-xs">{userInfo?.email}</span>
                            </div>
                            <Check
                              className={`ml-2 size-4 shrink-0 ${tenant.tenantId === currentTenantId ? "" : "invisible"}`}
                            />
                          </div>
                        </DropdownMenuItem>
                      ))
                    )}
                  </DropdownMenuSubContent>
                </DropdownMenuSub>
              )}
              {canAccessAccountSettings && (
                <DropdownMenuItem onClick={handleNavigateToAccountSettings} aria-label={t`Account settings`}>
                  <SettingsIcon className="size-5" />
                  <Trans>Account settings</Trans>
                </DropdownMenuItem>
              )}
            </DropdownMenuGroup>
          )}

          <DropdownMenuSeparator />

          <DropdownMenuItem onClick={handleShowSupport} aria-label={t`Contact support`}>
            <MailQuestion className="size-5" />
            <Trans>Contact support</Trans>
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>

      <SupportDialog isOpen={isSupportDialogOpen} onOpenChange={setIsSupportDialogOpen} />
      {isSwitching && <SwitchingAccountLoader />}
    </div>
  );
}

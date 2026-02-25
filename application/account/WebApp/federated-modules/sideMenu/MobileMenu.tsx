import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { authSyncService, type TenantSwitchedMessage } from "@repo/infrastructure/auth/AuthSyncService";
import { loggedInPath, loginPath } from "@repo/infrastructure/auth/constants";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { hasPermission } from "@repo/infrastructure/auth/routeGuards";
import { createLoginUrlWithReturnPath } from "@repo/infrastructure/auth/util";
import { enhancedFetch } from "@repo/infrastructure/http/httpClient";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
import { MenuButton, overlayContext, SideMenuSeparator } from "@repo/ui/components/SideMenu";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import {
  ArrowLeftRightIcon,
  Check,
  LayoutDashboardIcon,
  LogOutIcon,
  MailQuestion,
  SettingsIcon,
  SlidersHorizontalIcon,
  UserIcon
} from "lucide-react";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import { SupportDialog } from "../common/SupportDialog";
import { SwitchingAccountLoader } from "../common/SwitchingAccountLoader";

interface TenantInfo {
  tenantId: string;
  tenantName: string | null;
  logoUrl: string | null;
  isNew: boolean;
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

async function fetchTenants(): Promise<{ tenants: TenantInfo[] }> {
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

function MobileMenuHeader({ onNavigate }: { onNavigate?: (path: string) => void }) {
  const userInfo = useUserInfo();
  const overlayCtx = useContext(overlayContext);
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const [isSwitching, setIsSwitching] = useState(false);
  const [isSupportDialogOpen, setIsSupportDialogOpen] = useState(false);
  const [tenants, setTenants] = useState<TenantInfo[]>([]);

  const canAccessAccountSettings = hasPermission({ allowedRoles: ["Owner", "Admin"] });
  const currentTenantId = userInfo?.tenantId;

  useEffect(() => {
    if (userInfo?.isAuthenticated) {
      fetchTenants()
        .then((response) => setTenants(response.tenants || []))
        .catch(() => setTenants([]));
    }
  }, [userInfo?.isAuthenticated]);

  const sortedTenants = sortTenants(tenants);

  const handleTenantSwitch = async (tenant: TenantInfo) => {
    if (tenant.tenantId === currentTenantId || tenant.isNew) {
      return;
    }

    setIsSwitching(true);
    if (overlayCtx?.isOpen) {
      overlayCtx.close();
    }

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

  const handleLogout = async () => {
    if (overlayCtx?.isOpen) {
      overlayCtx.close();
    }

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

  const handleNavigateToPreferences = () => {
    if (overlayCtx?.isOpen) {
      overlayCtx.close();
    }
    setTimeout(() => {
      if (onNavigate) {
        onNavigate("/user/preferences");
      } else {
        navigate({ to: "/user/preferences" });
      }
    }, 10);
  };

  const handleNavigateToAccountSettings = () => {
    if (overlayCtx?.isOpen) {
      overlayCtx.close();
    }
    setTimeout(() => {
      if (onNavigate) {
        onNavigate("/account");
      } else {
        navigate({ to: "/account" });
      }
    }, 10);
  };

  const handleNavigateToProfile = () => {
    if (overlayCtx?.isOpen) {
      overlayCtx.close();
    }
    setTimeout(() => {
      if (onNavigate) {
        onNavigate("/user/profile");
      } else {
        navigate({ to: "/user/profile" });
      }
    }, 10);
  };

  const handleShowSupport = () => {
    setIsSupportDialogOpen(true);
  };

  return (
    <>
      <div>
        <div className="flex flex-col">
          {userInfo && (
            <div className="flex items-center gap-3">
              <Avatar className="size-12">
                <AvatarImage src={userInfo.avatarUrl ?? undefined} />
                <AvatarFallback>{userInfo.initials ?? ""}</AvatarFallback>
              </Avatar>
              <div className="min-w-0 flex-1">
                <div className="truncate font-medium text-foreground text-sm">{userInfo.fullName}</div>
                <div className="truncate text-muted-foreground text-xs">{userInfo.title ?? userInfo.email}</div>
              </div>
              <div className="shrink-0" style={{ position: "relative", zIndex: 1000 }}>
                <Button
                  variant="outline"
                  size="default"
                  onClick={(e) => {
                    e.stopPropagation();
                    e.preventDefault();
                    handleNavigateToProfile();
                  }}
                  onTouchEnd={(e) => {
                    e.stopPropagation();
                    e.preventDefault();
                  }}
                  style={{ pointerEvents: "auto", position: "relative", touchAction: "none" }}
                >
                  <UserIcon />
                  <Trans>Edit</Trans>
                </Button>
              </div>
            </div>
          )}

          {canAccessAccountSettings && (
            <div className="flex items-center justify-between">
              <Button
                variant="ghost"
                onClick={handleNavigateToAccountSettings}
                className="flex h-11 w-full items-center justify-start gap-4 py-2 pr-2 pl-3 font-normal text-muted-foreground text-sm hover:bg-hover-background hover:text-foreground"
                style={{ pointerEvents: "auto", touchAction: "none" }}
              >
                <div className="flex size-6 shrink-0 items-center justify-center">
                  <SettingsIcon className="size-5 stroke-current" />
                </div>
                <div className="overflow-hidden whitespace-nowrap text-start">
                  <Trans>Account settings</Trans>
                </div>
              </Button>
            </div>
          )}

          {sortedTenants.length > 1 && (
            <DropdownMenu>
              <DropdownMenuTrigger
                render={
                  <Button
                    variant="ghost"
                    className="flex h-11 w-full items-center justify-start gap-4 px-3 py-2 font-normal text-muted-foreground text-sm hover:bg-hover-background hover:text-foreground"
                    style={{ pointerEvents: "auto" }}
                  >
                    <div className="flex size-6 shrink-0 items-center justify-center">
                      <ArrowLeftRightIcon className="size-5 stroke-current" />
                    </div>
                    <div className="min-w-0 flex-1 overflow-hidden whitespace-nowrap text-start">
                      <Trans>Switch account</Trans>
                    </div>
                  </Button>
                }
              />
              <DropdownMenuContent align="end">
                {sortedTenants.map((tenant) => (
                  <DropdownMenuItem key={tenant.tenantId} onClick={() => handleTenantSwitch(tenant)}>
                    <TenantLogo logoUrl={tenant.logoUrl} tenantName={tenant.tenantName || ""} />
                    <div className="flex flex-1 items-center justify-between gap-2">
                      <span className="overflow-hidden text-ellipsis whitespace-nowrap">
                        {tenant.tenantName || t`Unnamed account`}
                      </span>
                      <div className="flex shrink-0 items-center gap-2">
                        {tenant.isNew && (
                          <Badge variant="secondary" className="bg-warning text-warning-foreground text-xs">
                            <Trans>Invitation pending</Trans>
                          </Badge>
                        )}
                        {tenant.tenantId === currentTenantId && <Check className="size-4" />}
                      </div>
                    </div>
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>
          )}

          <div className="flex items-center justify-between">
            <Button
              variant="ghost"
              onClick={handleShowSupport}
              className="flex h-11 w-full items-center justify-start gap-4 py-2 pr-2 pl-3 font-normal text-muted-foreground text-sm hover:bg-hover-background hover:text-foreground"
              style={{ pointerEvents: "auto", touchAction: "none" }}
            >
              <div className="flex size-6 shrink-0 items-center justify-center">
                <MailQuestion className="size-5 stroke-current" />
              </div>
              <div className="overflow-hidden whitespace-nowrap text-start">
                <Trans>Contact support</Trans>
              </div>
            </Button>
          </div>

          <div className="flex items-center justify-between">
            <Button
              variant="ghost"
              onClick={handleNavigateToPreferences}
              className="flex h-11 w-full items-center justify-start gap-4 py-2 pr-2 pl-3 font-normal text-muted-foreground text-sm hover:bg-hover-background hover:text-foreground"
              style={{ pointerEvents: "auto", touchAction: "none" }}
            >
              <div className="flex size-6 shrink-0 items-center justify-center">
                <SlidersHorizontalIcon className="size-5 stroke-current" />
              </div>
              <div className="overflow-hidden whitespace-nowrap text-start">
                <Trans>Preferences</Trans>
              </div>
            </Button>
          </div>

          <div className="flex items-center justify-between">
            <Button
              variant="ghost"
              onClick={handleLogout}
              className="flex h-11 w-full items-center justify-start gap-4 py-2 pr-2 pl-3 font-normal text-muted-foreground text-sm hover:bg-hover-background hover:text-foreground"
              style={{ pointerEvents: "auto", touchAction: "none" }}
            >
              <div className="flex size-6 shrink-0 items-center justify-center">
                <LogOutIcon className="size-5 stroke-current" />
              </div>
              <div className="overflow-hidden whitespace-nowrap text-start">
                <Trans>Log out</Trans>
              </div>
            </Button>
          </div>
        </div>
      </div>

      <SupportDialog isOpen={isSupportDialogOpen} onOpenChange={setIsSupportDialogOpen} />
      {isSwitching && <SwitchingAccountLoader />}
    </>
  );
}

export interface MobileMenuProps {
  navigationContent?: React.ReactNode;
  onNavigate?: (path: string) => void;
}

export default function MobileMenu({ navigationContent, onNavigate }: Readonly<MobileMenuProps>) {
  return (
    <div
      className="flex h-full flex-col overflow-hidden"
      onTouchStart={(e) => e.stopPropagation()}
      style={{ touchAction: "pan-y" }}
    >
      <div className="flex-1 overflow-y-auto overflow-x-hidden p-1">
        <MobileMenuHeader onNavigate={onNavigate} />

        <div className="mx-2 my-5 border-border border-b" />

        <div className="flex flex-col">
          <SideMenuSeparator>
            <Trans>Navigation</Trans>
          </SideMenuSeparator>
          {navigationContent ?? (
            <MenuButton
              icon={LayoutDashboardIcon}
              label={t`Dashboard`}
              href="/dashboard"
              federatedNavigation={true}
              onNavigate={onNavigate}
            />
          )}
        </div>
      </div>
    </div>
  );
}

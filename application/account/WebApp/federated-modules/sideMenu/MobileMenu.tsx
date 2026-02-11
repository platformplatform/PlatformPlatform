import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { trackInteraction, useTrackOpen } from "@repo/infrastructure/applicationInsights/ApplicationInsightsProvider";
import { authSyncService, type TenantSwitchedMessage } from "@repo/infrastructure/auth/AuthSyncService";
import { loggedInPath, loginPath } from "@repo/infrastructure/auth/constants";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { hasPermission } from "@repo/infrastructure/auth/routeGuards";
import { createLoginUrlWithReturnPath } from "@repo/infrastructure/auth/util";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Dialog, DialogBody, DialogContent, DialogHeader, DialogTitle } from "@repo/ui/components/Dialog";
import { MenuButton, overlayContext, SideMenuSeparator } from "@repo/ui/components/SideMenu";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import {
  ArrowRightLeftIcon,
  Check,
  ChevronDownIcon,
  CircleUserIcon,
  CreditCardIcon,
  HomeIcon,
  LayoutDashboardIcon,
  LogOutIcon,
  MessageCircleQuestion,
  MonitorSmartphoneIcon,
  SlidersHorizontalIcon,
  UserIcon,
  UsersIcon
} from "lucide-react";
import { useContext, useEffect, useState } from "react";
import { SupportDialog } from "../common/SupportDialog";
import { SwitchingAccountLoader } from "../common/SwitchingAccountLoader";
import { fetchTenants, logoutApi, sortTenants, switchTenantApi, type TenantInfo } from "../common/tenantUtils";

const menuItemBaseClassName =
  "flex h-[var(--control-height)] w-full items-center justify-start gap-4 rounded-md px-3 py-2 text-sm hover:bg-hover-background hover:text-foreground active:bg-hover-background";

function menuItemClassName(pathname: string, itemPath: string, matchPrefix = false) {
  const isActive = matchPrefix ? pathname.startsWith(itemPath) : pathname === itemPath;
  return `${menuItemBaseClassName} ${isActive ? "font-semibold text-foreground" : "font-normal text-muted-foreground"}`;
}

function TenantSwitcherDrawer({
  isOpen,
  onOpenChange,
  tenants,
  currentTenantId,
  onTenantSwitch
}: {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  tenants: TenantInfo[];
  currentTenantId: string | undefined;
  onTenantSwitch: (tenant: TenantInfo) => void;
}) {
  const sortedTenants = sortTenants(tenants);

  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange} modal={false} trackingTitle="Switch account">
      <DialogContent
        className="top-auto bottom-0 h-auto max-h-[70dvh] translate-y-0 rounded-t-2xl sm:top-auto sm:bottom-0 sm:max-h-[70dvh] sm:-translate-y-0 sm:rounded-t-2xl sm:rounded-b-none"
        showCloseButton={false}
      >
        <DialogHeader>
          <DialogTitle>
            <Trans>Select account</Trans>
          </DialogTitle>
        </DialogHeader>
        <DialogBody>
          <div className="flex flex-col gap-1">
            {sortedTenants.map((tenant) => (
              <Button
                key={tenant.tenantId}
                variant="ghost"
                onClick={() => onTenantSwitch(tenant)}
                disabled={tenant.tenantId === currentTenantId || tenant.isNew}
                className="flex h-[var(--control-height)] w-full items-center justify-start gap-3 rounded-md px-3 py-2 font-normal text-sm hover:bg-hover-background active:bg-hover-background disabled:cursor-default disabled:opacity-100"
              >
                <TenantLogo logoUrl={tenant.logoUrl} tenantName={tenant.tenantName || ""} />
                <div className="flex min-w-0 flex-1 items-center justify-between gap-2">
                  <span className="overflow-hidden text-ellipsis whitespace-nowrap text-left">
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
              </Button>
            ))}
          </div>
        </DialogBody>
      </DialogContent>
    </Dialog>
  );
}

function MobileMenuContent({
  tenants,
  onOpenTenantSwitcher
}: {
  tenants: TenantInfo[];
  onOpenTenantSwitcher: () => void;
}) {
  const userInfo = useUserInfo();
  const overlayCtx = useContext(overlayContext);
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const pathname = window.location.pathname;
  const [isUserExpanded, setIsUserExpanded] = useState(pathname.startsWith("/user/"));
  const [isTenantExpanded, setIsTenantExpanded] = useState(pathname.startsWith("/account"));

  const currentTenantId = userInfo?.tenantId;
  const canAccessAccountSettings = hasPermission({ allowedRoles: ["Owner", "Admin"] });

  const hasMultipleTenants = sortTenants(tenants).length > 1;
  const hasSubscription = userInfo?.role === "Owner";
  const hasTenantActions = hasMultipleTenants || canAccessAccountSettings || hasSubscription;
  const currentTenant = tenants.find((tenant) => tenant.tenantId === currentTenantId);
  const currentTenantName = currentTenant?.tenantName || userInfo?.tenantName || "PlatformPlatform";
  const currentTenantNameForLogo = currentTenant?.tenantName || userInfo?.tenantName || "";
  const currentTenantLogoUrl = currentTenant ? currentTenant.logoUrl : userInfo?.tenantLogoUrl;

  const closeMenu = () => {
    if (overlayCtx?.isOpen) {
      overlayCtx.close();
    }
  };

  const handleLogout = async () => {
    closeMenu();

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

  const navigateTo = (path: string) => {
    navigate({ to: path });
    closeMenu();
  };

  return (
    <div className="flex flex-col">
      {userInfo && (
        <>
          <Button
            variant="ghost"
            onClick={() => setIsUserExpanded(!isUserExpanded)}
            className="flex h-14 w-full items-center justify-start gap-3 rounded-md py-2 pr-3 pl-2 font-normal text-sm hover:bg-hover-background active:bg-hover-background"
            aria-expanded={isUserExpanded}
          >
            <Avatar className="size-8">
              <AvatarImage src={userInfo.avatarUrl ?? undefined} />
              <AvatarFallback className="text-xs">{userInfo.initials ?? ""}</AvatarFallback>
            </Avatar>
            <div className="min-w-0 flex-1 text-left">
              <div className="truncate font-medium text-foreground">{userInfo.fullName}</div>
              <div className="truncate text-muted-foreground text-xs">{userInfo.email}</div>
            </div>
            <ChevronDownIcon
              className={`size-4 shrink-0 text-muted-foreground transition-transform duration-150 ${isUserExpanded ? "rotate-180" : ""}`}
            />
          </Button>

          {isUserExpanded && (
            <div className="flex flex-col">
              <Button
                variant="ghost"
                onClick={() => navigateTo("/user/profile")}
                className={menuItemClassName(pathname, "/user/profile")}
                aria-label={t`User profile`}
              >
                <div className="flex size-6 shrink-0 items-center justify-center">
                  <UserIcon className="size-5 stroke-current" />
                </div>
                <Trans>Profile</Trans>
              </Button>
              <Button
                variant="ghost"
                onClick={() => navigateTo("/user/preferences")}
                className={menuItemClassName(pathname, "/user/preferences")}
                aria-label={t`User preferences`}
              >
                <div className="flex size-6 shrink-0 items-center justify-center">
                  <SlidersHorizontalIcon className="size-5 stroke-current" />
                </div>
                <Trans>Preferences</Trans>
              </Button>
              <Button
                variant="ghost"
                onClick={() => navigateTo("/user/sessions")}
                className={menuItemClassName(pathname, "/user/sessions")}
                aria-label={t`User sessions`}
              >
                <div className="flex size-6 shrink-0 items-center justify-center">
                  <MonitorSmartphoneIcon className="size-5 stroke-current" />
                </div>
                <Trans>Sessions</Trans>
              </Button>
              <Button
                variant="ghost"
                onClick={handleLogout}
                className={`${menuItemBaseClassName} font-normal text-muted-foreground`}
                aria-label={t`Log out`}
              >
                <div className="flex size-6 shrink-0 items-center justify-center">
                  <LogOutIcon className="size-5 stroke-current" />
                </div>
                <Trans>Log out</Trans>
              </Button>
            </div>
          )}
        </>
      )}

      {hasTenantActions && (
        <>
          <Button
            variant="ghost"
            onClick={() => setIsTenantExpanded(!isTenantExpanded)}
            className="flex h-14 w-full items-center justify-start gap-3 rounded-md py-2 pr-3 pl-2 font-normal text-sm hover:bg-hover-background active:bg-hover-background"
            aria-expanded={isTenantExpanded}
          >
            <div className="flex size-8 shrink-0 items-center justify-center">
              <TenantLogo logoUrl={currentTenantLogoUrl} tenantName={currentTenantNameForLogo} />
            </div>
            <div className="min-w-0 flex-1 overflow-hidden text-ellipsis whitespace-nowrap text-left font-medium text-foreground">
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
                  onClick={() => navigateTo("/account/subscription")}
                  className={menuItemClassName(pathname, "/account/subscription", true)}
                  aria-label={t`Subscription`}
                >
                  <div className="flex size-6 shrink-0 items-center justify-center">
                    <CreditCardIcon className="size-5 stroke-current" />
                  </div>
                  <Trans>Subscription</Trans>
                </Button>
              )}
            </div>
          )}
        </>
      )}
    </div>
  );
}

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
    trackInteraction("Switch account", "menu", "open");
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
          <h5 className="mb-0 min-w-0 overflow-hidden text-ellipsis whitespace-nowrap font-normal">
            {currentTenantName}
          </h5>
        </div>
      )}
      <div className="flex-1 overflow-y-auto overflow-x-hidden px-1 py-1">
        <MobileMenuContent tenants={tenants} onOpenTenantSwitcher={handleOpenTenantSwitcher} />

        <div className="my-3 border-border border-b" />

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

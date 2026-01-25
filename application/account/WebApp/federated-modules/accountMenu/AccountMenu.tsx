import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { authSyncService, type TenantSwitchedMessage } from "@repo/infrastructure/auth/AuthSyncService";
import { loggedInPath, loginPath } from "@repo/infrastructure/auth/constants";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { hasPermission } from "@repo/infrastructure/auth/routeGuards";
import { createLoginUrlWithReturnPath } from "@repo/infrastructure/auth/util";
import { enhancedFetch } from "@repo/infrastructure/http/httpClient";
import localeMap from "@repo/infrastructure/translations/i18n.config.json";
import type { Locale } from "@repo/infrastructure/translations/TranslationContext";
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
import {
  ArrowLeftRightIcon,
  Check,
  ChevronsUpDownIcon,
  GlobeIcon,
  LogOutIcon,
  MailQuestion,
  MoonIcon,
  MoonStarIcon,
  PaletteIcon,
  SettingsIcon,
  SunIcon,
  SunMoonIcon
} from "lucide-react";
import { useTheme } from "next-themes";
import { useContext, useEffect, useState } from "react";
import { SupportDialog } from "../common/SupportDialog";
import { SwitchingAccountLoader } from "../common/SwitchingAccountLoader";

const PREFERRED_LOCALE_KEY = "preferred-locale";

const locales = Object.entries(localeMap).map(([id, info]) => ({
  id: id as Locale,
  label: info.label
}));

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

interface AccountMenuProps {
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

async function updateLocaleOnBackend(locale: Locale) {
  try {
    const response = await enhancedFetch("/api/account/users/me/change-locale", {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ Locale: locale })
    });
    return response.ok || response.status === 401;
  } catch {
    return true;
  }
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

const ThemeMode = {
  System: "system",
  Light: "light",
  Dark: "dark"
} as const;

async function logoutApi(): Promise<void> {
  await enhancedFetch("/api/account/authentication/logout", {
    method: "POST"
  });
}

export default function AccountMenu({ isCollapsed: isCollapsedProp }: Readonly<AccountMenuProps>) {
  const userInfo = useUserInfo();
  const isCollapsedContext = useContext(collapsedContext);
  const isCollapsed = isCollapsedProp ?? isCollapsedContext;
  const overlayCtx = useContext(overlayContext);
  const queryClient = useQueryClient();
  const { theme, setTheme, resolvedTheme } = useTheme();
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isSwitching, setIsSwitching] = useState(false);
  const [isSupportDialogOpen, setIsSupportDialogOpen] = useState(false);
  const [tenants, setTenants] = useState<TenantInfo[]>([]);
  const [isLoadingTenants, setIsLoadingTenants] = useState(false);
  const [currentLocale, setCurrentLocale] = useState<Locale>("en-US");

  const sidebarWidth = useSidebarWidth(isCollapsed);
  const canAccessAccountSettings = hasPermission({ allowedRoles: ["Owner", "Admin"] });

  useEffect(() => {
    const htmlLang = document.documentElement.lang as Locale;
    const savedLocale = localStorage.getItem(PREFERRED_LOCALE_KEY) as Locale;

    if (savedLocale && locales.some((l) => l.id === savedLocale)) {
      setCurrentLocale(savedLocale);
    } else if (htmlLang && locales.some((l) => l.id === htmlLang)) {
      setCurrentLocale(htmlLang);
    }
  }, []);

  useEffect(() => {
    window.dispatchEvent(new CustomEvent("tenant-menu-toggle", { detail: { isOpen: isMenuOpen } }));
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
  const currentLocaleLabel = locales.find((l) => l.id === currentLocale)?.label || currentLocale;

  const handleThemeChange = (newTheme: string) => {
    setTheme(newTheme);
  };

  const getThemeIcon = () => {
    if (theme === ThemeMode.Dark || (theme === ThemeMode.System && resolvedTheme === ThemeMode.Dark)) {
      return theme === ThemeMode.System ? <MoonStarIcon className="size-4" /> : <MoonIcon className="size-4" />;
    }
    return theme === ThemeMode.System ? <SunMoonIcon className="size-4" /> : <SunIcon className="size-4" />;
  };

  const getThemeLabel = () => {
    if (theme === ThemeMode.System) {
      return t`System`;
    }
    if (theme === ThemeMode.Light) {
      return t`Light`;
    }
    return t`Dark`;
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

  const handleLocaleChange = async (locale: Locale) => {
    if (locale === currentLocale) {
      return;
    }

    localStorage.setItem(PREFERRED_LOCALE_KEY, locale);
    await updateLocaleOnBackend(locale);
    window.location.reload();
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
    if (overlayCtx?.isOpen) {
      overlayCtx.close();
    }
    window.location.href = "/account";
  };

  const handleNavigateToProfile = () => {
    if (overlayCtx?.isOpen) {
      overlayCtx.close();
    }
    window.location.href = "/account/profile";
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
      <DropdownMenu onOpenChange={setIsMenuOpen}>
        <DropdownMenuTrigger disabled={isSwitching} className={triggerClassName} aria-label={t`Account menu`}>
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
          {canAccessAccountSettings && (
            <DropdownMenuItem onClick={handleNavigateToAccountSettings}>
              <SettingsIcon className="size-4" />
              <Trans>Account settings</Trans>
            </DropdownMenuItem>
          )}

          {sortedTenants.length > 1 && (
            <DropdownMenuSub>
              <DropdownMenuSubTrigger>
                <ArrowLeftRightIcon className="size-4" />
                <Trans>Switch account</Trans>
              </DropdownMenuSubTrigger>
              <DropdownMenuSubContent className="min-w-56">
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
                        <div className="flex flex-col overflow-hidden">
                          <span className="overflow-hidden text-ellipsis whitespace-nowrap">
                            {tenant.tenantName || t`Unnamed account`}
                          </span>
                          <span className="overflow-hidden text-ellipsis whitespace-nowrap text-muted-foreground text-xs">
                            {userInfo?.email}
                          </span>
                        </div>
                        {tenant.tenantId === currentTenantId && <Check className="ml-2 size-4 shrink-0" />}
                      </div>
                    </DropdownMenuItem>
                  ))
                )}
              </DropdownMenuSubContent>
            </DropdownMenuSub>
          )}

          {(canAccessAccountSettings || sortedTenants.length > 1) && <DropdownMenuSeparator />}

          <DropdownMenuGroup>
            <DropdownMenuLabel className="font-normal">
              <div className="flex flex-row items-center gap-2">
                <Avatar size="lg">
                  <AvatarImage src={userInfo.avatarUrl ?? undefined} />
                  <AvatarFallback>{userInfo.initials ?? ""}</AvatarFallback>
                </Avatar>
                <div className="my-1 flex flex-1 flex-col">
                  <span className="font-medium">{userInfo.fullName}</span>
                  <span className="text-muted-foreground text-sm">{userInfo.email}</span>
                </div>
                <button
                  type="button"
                  onClick={handleNavigateToProfile}
                  className="rounded-md bg-secondary px-2.5 py-1 text-secondary-foreground text-sm hover:bg-secondary/80"
                >
                  <Trans>Edit</Trans>
                </button>
              </div>
            </DropdownMenuLabel>
          </DropdownMenuGroup>

          <DropdownMenuSub>
            <DropdownMenuSubTrigger>
              <GlobeIcon className="size-4" />
              <span className="flex-1">
                <Trans>Language</Trans>
              </span>
              <span className="text-muted-foreground text-sm">{currentLocaleLabel}</span>
            </DropdownMenuSubTrigger>
            <DropdownMenuSubContent>
              {locales.map((locale) => (
                <DropdownMenuItem key={locale.id} onClick={() => handleLocaleChange(locale.id)}>
                  <div className="flex items-center gap-2">
                    <span>{locale.label}</span>
                    {locale.id === currentLocale && <Check className="ml-auto size-4" />}
                  </div>
                </DropdownMenuItem>
              ))}
            </DropdownMenuSubContent>
          </DropdownMenuSub>

          <DropdownMenuSub>
            <DropdownMenuSubTrigger>
              <PaletteIcon className="size-4" />
              <span className="flex-1">
                <Trans>Theme</Trans>
              </span>
              <span className="flex items-center gap-1 text-muted-foreground text-sm">
                {getThemeIcon()}
                {getThemeLabel()}
              </span>
            </DropdownMenuSubTrigger>
            <DropdownMenuSubContent>
              <DropdownMenuItem onClick={() => handleThemeChange(ThemeMode.System)}>
                <div className="flex items-center gap-2">
                  {resolvedTheme === ThemeMode.Dark ? (
                    <MoonStarIcon className="size-4" />
                  ) : (
                    <SunMoonIcon className="size-4" />
                  )}
                  <Trans>System</Trans>
                  {theme === ThemeMode.System && <Check className="ml-auto size-4" />}
                </div>
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => handleThemeChange(ThemeMode.Light)}>
                <div className="flex items-center gap-2">
                  <SunIcon className="size-4" />
                  <Trans>Light</Trans>
                  {theme === ThemeMode.Light && <Check className="ml-auto size-4" />}
                </div>
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => handleThemeChange(ThemeMode.Dark)}>
                <div className="flex items-center gap-2">
                  <MoonIcon className="size-4" />
                  <Trans>Dark</Trans>
                  {theme === ThemeMode.Dark && <Check className="ml-auto size-4" />}
                </div>
              </DropdownMenuItem>
            </DropdownMenuSubContent>
          </DropdownMenuSub>

          <DropdownMenuItem onClick={handleLogout}>
            <LogOutIcon className="size-4" />
            <Trans>Log out</Trans>
          </DropdownMenuItem>

          <DropdownMenuSeparator />

          <DropdownMenuItem onClick={handleShowSupport}>
            <MailQuestion className="size-4" />
            <Trans>Contact support</Trans>
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>

      <SupportDialog isOpen={isSupportDialogOpen} onOpenChange={setIsSupportDialogOpen} />
      {isSwitching && <SwitchingAccountLoader />}
    </div>
  );
}

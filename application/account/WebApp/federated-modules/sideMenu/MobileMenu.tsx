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
import {
  ArrowLeftRightIcon,
  Check,
  GlobeIcon,
  LayoutDashboardIcon,
  LogOutIcon,
  MailQuestion,
  MoonIcon,
  MoonStarIcon,
  PaletteIcon,
  SettingsIcon,
  SunIcon,
  SunMoonIcon,
  UserIcon
} from "lucide-react";
import { useTheme } from "next-themes";
import type React from "react";
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
}

const ThemeMode = {
  System: "system",
  Light: "light",
  Dark: "dark"
} as const;

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

function MobileMenuHeader() {
  const userInfo = useUserInfo();
  const overlayCtx = useContext(overlayContext);
  const queryClient = useQueryClient();
  const { theme, setTheme, resolvedTheme } = useTheme();

  const [isSwitching, setIsSwitching] = useState(false);
  const [isSupportDialogOpen, setIsSupportDialogOpen] = useState(false);
  const [tenants, setTenants] = useState<TenantInfo[]>([]);
  const [currentLocale, setCurrentLocale] = useState<Locale>("en-US");

  const canAccessAccountSettings = hasPermission({ allowedRoles: ["Owner", "Admin"] });
  const currentTenantId = userInfo?.tenantId;

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
    if (userInfo?.isAuthenticated) {
      fetchTenants()
        .then((response) => setTenants(response.tenants || []))
        .catch(() => setTenants([]));
    }
  }, [userInfo?.isAuthenticated]);

  const sortedTenants = sortTenants(tenants);
  const currentLocaleLabel = locales.find((l) => l.id === currentLocale)?.label || currentLocale;

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

  const handleLocaleChange = async (locale: Locale) => {
    if (locale === currentLocale) {
      return;
    }

    if (overlayCtx?.isOpen) {
      overlayCtx.close();
    }

    localStorage.setItem(PREFERRED_LOCALE_KEY, locale);
    await updateLocaleOnBackend(locale);
    window.location.reload();
  };

  const handleThemeChange = (newTheme: string) => {
    setTheme(newTheme);
    if (overlayCtx?.isOpen) {
      overlayCtx.close();
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

      window.location.href = createLoginUrlWithReturnPath(loginPath);
    } catch {
      window.location.href = createLoginUrlWithReturnPath(loginPath);
    }
  };

  const handleNavigateToAccountSettings = () => {
    if (overlayCtx?.isOpen) {
      overlayCtx.close();
    }
    setTimeout(() => {
      window.location.href = "/account";
    }, 10);
  };

  const handleNavigateToProfile = () => {
    if (overlayCtx?.isOpen) {
      overlayCtx.close();
    }
    setTimeout(() => {
      window.location.href = "/account/profile";
    }, 10);
  };

  const handleShowSupport = () => {
    setIsSupportDialogOpen(true);
  };

  const getThemeIcon = () => {
    if (theme === ThemeMode.Dark || (theme === ThemeMode.System && resolvedTheme === ThemeMode.Dark)) {
      return theme === ThemeMode.System ? (
        <MoonStarIcon className="size-5 stroke-current" />
      ) : (
        <MoonIcon className="size-5 stroke-current" />
      );
    }
    return theme === ThemeMode.System ? (
      <SunMoonIcon className="size-5 stroke-current" />
    ) : (
      <SunIcon className="size-5 stroke-current" />
    );
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

          <DropdownMenu>
            <DropdownMenuTrigger
              render={
                <Button
                  variant="ghost"
                  className="flex h-11 w-full items-center justify-start gap-4 px-3 py-2 font-normal text-muted-foreground text-sm hover:bg-hover-background hover:text-foreground"
                  style={{ pointerEvents: "auto" }}
                >
                  <div className="flex size-6 shrink-0 items-center justify-center">
                    <GlobeIcon className="size-5 stroke-current" />
                  </div>
                  <div className="min-w-0 flex-1 overflow-hidden whitespace-nowrap text-start">
                    <Trans>Language</Trans>
                  </div>
                  <div className="shrink-0 text-muted-foreground text-sm">{currentLocaleLabel}</div>
                </Button>
              }
            />
            <DropdownMenuContent align="end">
              {locales.map((locale) => (
                <DropdownMenuItem key={locale.id} onClick={() => handleLocaleChange(locale.id)}>
                  <div className="flex items-center gap-2">
                    <span>{locale.label}</span>
                    {locale.id === currentLocale && <Check className="ml-auto size-4" />}
                  </div>
                </DropdownMenuItem>
              ))}
            </DropdownMenuContent>
          </DropdownMenu>

          <DropdownMenu>
            <DropdownMenuTrigger
              render={
                <Button
                  variant="ghost"
                  className="flex h-11 w-full items-center justify-start gap-4 px-3 py-2 font-normal text-muted-foreground text-sm hover:bg-hover-background hover:text-foreground"
                  style={{ pointerEvents: "auto" }}
                >
                  <div className="flex size-6 shrink-0 items-center justify-center">
                    <PaletteIcon className="size-5 stroke-current" />
                  </div>
                  <div className="min-w-0 flex-1 overflow-hidden whitespace-nowrap text-start">
                    <Trans>Theme</Trans>
                  </div>
                  <div className="flex shrink-0 items-center gap-1 text-muted-foreground text-sm">
                    {getThemeIcon()}
                    {getThemeLabel()}
                  </div>
                </Button>
              }
            />
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => handleThemeChange(ThemeMode.System)}>
                <div className="flex items-center gap-2">
                  {resolvedTheme === ThemeMode.Dark ? (
                    <MoonStarIcon className="size-5" />
                  ) : (
                    <SunMoonIcon className="size-5" />
                  )}
                  <Trans>System</Trans>
                  {theme === ThemeMode.System && <Check className="ml-auto size-5" />}
                </div>
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => handleThemeChange(ThemeMode.Light)}>
                <div className="flex items-center gap-2">
                  <SunIcon className="size-5" />
                  <Trans>Light</Trans>
                  {theme === ThemeMode.Light && <Check className="ml-auto size-5" />}
                </div>
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => handleThemeChange(ThemeMode.Dark)}>
                <div className="flex items-center gap-2">
                  <MoonIcon className="size-5" />
                  <Trans>Dark</Trans>
                  {theme === ThemeMode.Dark && <Check className="ml-auto size-5" />}
                </div>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>

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
}

export default function MobileMenu({ navigationContent }: Readonly<MobileMenuProps>) {
  return (
    <div
      className="flex h-full flex-col overflow-hidden"
      onTouchStart={(e) => e.stopPropagation()}
      style={{ touchAction: "pan-y" }}
    >
      <div className="flex-1 overflow-y-auto overflow-x-hidden p-1 pt-[calc(0.25rem+var(--past-due-banner-height,0rem)+var(--invitation-banner-height,0rem))]">
        <MobileMenuHeader />

        <div className="mx-2 my-5 border-border border-b" />

        <div className="flex flex-col">
          <SideMenuSeparator>
            <Trans>Navigation</Trans>
          </SideMenuSeparator>
          {navigationContent ?? <MenuButton icon={LayoutDashboardIcon} label={t`Dashboard`} href="/dashboard" />}
        </div>
      </div>
    </div>
  );
}

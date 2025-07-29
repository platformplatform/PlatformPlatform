import { api } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import { loginPath } from "@repo/infrastructure/auth/constants";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { createLoginUrlWithReturnPath } from "@repo/infrastructure/auth/util";
import { type Locale, translationContext } from "@repo/infrastructure/translations/TranslationContext";
import { Avatar } from "@repo/ui/components/Avatar";
import { Button } from "@repo/ui/components/Button";
import { Menu, MenuItem, MenuTrigger } from "@repo/ui/components/Menu";
import { MenuButton, SideMenu, SideMenuSeparator, overlayContext } from "@repo/ui/components/SideMenu";
import { useThemeMode } from "@repo/ui/theme/mode/ThemeMode";
import { SystemThemeMode, ThemeMode } from "@repo/ui/theme/mode/utils";
import { useQueryClient } from "@tanstack/react-query";
import {
  CheckIcon,
  CircleUserIcon,
  GlobeIcon,
  HomeIcon,
  LogOutIcon,
  MoonIcon,
  MoonStarIcon,
  SunIcon,
  SunMoonIcon,
  UserIcon,
  UsersIcon
} from "lucide-react";
import type React from "react";
import { use, useContext, useState } from "react";
import UserProfileModal from "./userModals/UserProfileModal";

type SharedSideMenuProps = {
  children?: React.ReactNode;
  ariaLabel: string;
};

export function SharedSideMenu({ children, ariaLabel }: Readonly<SharedSideMenuProps>) {
  const userInfo = useUserInfo();
  const { i18n } = useLingui();
  const { getLocaleInfo, locales, setLocale } = use(translationContext);
  const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);
  const queryClient = useQueryClient();
  const { themeMode, resolvedThemeMode, setThemeMode } = useThemeMode();

  // Access mobile menu overlay context to close menu when needed
  const overlayCtx = useContext(overlayContext);

  const currentLocale = i18n.locale as Locale;
  const currentLocaleLabel = getLocaleInfo(currentLocale).label;

  const getThemeName = (mode: ThemeMode) => {
    switch (mode) {
      case ThemeMode.System:
        return t`System`;
      case ThemeMode.Light:
        return t`Light`;
      case ThemeMode.Dark:
        return t`Dark`;
      default:
        return t`System`;
    }
  };

  const getThemeIcon = (themeMode: ThemeMode, resolvedThemeMode: SystemThemeMode) => {
    if (resolvedThemeMode === SystemThemeMode.Dark) {
      return themeMode === ThemeMode.System ? (
        <MoonStarIcon className="h-5 w-5 stroke-current" />
      ) : (
        <MoonIcon className="h-5 w-5 stroke-current" />
      );
    }
    return themeMode === ThemeMode.System ? (
      <SunMoonIcon className="h-5 w-5 stroke-current" />
    ) : (
      <SunIcon className="h-5 w-5 stroke-current" />
    );
  };

  const logoutMutation = api.useMutation("post", "/api/account-management/authentication/logout", {
    onMutate: async () => {
      await queryClient.cancelQueries();
      queryClient.clear();
    },
    onSuccess: () => {
      window.location.href = createLoginUrlWithReturnPath(loginPath);
    },
    meta: {
      skipQueryInvalidation: true
    }
  });

  const topMenuContent = (
    <div className="flex h-full flex-col">
      {/* User Profile Section */}
      <div className="flex flex-col gap-3">
        {/* User Profile */}
        {userInfo && (
          <div className="flex items-center gap-3 px-3">
            <Avatar avatarUrl={userInfo.avatarUrl} initials={userInfo.initials ?? ""} isRound={true} size="md" />
            <div className="min-w-0 flex-1">
              <div className="truncate font-medium text-foreground text-sm">{userInfo.fullName}</div>
              <div className="truncate text-muted-foreground text-xs">{userInfo.title || userInfo.email}</div>
            </div>
            <div className="shrink-0" style={{ position: "relative", zIndex: 1000 }}>
              <button
                type="button"
                onClick={() => {
                  // Close mobile menu if it's open
                  if (overlayCtx?.isOpen) {
                    overlayCtx.close();
                  } else {
                    // Fallback: dispatch custom event to close mobile menu
                    window.dispatchEvent(new CustomEvent("close-mobile-menu"));
                  }

                  // Small delay to ensure menu closes before modal opens
                  setTimeout(() => {
                    setIsProfileModalOpen(true);
                  }, 50);
                }}
                className="rounded border border-border bg-background px-2 py-1 text-sm hover:bg-hover-background"
                style={{ pointerEvents: "auto", position: "relative" }}
              >
                <UserIcon className="mr-1 inline h-4 w-4" />
                <Trans>Edit</Trans>
              </button>
            </div>
          </div>
        )}

        {/* Logout */}
        <div className="flex items-center justify-between">
          <Button
            variant="ghost"
            onPress={() => {
              // Close mobile menu if it's open
              if (overlayCtx?.isOpen) {
                overlayCtx.close();
              }
              logoutMutation.mutate({});
            }}
            className="flex h-11 w-full items-center justify-start gap-4 px-3 py-2 font-normal text-base text-muted-foreground hover:bg-hover-background hover:text-foreground"
            style={{ pointerEvents: "auto" }}
          >
            <div className="flex h-6 w-6 shrink-0 items-center justify-center">
              <LogOutIcon className="h-5 w-5 stroke-current" />
            </div>
            <div className="overflow-hidden whitespace-nowrap text-start">
              <Trans>Log out</Trans>
            </div>
          </Button>
        </div>

        {/* Theme Section - button that cycles themes */}
        <div className="flex items-center justify-between">
          <button
            type="button"
            onClick={() => {
              // Close mobile menu if it's open
              if (overlayCtx?.isOpen) {
                overlayCtx.close();
              }

              // Cycle through themes: System -> Light -> Dark -> System
              const nextTheme =
                themeMode === ThemeMode.System
                  ? ThemeMode.Light
                  : themeMode === ThemeMode.Light
                    ? ThemeMode.Dark
                    : ThemeMode.System;
              setThemeMode(nextTheme);
            }}
            className="flex h-11 w-full items-center justify-start gap-4 rounded px-3 py-2 font-normal text-base text-muted-foreground hover:bg-hover-background hover:text-foreground"
            style={{ pointerEvents: "auto" }}
          >
            <div className="flex h-6 w-6 shrink-0 items-center justify-center">
              {getThemeIcon(themeMode, resolvedThemeMode)}
            </div>
            <div className="min-w-0 flex-1 overflow-hidden whitespace-nowrap text-start">
              <Trans>Theme</Trans>
            </div>
            <div className="shrink-0 text-base text-muted-foreground">{getThemeName(themeMode)}</div>
          </button>
        </div>

        {/* Language Section - styled like menu item */}
        <div className="flex items-center justify-between">
          <MenuTrigger>
            <Button
              variant="ghost"
              className="flex h-11 w-full items-center justify-start gap-4 px-3 py-2 font-normal text-base text-muted-foreground hover:bg-hover-background hover:text-foreground"
              style={{ pointerEvents: "auto" }}
            >
              <div className="flex h-6 w-6 shrink-0 items-center justify-center">
                <GlobeIcon className="h-5 w-5 stroke-current" />
              </div>
              <div className="min-w-0 flex-1 overflow-hidden whitespace-nowrap text-start">
                <Trans>Language</Trans>
              </div>
              <div className="shrink-0 text-base text-muted-foreground">{currentLocaleLabel}</div>
            </Button>
            <Menu
              onAction={(key) => {
                const locale = key.toString() as Locale;
                if (locale !== currentLocale) {
                  setLocale(locale);
                }
              }}
              placement="bottom end"
            >
              {locales.map((locale) => (
                <MenuItem key={locale} id={locale} textValue={getLocaleInfo(locale).label}>
                  <div className="flex items-center gap-2">
                    <span>{getLocaleInfo(locale).label}</span>
                    {locale === currentLocale && <CheckIcon className="ml-auto h-4 w-4" />}
                  </div>
                </MenuItem>
              ))}
            </Menu>
          </MenuTrigger>
        </div>
      </div>

      {/* Divider */}
      <div className="mx-3 my-5 border-border border-b" />

      {/* Navigation Section for Mobile */}
      <div className="flex flex-col gap-3">
        <SideMenuSeparator>
          <Trans>Navigation</Trans>
        </SideMenuSeparator>
        <MenuButton icon={HomeIcon} label={t`Home`} href="/admin" />
        <SideMenuSeparator>
          <Trans>Organization</Trans>
        </SideMenuSeparator>
        <MenuButton icon={CircleUserIcon} label={t`Account`} href="/admin/account" />
        <MenuButton icon={UsersIcon} label={t`Users`} href="/admin/users" />
        {children}
      </div>

      {/* Spacer to push content up */}
      <div className="flex-1" />
    </div>
  );

  return (
    <>
      <SideMenu ariaLabel={ariaLabel} topMenuContent={topMenuContent} tenantName={userInfo?.tenantName}>
        <MenuButton icon={HomeIcon} label={t`Home`} href="/admin" />
        <SideMenuSeparator>
          <Trans>Organization</Trans>
        </SideMenuSeparator>
        <MenuButton icon={CircleUserIcon} label={t`Account`} href="/admin/account" />
        <MenuButton icon={UsersIcon} label={t`Users`} href="/admin/users" />
        {children}
      </SideMenu>

      <UserProfileModal isOpen={isProfileModalOpen} onOpenChange={setIsProfileModalOpen} />
    </>
  );
}

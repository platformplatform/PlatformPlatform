import { api } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { loginPath } from "@repo/infrastructure/auth/constants";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { createLoginUrlWithReturnPath } from "@repo/infrastructure/auth/util";
import { Avatar } from "@repo/ui/components/Avatar";
import { Button } from "@repo/ui/components/Button";
import { Menu, MenuItem, MenuTrigger } from "@repo/ui/components/Menu";
import { MenuButton, SideMenu, SideMenuSeparator, SideMenuSpacer, overlayContext } from "@repo/ui/components/SideMenu";
import { ThemeModeSelector } from "@repo/ui/theme/ThemeModeSelector";
import { useQueryClient } from "@tanstack/react-query";
import {
  BoxIcon,
  CheckIcon,
  CircleUserIcon,
  GlobeIcon,
  HomeIcon,
  LogOutIcon,
  MailQuestion,
  UserIcon,
  UsersIcon
} from "lucide-react";
import type React from "react";
import { useContext, useState } from "react";
import { SupportDialog } from "./support/SupportDialog";
import UserProfileModal from "./userModals/UserProfileModal";
import "@repo/ui/tailwind.css";

type SharedSideMenuProps = {
  children?: React.ReactNode;
  ariaLabel: string;
  currentLocale?: string;
  currentLocaleLabel?: string;
  locales?: Array<{ value: string; label: string }>;
  onLocaleChange?: (locale: string) => void;
};

export default function SharedSideMenu({
  children,
  ariaLabel,
  currentLocale,
  currentLocaleLabel,
  locales,
  onLocaleChange
}: Readonly<SharedSideMenuProps>) {
  const userInfo = useUserInfo();
  const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);
  const queryClient = useQueryClient();

  // Access mobile menu overlay context to close menu when needed
  const overlayCtx = useContext(overlayContext);

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

        {/* Theme Section - using ThemeModeSelector with mobile menu variant */}
        <div className="flex items-center justify-between">
          <ThemeModeSelector
            aria-label={t`Change theme`}
            variant="mobile-menu"
            onAction={() => {
              // Close mobile menu if it's open
              if (overlayCtx?.isOpen) {
                overlayCtx.close();
              }
            }}
          />
        </div>

        {/* Language Section - styled like menu item */}
        {currentLocale && currentLocaleLabel && locales && onLocaleChange && (
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
                  const locale = key.toString();
                  if (locale !== currentLocale) {
                    onLocaleChange(locale);
                  }
                }}
                placement="bottom end"
              >
                {locales.map((locale) => (
                  <MenuItem key={locale.value} id={locale.value} textValue={locale.label}>
                    <div className="flex items-center gap-2">
                      <span>{locale.label}</span>
                      {locale.value === currentLocale && <CheckIcon className="ml-auto h-4 w-4" />}
                    </div>
                  </MenuItem>
                ))}
              </Menu>
            </MenuTrigger>
          </div>
        )}

        {/* Support Section - styled like menu item */}
        <div className="flex items-center justify-between">
          <SupportDialog>
            <Button
              variant="ghost"
              className="flex h-11 w-full items-center justify-start gap-4 px-3 py-2 font-normal text-base text-muted-foreground hover:bg-hover-background hover:text-foreground"
              style={{ pointerEvents: "auto" }}
            >
              <div className="flex h-6 w-6 shrink-0 items-center justify-center">
                <MailQuestion className="h-5 w-5 stroke-current" />
              </div>
              <div className="overflow-hidden whitespace-nowrap text-start">
                <Trans>Contact support</Trans>
              </div>
            </Button>
          </SupportDialog>
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
        <MenuButton icon={BoxIcon} label={t`Back Office`} href="/back-office" forceReload={true} />
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
        <MenuButton icon={BoxIcon} label={t`Back Office`} href="/back-office" forceReload={true} />
        {children}
      </SideMenu>

      <UserProfileModal isOpen={isProfileModalOpen} onOpenChange={setIsProfileModalOpen} />
    </>
  );
}

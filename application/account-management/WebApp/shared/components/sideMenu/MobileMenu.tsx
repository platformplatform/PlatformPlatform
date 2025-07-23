import { api } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import { loginPath } from "@repo/infrastructure/auth/constants";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { createLoginUrlWithReturnPath } from "@repo/infrastructure/auth/util";
import type { Locale } from "@repo/infrastructure/translations/TranslationContext";
import localeMap from "@repo/infrastructure/translations/i18n.config.json";
import { Avatar } from "@repo/ui/components/Avatar";
import { Button } from "@repo/ui/components/Button";
import { Menu, MenuItem, MenuTrigger } from "@repo/ui/components/Menu";
import { SideMenuSeparator, overlayContext } from "@repo/ui/components/SideMenu";
import { ThemeModeSelector } from "@repo/ui/theme/ThemeModeSelector";
import { useQueryClient } from "@tanstack/react-query";
import { CheckIcon, GlobeIcon, LogOutIcon, MailQuestion, UserIcon } from "lucide-react";
import { useContext, useState } from "react";
import { SupportDialog } from "../support/SupportDialog";
import UserProfileModal from "../userModals/UserProfileModal";
import { NavigationMenuItems } from "./NavigationMenuItems";
import type { SharedSideMenuProps } from "./SharedSideMenu";

// Mobile menu header section with user profile and settings
function MobileMenuHeader() {
  const userInfo = useUserInfo();
  const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);
  const queryClient = useQueryClient();
  const { i18n } = useLingui();
  const overlayCtx = useContext(overlayContext);

  const currentLocale = i18n.locale as Locale;
  const locales = Object.keys(localeMap) as Locale[];
  const currentLocaleLabel = localeMap[currentLocale].label;

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

  return (
    <div>
      {/* User Profile Section */}
      <div className="flex flex-col gap-3">
        {/* User Profile */}
        {userInfo && (
          <div className="flex items-center gap-3 px-3">
            <Avatar avatarUrl={userInfo.avatarUrl} initials={userInfo.initials ?? ""} isRound={true} size="md" />
            <div className="min-w-0 flex-1">
              <div className="truncate font-medium text-foreground text-sm">{userInfo.fullName}</div>
              <div className="truncate text-muted-foreground text-xs">{userInfo.title ?? userInfo.email}</div>
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
              onAction={async (key) => {
                const locale = key.toString() as Locale;
                if (locale !== currentLocale) {
                  // Dynamically load and activate the locale
                  const localeModule = await import(`@/shared/translations/locale/${locale}.ts`);
                  i18n.loadAndActivate({ locale, messages: localeModule.messages });
                  document.documentElement.lang = locale;
                }
              }}
              placement="bottom end"
            >
              {locales.map((locale) => (
                <MenuItem key={locale} id={locale} textValue={localeMap[locale].label}>
                  <div className="flex items-center gap-2">
                    <span>{localeMap[locale].label}</span>
                    {locale === currentLocale && <CheckIcon className="ml-auto h-4 w-4" />}
                  </div>
                </MenuItem>
              ))}
            </Menu>
          </MenuTrigger>
        </div>

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

      <UserProfileModal isOpen={isProfileModalOpen} onOpenChange={setIsProfileModalOpen} />
    </div>
  );
}

// Complete mobile menu including header and navigation
export function MobileMenu({ currentSystem }: Readonly<{ currentSystem: SharedSideMenuProps["currentSystem"] }>) {
  return (
    <div className="flex h-full flex-col">
      <MobileMenuHeader />

      {/* Divider */}
      <div className="mx-3 my-5 border-border border-b" />

      {/* Navigation Section for Mobile */}
      <div className="flex flex-col gap-3">
        <SideMenuSeparator>
          <Trans>Navigation</Trans>
        </SideMenuSeparator>
        <NavigationMenuItems currentSystem={currentSystem} />
      </div>

      {/* Spacer to push content up */}
      <div className="flex-1" />
    </div>
  );
}

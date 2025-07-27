import { api } from "@/shared/lib/api/client";
import { Trans } from "@lingui/react/macro";
import { loginPath } from "@repo/infrastructure/auth/constants";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { createLoginUrlWithReturnPath } from "@repo/infrastructure/auth/util";
import { Avatar } from "@repo/ui/components/Avatar";
import { Button } from "@repo/ui/components/Button";
import { SideMenuSeparator, overlayContext } from "@repo/ui/components/SideMenu";
import { useQueryClient } from "@tanstack/react-query";
import { LogOutIcon, MailQuestion, UserIcon } from "lucide-react";
import { useContext } from "react";
import LocaleSwitcher from "../common/LocaleSwitcher";
import { SupportDialog } from "../common/SupportDialog";
import ThemeModeSelector from "../common/ThemeModeSelector";
import type { FederatedSideMenuProps } from "./FederatedSideMenu";
import { NavigationMenuItems } from "./NavigationMenuItems";

// Mobile menu header section with user profile and settings
function MobileMenuHeader({ onEditProfile }: { onEditProfile: () => void }) {
  const userInfo = useUserInfo();
  const queryClient = useQueryClient();
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
                onClick={(e) => {
                  e.stopPropagation();
                  e.preventDefault();
                  setTimeout(() => {
                    onEditProfile();
                    if (overlayCtx?.isOpen) {
                      overlayCtx.close();
                    }
                  }, 10);
                }}
                onTouchEnd={(e) => {
                  e.stopPropagation();
                  e.preventDefault();
                }}
                className="rounded border border-border bg-background px-2 py-1 text-sm hover:bg-hover-background"
                style={{ pointerEvents: "auto", position: "relative", touchAction: "none" }}
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
              setTimeout(() => {
                // Close mobile menu if it's open
                if (overlayCtx?.isOpen) {
                  overlayCtx.close();
                }
                logoutMutation.mutate({});
              }, 10);
            }}
            className="flex h-11 w-full items-center justify-start gap-4 px-3 py-2 font-normal text-base text-muted-foreground hover:bg-hover-background hover:text-foreground"
            style={{ pointerEvents: "auto", touchAction: "none" }}
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
            variant="mobile-menu"
            onAction={() => {
              // Small delay to ensure touch events are fully processed
              setTimeout(() => {
                // Close mobile menu if it's open
                if (overlayCtx?.isOpen) {
                  overlayCtx.close();
                }
              }, 10);
            }}
          />
        </div>

        {/* Language Section - using LocaleSwitcher with mobile menu variant */}
        <div className="flex items-center justify-between">
          <LocaleSwitcher
            variant="mobile-menu"
            onAction={() => {
              // Small delay to ensure touch events are fully processed
              setTimeout(() => {
                // Close mobile menu if it's open
                if (overlayCtx?.isOpen) {
                  overlayCtx.close();
                }
              }, 10);
            }}
          />
        </div>

        {/* Support Section - styled like menu item */}
        <div className="flex items-center justify-between">
          <SupportDialog>
            <Button
              variant="ghost"
              className="flex h-11 w-full items-center justify-start gap-4 px-3 py-2 font-normal text-base text-muted-foreground hover:bg-hover-background hover:text-foreground"
              style={{ pointerEvents: "auto", touchAction: "none" }}
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
    </div>
  );
}

// Complete mobile menu including header and navigation
export function MobileMenu({
  currentSystem,
  onEditProfile
}: Readonly<{ currentSystem: FederatedSideMenuProps["currentSystem"]; onEditProfile: () => void }>) {
  return (
    <div
      className="flex h-full flex-col overflow-hidden"
      onTouchStart={(e) => e.stopPropagation()}
      style={{ touchAction: "pan-y" }}
    >
      <div className="flex-1 overflow-y-auto overflow-x-hidden">
        <MobileMenuHeader onEditProfile={onEditProfile} />

        {/* Divider */}
        <div className="mx-3 my-5 border-border border-b" />

        {/* Navigation Section for Mobile */}
        <div className="flex flex-col gap-3">
          <SideMenuSeparator>
            <Trans>Navigation</Trans>
          </SideMenuSeparator>
          <NavigationMenuItems currentSystem={currentSystem} />
        </div>
      </div>
    </div>
  );
}

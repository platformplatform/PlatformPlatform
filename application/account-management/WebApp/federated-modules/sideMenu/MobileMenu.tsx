import { Trans } from "@lingui/react/macro";
import { loginPath } from "@repo/infrastructure/auth/constants";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { createLoginUrlWithReturnPath } from "@repo/infrastructure/auth/util";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Button } from "@repo/ui/components/Button";
import { overlayContext, SideMenuSeparator } from "@repo/ui/components/SideMenu";
import { useQueryClient } from "@tanstack/react-query";
import { LogOutIcon, MailQuestion, MonitorSmartphoneIcon, UserIcon } from "lucide-react";
import { useContext, useState } from "react";
import type { components } from "@/shared/lib/api/api.generated";
import { api } from "@/shared/lib/api/client";
import LocaleSwitcher from "../common/LocaleSwitcher";
import { SupportDialog } from "../common/SupportDialog";
import TenantSelector from "../common/TenantSelector";
import ThemeModeSelector from "../common/ThemeModeSelector";
import type { FederatedSideMenuProps } from "./FederatedSideMenu";
import { NavigationMenuItems } from "./NavigationMenuItems";

// Mobile menu header section with user profile and settings
function MobileMenuHeader({
  onEditProfile,
  onShowSessions
}: {
  onEditProfile: () => void;
  onShowSessions: () => void;
}) {
  const [isSupportDialogOpen, setIsSupportDialogOpen] = useState(false);
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
                style={{ pointerEvents: "auto", position: "relative", touchAction: "none" }}
              >
                <UserIcon />
                <Trans>Edit</Trans>
              </Button>
            </div>
          </div>
        )}

        {/* Sessions */}
        <div className="flex items-center justify-between">
          <Button
            variant="ghost"
            onClick={() => {
              setTimeout(() => {
                onShowSessions();
                if (overlayCtx?.isOpen) {
                  overlayCtx.close();
                }
              }, 10);
            }}
            className="flex h-11 w-full items-center justify-start gap-4 py-2 pr-2 pl-3 font-normal text-base text-muted-foreground hover:bg-hover-background hover:text-foreground"
            style={{ pointerEvents: "auto", touchAction: "none" }}
          >
            <div className="flex size-6 shrink-0 items-center justify-center">
              <MonitorSmartphoneIcon className="size-5 stroke-current" />
            </div>
            <div className="overflow-hidden whitespace-nowrap text-start">
              <Trans>Sessions</Trans>
            </div>
          </Button>
        </div>

        {/* Logout */}
        <div className="flex items-center justify-between">
          <Button
            variant="ghost"
            onClick={() => {
              setTimeout(() => {
                // Close mobile menu if it's open
                if (overlayCtx?.isOpen) {
                  overlayCtx.close();
                }
                logoutMutation.mutate({});
              }, 10);
            }}
            className="flex h-11 w-full items-center justify-start gap-4 py-2 pr-2 pl-3 font-normal text-base text-muted-foreground hover:bg-hover-background hover:text-foreground"
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
          <Button
            variant="ghost"
            onClick={() => setIsSupportDialogOpen(true)}
            className="flex h-11 w-full items-center justify-start gap-4 py-2 pr-2 pl-3 font-normal text-base text-muted-foreground hover:bg-hover-background hover:text-foreground"
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

        <SupportDialog isOpen={isSupportDialogOpen} onOpenChange={setIsSupportDialogOpen} />
      </div>
    </div>
  );
}

// Complete mobile menu including header and navigation
export function MobileMenu({
  currentSystem,
  onEditProfile,
  onShowSessions,
  onShowInvitationDialog
}: Readonly<{
  currentSystem: FederatedSideMenuProps["currentSystem"];
  onEditProfile: () => void;
  onShowSessions: () => void;
  onShowInvitationDialog?: (tenant: components["schemas"]["TenantInfo"]) => void;
}>) {
  return (
    <div
      className="flex h-full flex-col overflow-hidden"
      onTouchStart={(e) => e.stopPropagation()}
      style={{ touchAction: "pan-y" }}
    >
      <div className="flex-1 overflow-y-auto overflow-x-hidden p-1">
        <MobileMenuHeader onEditProfile={onEditProfile} onShowSessions={onShowSessions} />

        {/* Divider */}
        <div className="mx-2 my-5 border-border border-b" />

        {/* Tenant Selector */}
        <div className="-mx-3">
          <TenantSelector variant="mobile-menu" onShowInvitationDialog={onShowInvitationDialog} />
        </div>

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

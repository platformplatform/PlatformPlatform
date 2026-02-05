import { Trans } from "@lingui/react/macro";
import { loginPath } from "@repo/infrastructure/auth/constants";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { createLoginUrlWithReturnPath } from "@repo/infrastructure/auth/util";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Button } from "@repo/ui/components/Button";
import { overlayContext, SideMenuSeparator } from "@repo/ui/components/SideMenu";
import { useQueryClient } from "@tanstack/react-query";
import { LogOutIcon, MailQuestion, MonitorSmartphoneIcon, UserIcon } from "lucide-react";
import { useContext } from "react";
import type { components } from "@/shared/lib/api/api.generated";
import { api } from "@/shared/lib/api/client";
import LocaleSwitcher from "../common/LocaleSwitcher";
import TenantSelector from "../common/TenantSelector";
import ThemeModeSelector from "../common/ThemeModeSelector";
import type { FederatedSideMenuProps } from "./FederatedSideMenu";
import { NavigationMenuItems } from "./NavigationMenuItems";

// Delay before closing menu to allow dialog to render on top (dialog z-50 > menu z-40)
const MENU_CLOSE_DELAY = 250;

// Mobile menu header section with user profile and settings
function MobileMenuHeader({
  onEditProfile,
  onShowSessions,
  onShowSupport
}: {
  onEditProfile: () => void;
  onShowSessions: () => void;
  onShowSupport: () => void;
}) {
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

  // Opens a dialog from the menu: dialog opens immediately (appears on top of menu),
  // then menu closes after a delay (closing happens behind the dialog)
  const openDialogFromMenu = (openDialog: () => void) => {
    openDialog();
    setTimeout(() => overlayCtx?.close(), MENU_CLOSE_DELAY);
  };

  // Closes menu immediately (for actions that don't open a dialog)
  const closeMenu = () => overlayCtx?.close();

  // Standard button styles for menu items
  const menuButtonClass =
    "flex h-11 w-full items-center justify-start gap-4 py-2 pr-2 pl-3 font-normal text-base text-muted-foreground hover:bg-hover-background hover:text-foreground";

  return (
    <div>
      <div className="flex flex-col gap-0">
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
            <Button variant="outline" size="default" onClick={() => openDialogFromMenu(onEditProfile)}>
              <UserIcon />
              <Trans>Edit</Trans>
            </Button>
          </div>
        )}

        {/* Sessions */}
        <Button variant="ghost" onClick={() => openDialogFromMenu(onShowSessions)} className={menuButtonClass}>
          <div className="flex size-6 shrink-0 items-center justify-center">
            <MonitorSmartphoneIcon className="size-5 stroke-current" />
          </div>
          <Trans>Sessions</Trans>
        </Button>

        {/* Logout */}
        <Button
          variant="ghost"
          onClick={() => {
            closeMenu();
            logoutMutation.mutate({});
          }}
          className={menuButtonClass}
        >
          <div className="flex size-6 shrink-0 items-center justify-center">
            <LogOutIcon className="size-5 stroke-current" />
          </div>
          <Trans>Log out</Trans>
        </Button>

        {/* Theme */}
        <ThemeModeSelector variant="mobile-menu" onAction={closeMenu} />

        {/* Language */}
        <LocaleSwitcher variant="mobile-menu" onAction={closeMenu} />

        {/* Support */}
        <Button variant="ghost" onClick={() => openDialogFromMenu(onShowSupport)} className={menuButtonClass}>
          <div className="flex size-6 shrink-0 items-center justify-center">
            <MailQuestion className="size-5 stroke-current" />
          </div>
          <Trans>Contact support</Trans>
        </Button>
      </div>
    </div>
  );
}

// Complete mobile menu including header and navigation
export function MobileMenu({
  currentSystem,
  onEditProfile,
  onShowSessions,
  onShowSupport,
  onShowInvitationDialog
}: Readonly<{
  currentSystem: FederatedSideMenuProps["currentSystem"];
  onEditProfile: () => void;
  onShowSessions: () => void;
  onShowSupport: () => void;
  onShowInvitationDialog?: (tenant: components["schemas"]["TenantInfo"]) => void;
}>) {
  return (
    <div
      className="flex h-full flex-col overflow-hidden"
      onTouchStart={(e) => e.stopPropagation()}
      style={{ touchAction: "pan-y" }}
    >
      <div className="flex-1 overflow-y-auto overflow-x-hidden p-1">
        <MobileMenuHeader onEditProfile={onEditProfile} onShowSessions={onShowSessions} onShowSupport={onShowSupport} />

        {/* Divider */}
        <div className="mx-2 my-5 border-border border-b" />

        {/* Tenant Selector */}
        <div className="-mx-3">
          <TenantSelector variant="mobile-menu" onShowInvitationDialog={onShowInvitationDialog} />
        </div>

        {/* Navigation Section for Mobile */}
        <div className="flex flex-col gap-0">
          <SideMenuSeparator>
            <Trans>Navigation</Trans>
          </SideMenuSeparator>
          <NavigationMenuItems currentSystem={currentSystem} />
        </div>
      </div>
    </div>
  );
}

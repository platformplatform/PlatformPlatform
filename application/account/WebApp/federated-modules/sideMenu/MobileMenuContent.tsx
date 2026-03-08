import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { authSyncService } from "@repo/infrastructure/auth/AuthSyncService";
import { loginPath } from "@repo/infrastructure/auth/constants";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { createLoginUrlWithReturnPath } from "@repo/infrastructure/auth/util";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Button } from "@repo/ui/components/Button";
import { overlayContext } from "@repo/ui/components/SideMenu";
import { useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { ChevronDownIcon, LogOutIcon, MonitorSmartphoneIcon, SlidersHorizontalIcon, UserIcon } from "lucide-react";
import { useContext, useState } from "react";

import { logoutApi, type TenantInfo } from "../common/tenantUtils";
import { menuItemBaseClassName, menuItemClassName } from "./menuUtils";
import { TenantMenuSection } from "./TenantMenuSection";

export function MobileMenuContent({
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
            className="flex h-14 w-full items-center justify-start gap-3 rounded-md py-2 pr-3 pl-2 text-sm font-normal hover:bg-hover-background active:bg-hover-background"
            aria-expanded={isUserExpanded}
          >
            <Avatar className="size-8">
              <AvatarImage src={userInfo.avatarUrl ?? undefined} />
              <AvatarFallback className="text-xs">{userInfo.initials ?? ""}</AvatarFallback>
            </Avatar>
            <div className="min-w-0 flex-1 text-left">
              <div className="truncate font-medium text-foreground">{userInfo.fullName}</div>
              <div className="truncate text-xs text-muted-foreground">{userInfo.email}</div>
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

      <TenantMenuSection
        tenants={tenants}
        onOpenTenantSwitcher={onOpenTenantSwitcher}
        pathname={pathname}
        navigateTo={navigateTo}
      />
    </div>
  );
}

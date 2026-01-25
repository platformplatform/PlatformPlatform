import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { collapsedContext, MenuButton, SideMenu, SideMenuSeparator } from "@repo/ui/components/SideMenu";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { LayoutDashboardIcon } from "lucide-react";
import { useContext } from "react";

function AccountMenuPlaceholder() {
  const userInfo = useUserInfo();
  const isCollapsed = useContext(collapsedContext);

  const tenantName = userInfo?.tenantName ?? "";
  const tenantLogoUrl = userInfo?.tenantLogoUrl;

  return (
    <div className={`relative w-full ${isCollapsed ? "px-2" : "px-3"}`}>
      <div
        className={`flex h-11 items-center rounded-md py-2 text-sm ${
          isCollapsed ? "ml-[0.375rem] w-11 justify-center" : "w-full pr-2 pl-2"
        }`}
      >
        <div className="flex size-8 shrink-0 items-center justify-center">
          <TenantLogo logoUrl={tenantLogoUrl} tenantName={tenantName} />
        </div>
        {!isCollapsed && (
          <div className="ml-4 flex-1 overflow-hidden text-ellipsis whitespace-nowrap text-left font-semibold text-foreground">
            {tenantName || "PlatformPlatform"}
          </div>
        )}
      </div>
    </div>
  );
}

export function MainSideMenu() {
  return (
    <SideMenu
      sidebarToggleAriaLabel={t`Toggle sidebar`}
      mobileMenuAriaLabel={t`Open navigation menu`}
      logoContent={<AccountMenuPlaceholder />}
    >
      <SideMenuSeparator>
        <Trans>Navigation</Trans>
      </SideMenuSeparator>

      <MenuButton icon={LayoutDashboardIcon} label={t`Dashboard`} href="/dashboard" />
    </SideMenu>
  );
}

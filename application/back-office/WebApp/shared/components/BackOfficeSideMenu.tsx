import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { MenuButton, SideMenu, SideMenuSeparator } from "@repo/ui/components/SideMenu";
import { BoxIcon } from "lucide-react";

export function BackOfficeSideMenu() {
  return (
    <SideMenu sidebarToggleAriaLabel={t`Toggle sidebar`} mobileMenuAriaLabel={t`Open navigation menu`}>
      <SideMenuSeparator>
        <Trans>Navigation</Trans>
      </SideMenuSeparator>

      <MenuButton icon={BoxIcon} label={t`Dashboard`} href="/back-office" />
    </SideMenu>
  );
}

import { MenuButton, SideMenu } from "@repo/ui/components/SideMenu";
import { HomeIcon } from "lucide-react";
import { t } from "@lingui/macro";

type SharedSideMenuProps = {
  children?: React.ReactNode;
};

export function SharedSideMenu({ children }: Readonly<SharedSideMenuProps>) {
  return (
    <SideMenu>
      <MenuButton icon={HomeIcon} label={t`Home`} href="/back-office" />
      {children}
    </SideMenu>
  );
}

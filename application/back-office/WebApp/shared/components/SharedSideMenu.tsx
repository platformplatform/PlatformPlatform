import { MenuButton, SideMenu } from "@repo/ui/components/SideMenu";
import { HomeIcon } from "lucide-react";

type SharedSideMenuProps = {
  children?: React.ReactNode;
};

export function SharedSideMenu({ children }: Readonly<SharedSideMenuProps>) {
  return (
    <SideMenu>
      <MenuButton icon={HomeIcon} label="Home" href="/back-office" />
      {children}
    </SideMenu>
  );
}

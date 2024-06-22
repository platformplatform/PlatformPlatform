import {
  ChevronRightIcon,
  CircleUserIcon,
  Languages,
  LifeBuoyIcon,
  LogOutIcon,
  MoonIcon,
  SettingsIcon,
  UserIcon
} from "lucide-react";
import { MenuTrigger } from "react-aria-components";
import ProfileMenuItem from "./profileMenuItem";
import { Button } from "@repo/ui/components/Button";
import { Menu, MenuItem, MenuSeparator } from "@repo/ui/components/Menu";
import { Popover } from "@repo/ui/components/Popover";

export function UserHeader() {
  return (
    <div className="flex text-slate-600 text-sm font-medium items-center justify-between space-x-8">
      <div className="flex gap-4 items-center">
        <div className="flex gap-4 whitespace-nowrap">
          Home
          <ChevronRightIcon className="text-slate-600" size={20} />
        </div>
        <div className="flex gap-4 whitespace-nowrap">
          Users
          <ChevronRightIcon className="text-slate-600" size={20} />
        </div>
        <div className="flex gap-4 text-gray-400 text-sm font-semibold whitespace-nowrap">All Users</div>
      </div>
      <div className="flex flex-row gap-6 items-center">
        <MoonIcon size={20} />
        <LifeBuoyIcon size={20} />
        <Languages size={20} />
        <MenuTrigger>
          <Button aria-label="Menu" variant="icon">
            <CircleUserIcon size={30} />
          </Button>
          <Popover>
            <Menu>
              <MenuItem className="h-16 w-60" onAction={() => alert("open")}>
                <ProfileMenuItem />
              </MenuItem>
              <MenuSeparator />
              <MenuItem className="h-11 w-60" onAction={() => alert("open")}>
                <UserIcon size={16} />
                Edit profile
              </MenuItem>
              <MenuItem className="h-11 w-60" onAction={() => alert("rename")}>
                <SettingsIcon size={16} />
                Account settings
              </MenuItem>
              <MenuSeparator />
              <MenuItem className="h-12 w-60" onAction={() => alert("rename")}>
                <LogOutIcon size={16} /> Sign out
              </MenuItem>
            </Menu>
          </Popover>
        </MenuTrigger>
      </div>
    </div>
  );
}

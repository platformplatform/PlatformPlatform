import { useState } from "react";
import { ChevronsLeftIcon, ChevronsRightIcon, CircleUserIcon, HomeIcon, UsersRoundIcon } from "lucide-react";
import { Button } from "./Button";

const logoWrap = "https://platformplatformgithub.blob.core.windows.net/logo-wrap.svg?url";

const MenuButton: React.FC<{ icon: React.ReactElement; label: string; color?: string; isCollapsed?: boolean }> = ({
  icon,
  label,
  color = "gray-600",
  isCollapsed = false
}) => (
  <Button variant="icon" className={`flex text-${color} text-base font-normal gap-4 justify-start p-2`}>
    {icon}
    {!isCollapsed && <span>{label}</span>}
  </Button>
);

export function SideMenu() {
  const [isCollapsed, setIsCollapsed] = useState(false);

  const toggleCollapse = () => {
    setIsCollapsed((v) => !v);
  };

  return (
    <div
      className={`text-gray-800 ${isCollapsed ? "w-20" : "min-w-72"} pl-8 py-4 flex flex-col gap-6 ${isCollapsed ? "items-center" : ""}`}
    >
      <div className="flex items-center self-end">
        <Button variant="icon" className="flex size-10" onPress={toggleCollapse}>
          {isCollapsed ? <ChevronsRightIcon size={20} /> : <ChevronsLeftIcon size={20} />}
          <div className="border border-gray-200 h-8" />
        </Button>
      </div>
      {!isCollapsed && <img src={logoWrap} alt="Logo Wrap" className="self-start" />}
      <MenuButton icon={<HomeIcon />} label="Home" isCollapsed={isCollapsed} />
      {!isCollapsed && <div className="text-gray-400 text-xs font-semibold pt-4 uppercase">Organisation</div>}
      <MenuButton icon={<CircleUserIcon />} label="Account" isCollapsed={isCollapsed} />
      <MenuButton icon={<UsersRoundIcon />} label="Users" color="black" isCollapsed={isCollapsed} />
    </div>
  );
}

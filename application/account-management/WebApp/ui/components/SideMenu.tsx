import { ChevronsLeftIcon, CircleUserIcon, HomeIcon, UsersRoundIcon } from "lucide-react";

const logoWrap = "https://platformplatformgithub.blob.core.windows.net/logo-wrap.svg?url";

export function SideMenu() {
  return (
    <div className="text-gray-800 min-w-72 pl-8 py-4 flex flex-col gap-6">
      <div className="flex items-center gap-1 ml-auto">
        <ChevronsLeftIcon size={20} />
        <div className="border border-gray-200 h-8 " />
      </div>
      <div className="flex">
        <img className="" src={logoWrap} alt="Logo Wrap" />
      </div>
      <div className="flex text-gray-600 text-base font-normal gap-4">
        <HomeIcon />
        Home
      </div>
      <div className="text-gray-400 text-xs font-semibold pt-4 uppercase">
        Organisation
      </div>
      <div className="flex text-gray-600 text-base font-normal gap-4">
        <CircleUserIcon />
        Account
      </div>
      <div className="flex text-black text-base font-semibold gap-4">
        <UsersRoundIcon />
        Users
      </div>
    </div>
  );
}

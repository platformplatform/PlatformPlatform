import { ChevronRightIcon, CircleUserIcon, Languages, LifeBuoyIcon, MoonIcon } from "lucide-react";

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
        <div className="flex gap-4 text-gray-400 text-sm font-semibold whitespace-nowrap">
          All Users
        </div>
      </div>
      <div className="flex flex-row gap-6 items-center">
        <MoonIcon size={20} />
        <LifeBuoyIcon size={20} />
        <Languages size={20} />
        <CircleUserIcon size={30} />
      </div>
    </div>
  );
}

import { UserHeader } from "./UserHeader";
import { UserTabs } from "./UserTabs";
import { UserQuerying } from "./UserQuerying";
import { UserTable } from "./UserTable";
import { UserInvite } from "./UserInvite";
import { SideMenu } from "@/ui/components/SideMenu";

export default function UsersPage() {
  return (
    <div className="flex gap-4 h-full bg-gray-50">
      <SideMenu />
      <div className="flex-grow flex flex-col gap-4 pr-8 py-4 overflow-x-auto">
        <UserHeader />
        <UserInvite />
        <UserTabs />
        <UserQuerying />
        <UserTable />
      </div>
    </div>
  );
}

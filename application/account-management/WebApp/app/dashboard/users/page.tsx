import { UserHeader } from "./UserHeader";
import { UserTabs } from "./UserTabs";
import { UserQuerying } from "./UserQuerying";
import { UserTable } from "./UserTable";
import { UserInvite } from "./UserInvite";
import { SideMenu } from "@/ui/components/SideMenu";

export default function UsersPage() {
  return (
    <div className="flex gap-4 h-full bg-gray-50 overflow-x-auto">
      <SideMenu />
      <div className="flex-grow pr-8 py-4 flex flex-col gap-4">
        <div className="flex flex-col gap-8">
          <UserHeader />
          <UserInvite />
          <UserTabs />
          <UserQuerying />
          <UserTable />
        </div>
      </div>
    </div>
  );
}

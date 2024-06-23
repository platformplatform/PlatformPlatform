import { createFileRoute } from "@tanstack/react-router";
import { UserHeader } from "./-components/UserHeader";
import { UserTabs } from "./-components/UserTabs";
import { UserQuerying } from "./-components/UserQuerying";
import { UserTable } from "./-components/UserTable";
import { UserInvite } from "./-components/UserInvite";
import { SideMenu } from "@repo/ui/components/SideMenu";

export const Route = createFileRoute("/admin/users/")({
  component: UsersPage
});

export default function UsersPage() {
  return (
    <div className="flex gap-4 h-screen bg-gray-50">
      <SideMenu />
      <div className="flex flex-grow flex-col gap-4 pl-1 pr-6 py-3 overflow-x-auto">
        <div className="z-10">
          <UserHeader />
          <UserInvite />
          <UserTabs />
          <UserQuerying />
        </div>
        <div className="flex-grow z-0 overflow-auto min-h-48">
          <UserTable />
        </div>
      </div>
    </div>
  );
}

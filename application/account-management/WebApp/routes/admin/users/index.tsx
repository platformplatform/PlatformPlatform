import { createFileRoute } from "@tanstack/react-router";
import { UserHeader } from "./-components/UserHeader";
import { UserTabs } from "./-components/UserTabs";
import { UserQuerying } from "./-components/UserQuerying";
import { UserTable } from "./-components/UserTable";
import { UserInvite } from "./-components/UserInvite";
import { SideMenu } from "@/ui/components/SideMenu";

export const Route = createFileRoute("/admin/users/")({
  component: UsersPage,
});

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

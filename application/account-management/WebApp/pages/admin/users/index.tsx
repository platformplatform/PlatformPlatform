import { createFileRoute } from "@tanstack/react-router";
import { TopMenu } from "./-components/TopMenu";
import { UserTabs } from "./-components/UserTabs";
import { UserQuerying } from "./-components/UserQuerying";
import { UserTable } from "./-components/UserTable";
import { UserInvite } from "./-components/UserInvite";
import { SideMenu } from "@repo/ui/components/SideMenu";
import { Suspense } from "react";
import { accountManagementApi } from "@/shared/lib/api/client";
import { z } from "zod";

const userPageSearchSchema = z.object({
  page: z.number().optional().catch(0)
});

export const Route = createFileRoute("/admin/users/")({
  component: UsersPage,
  validateSearch: userPageSearchSchema
});

export default function UsersPage() {
  const { page } = Route.useSearch();
  const usersPromise = accountManagementApi
    .GET("/api/account-management/users", {
      params: {
        query: {
          PageOffset: page
        }
      }
    })
    .then(({ response, data, error }) => {
      if (error) {
        throw error;
      }
      if (!data) {
        throw new Error("No data");
      }
      return data;
    });
  return (
    <div className="flex gap-4 h-screen bg-gray-50">
      <SideMenu />
      <div className="flex flex-grow flex-col gap-4 pl-1 pr-6 py-3 overflow-x-auto">
        <div className="z-10">
          <TopMenu />
          <UserInvite />
          <UserTabs />
          <UserQuerying />
        </div>
        <div className="flex-grow z-0 overflow-auto min-h-48">
          <Suspense fallback={<div>loading data</div>}>
            <UserTable usersPromise={usersPromise} />
          </Suspense>
        </div>
      </div>
    </div>
  );
}

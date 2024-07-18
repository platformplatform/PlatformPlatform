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
  pageOffset: z.number().optional().catch(0)
});

export const Route = createFileRoute("/admin/users/")({
  component: UsersPage,
  validateSearch: userPageSearchSchema
});

export default function UsersPage() {
  const { pageOffset } = Route.useSearch();
  const usersPromise = accountManagementApi
    .GET("/api/account-management/users", {
      params: {
        query: {
          PageOffset: pageOffset
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
    <div className="flex gap-4 w-full h-full border">
      <SideMenu />
      <div className="flex flex-col gap-4 pl-1 pr-6 py-3 w-full">
        <TopMenu />
        <UserInvite />
        <UserTabs />
        <UserQuerying />
        <Suspense fallback={<div>Loading data...</div>}>
          <UserTable usersPromise={usersPromise} />
        </Suspense>
      </div>
    </div>
  );
}

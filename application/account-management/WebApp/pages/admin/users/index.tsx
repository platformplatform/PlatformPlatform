import { createFileRoute } from "@tanstack/react-router";
import { TopMenu } from "./-components/TopMenu";
import { UserTabs } from "./-components/UserTabs";
import { UserQuerying } from "./-components/UserQuerying";
import { UserTable } from "./-components/UserTable";
import { UserInvite } from "./-components/UserInvite";
import { SideMenu } from "@repo/ui/components/SideMenu";
import { Suspense, useEffect, useState } from "react";
import { accountManagementApi } from "@/shared/lib/api/client";
import { z } from "zod";
import type { components } from "@/shared/lib/api/api.generated";

const userPageSearchSchema = z.object({
  pageOffset: z.number().optional().catch(0)
});

export const Route = createFileRoute("/admin/users/")({
  component: UsersPage,
  validateSearch: userPageSearchSchema
});

export default function UsersPage() {
  const [pageOffset, setPageOffset] = useState(0);
  const [orderBy, setOrderBy] = useState<components["schemas"]["SortableUserProperties"]>();
  const [sortOrder, setSortOrder] = useState<components["schemas"]["SortOrder"]>();
  const [userData, setUserData] = useState<components["schemas"]["GetUsersResponseDto"] | null>(null);

  useEffect(() => {
    accountManagementApi
      .GET("/api/account-management/users", {
        params: {
          query: {
            PageOffset: pageOffset,
            OrderBy: orderBy,
            SortOrder: sortOrder
          }
        }
      })
      .then(({ data }) => setUserData(data ?? null))
      .catch((e) => console.error(e));
  }, [pageOffset, orderBy, sortOrder]);

  return (
    <div className="flex gap-4 w-full h-full border">
      <SideMenu />
      <div className="flex flex-col gap-4 pl-1 pr-6 py-3 w-full">
        <TopMenu />
        <UserInvite />
        <UserTabs usersData={userData} />
        <UserQuerying />
        <Suspense fallback={<div>Loading data...</div>}>
          <UserTable usersData={userData} onPageChange={setPageOffset} onSortChange={[setOrderBy, setSortOrder]} />
        </Suspense>
      </div>
    </div>
  );
}

import { useEffect, useState } from "react";
import type { GetUsersResponse } from "@/shared/lib/api/users";
import { getUsers } from "@/shared/lib/api/users";
import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/admin/")({
  component: DashboardPage
});

export default function DashboardPage() {
  const [userList, setUserList] = useState<GetUsersResponse | null>(null);

  useEffect(() => {
    (async () => {
      setUserList(
        await getUsers({ Search: "", PageSize: 1, PageOffset: 0, OrderBy: "CreatedAt", SortOrder: "Ascending" })
      );
    })();
  }, []);

  return (
    <div className="items-center flex flex-col justify-center h-full">
      <div className="p-8 bg-gray-800 text-white rounded-xl shadow-md text-center gap-4 flex flex-col">
        <h1 className="text-2xl">Users</h1>
        {userList && <p>{userList.totalCount} users in the system</p>}
      </div>
    </div>
  );
}

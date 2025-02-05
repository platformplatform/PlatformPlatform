import { createFileRoute, Link } from "@tanstack/react-router";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useApi, UserStatus } from "@/shared/lib/api/client";
import { TopMenu } from "@/shared/components/topMenu";
import { SharedSideMenu } from "@/shared/components/SharedSideMenu";

export const Route = createFileRoute("/admin/")({
  component: Home
});

export default function Home() {
  const { data } = useApi("/api/account-management/users/summary", {});

  return (
    <div className="flex gap-4 w-full h-full">
      <SharedSideMenu />
      <div className="flex flex-col gap-4 py-3 px-4 w-full">
        <TopMenu />
        <div className="flex h-20 w-full items-center justify-between space-x-2 sm:mt-4 mb-4">
          <div className="text-foreground text-3xl font-semibold flex gap-2 flex-col mt-3">
            <h1>
              <Trans>Welcome home</Trans>
            </h1>
            <p className="text-muted-foreground text-sm font-normal">
              <Trans>Here's your overview of what's happening.</Trans>
            </p>
          </div>
        </div>
        <div className="flex flex-row">
          <Link
            to="/admin/users"
            className="text-muted-foreground p-6 bg-white rounded-xl shadow-md w-1/3 hover:bg-muted/80 transition-colors"
            aria-label={t`View users`}
          >
            <div className="text-sm text-gray-800">
              <Trans>Total Users</Trans>
            </div>
            <div className="text-sm text-gray-500">
              <Trans>Add more in the Users menu</Trans>
            </div>
            <div className="py-2 text-black text-2xl font-semibold">
              {data?.totalUsers ? <p>{data.totalUsers}</p> : <p>-</p>}
            </div>
          </Link>
          <Link
            to="/admin/users"
            search={{
              userStatus: UserStatus.Active,
              startDate: new Date(new Date().setDate(new Date().getDate() - 30)).toISOString().slice(0, 10),
              endDate: new Date().toISOString().slice(0, 10)
            }}
            className="p-6 bg-white rounded-xl shadow-md w-1/3 mx-6 hover:bg-muted/80 transition-colors"
            aria-label={t`View active users`}
          >
            <div className="text-sm text-gray-800">
              <Trans>Active Users</Trans>
            </div>
            <div className="text-sm text-gray-500">
              <Trans>Active users in the past 30 days</Trans>
            </div>
            <div className="py-2 text-black text-2xl font-semibold">
              {data?.activeUsers ? <p>{data.activeUsers}</p> : <p>-</p>}
            </div>
          </Link>
          <Link
            to="/admin/users"
            search={{ userStatus: UserStatus.Pending }}
            className="p-6 bg-white rounded-xl shadow-md w-1/3 hover:bg-muted/80 transition-colors"
            aria-label={t`View invited users`}
          >
            <div className="text-sm text-gray-800">
              <Trans>Invited Users</Trans>
            </div>
            <div className="text-sm text-gray-500">
              <Trans>Users who haven't confirmed their email</Trans>
            </div>
            <div className="py-2 text-black text-2xl font-semibold">
              {data?.pendingUsers ? <p>{data.pendingUsers}</p> : <p>-</p>}
            </div>
          </Link>
        </div>
      </div>
    </div>
  );
}

import { SharedSideMenu } from "@/shared/components/SharedSideMenu";
import { TopMenu } from "@/shared/components/topMenu";
import { UserStatus, api } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { getDateDaysAgo, getTodayIsoDate } from "@repo/utils/date/formatDate";
import { Link, createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/admin/")({
  component: Home
});

export default function Home() {
  const { data: usersSummary } = api.useQuery("get", "/api/account-management/users/summary");

  return (
    <div className="flex h-full w-full gap-4">
      <SharedSideMenu ariaLabel={t`Toggle collapsed menu`} />
      <div className="flex w-full flex-col gap-4 px-4 py-3">
        <TopMenu />
        <div className="mb-4 flex h-20 w-full items-center justify-between space-x-2 sm:mt-4">
          <div className="mt-3 flex flex-col gap-2 font-semibold text-3xl text-foreground">
            <h1>
              <Trans>Welcome home</Trans>
            </h1>
            <p className="font-normal text-muted-foreground text-sm">
              <Trans>Here's your overview of what's happening.</Trans>
            </p>
          </div>
        </div>
        <div className="flex flex-row">
          <Link
            to="/admin/users"
            className="w-1/3 rounded-xl bg-muted/50 p-6 transition-all hover:bg-accent"
            aria-label={t`View users`}
          >
            <div className="text-foreground text-sm">
              <Trans>Total users</Trans>
            </div>
            <div className="text-muted-foreground text-sm">
              <Trans>Add more in the Users menu</Trans>
            </div>
            <div className="py-2 font-semibold text-2xl text-foreground">
              {usersSummary?.totalUsers ? <p>{usersSummary.totalUsers}</p> : <p>-</p>}
            </div>
          </Link>
          <Link
            to="/admin/users"
            search={{
              userStatus: UserStatus.Active,
              startDate: getDateDaysAgo(30),
              endDate: getTodayIsoDate()
            }}
            className="mx-6 w-1/3 rounded-xl bg-muted/50 p-6 transition-all hover:bg-accent"
            aria-label={t`View active users`}
          >
            <div className="text-foreground text-sm">
              <Trans>Active users</Trans>
            </div>
            <div className="text-muted-foreground text-sm">
              <Trans>Active users in the past 30 days</Trans>
            </div>
            <div className="py-2 font-semibold text-2xl text-foreground">
              {usersSummary?.activeUsers ? <p>{usersSummary.activeUsers}</p> : <p>-</p>}
            </div>
          </Link>
          <Link
            to="/admin/users"
            search={{ userStatus: UserStatus.Pending }}
            className="w-1/3 rounded-xl bg-muted/50 p-6 transition-all hover:bg-accent"
            aria-label={t`View invited users`}
          >
            <div className="text-foreground text-sm">
              <Trans>Invited users</Trans>
            </div>
            <div className="text-muted-foreground text-sm">
              <Trans>Users who haven't confirmed their email</Trans>
            </div>
            <div className="py-2 font-semibold text-2xl text-foreground">
              {usersSummary?.pendingUsers ? <p>{usersSummary.pendingUsers}</p> : <p>-</p>}
            </div>
          </Link>
        </div>
      </div>
    </div>
  );
}

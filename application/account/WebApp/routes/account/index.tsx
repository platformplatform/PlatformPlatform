import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUsers } from "@repo/infrastructure/sync/hooks";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { getDateDaysAgo, getTodayIsoDate } from "@repo/utils/date/formatDate";
import { createFileRoute, Link } from "@tanstack/react-router";
import { useMemo } from "react";
import { UserStatus } from "@/shared/lib/api/userStatus";

export const Route = createFileRoute("/account/")({
  staticData: { trackingTitle: "Account overview" },
  component: Home
});

export default function Home() {
  const { data: users } = useUsers();

  const usersSummary = useMemo(() => {
    const thirtyDaysAgo = new Date();
    thirtyDaysAgo.setDate(thirtyDaysAgo.getDate() - 30);

    return {
      totalUsers: users.length,
      activeUsers: users.filter(
        (user) => user.emailConfirmed && user.lastSeenAt && new Date(user.lastSeenAt) >= thirtyDaysAgo
      ).length,
      pendingUsers: users.filter((user) => !user.emailConfirmed).length
    };
  }, [users]);

  return (
    <AppLayout
      variant="center"
      maxWidth="64rem"
      title={t`Account overview`}
      subtitle={t`A quick summary of your account activity.`}
    >
      <div className="grid w-full grid-cols-1 gap-4 pt-8 md:grid-cols-2 lg:grid-cols-3">
        <Link
          to="/account/users"
          className="flex flex-col justify-between rounded-xl bg-card p-6 outline-ring transition-[background-color] hover:bg-hover-background focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
          aria-label={t`View users`}
        >
          <div>
            <div className="font-medium text-foreground text-sm">
              <Trans>Total users</Trans>
            </div>
            <div className="mt-1 text-muted-foreground text-sm">
              <Trans>Add more in the Users menu</Trans>
            </div>
          </div>
          <div className="mt-4 font-semibold text-2xl text-foreground">{usersSummary.totalUsers}</div>
        </Link>
        <Link
          to="/account/users"
          search={{
            userStatus: UserStatus.Active,
            startDate: getDateDaysAgo(30),
            endDate: getTodayIsoDate()
          }}
          className="flex flex-col justify-between rounded-xl bg-card p-6 outline-ring transition-[background-color] hover:bg-hover-background focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
          aria-label={t`View active users`}
        >
          <div>
            <div className="font-medium text-foreground text-sm">
              <Trans>Active users</Trans>
            </div>
            <div className="mt-1 text-muted-foreground text-sm">
              <Trans>Active users in the past 30 days</Trans>
            </div>
          </div>
          <div className="mt-4 font-semibold text-2xl text-foreground">{usersSummary.activeUsers}</div>
        </Link>
        <Link
          to="/account/users"
          search={{ userStatus: UserStatus.Pending }}
          className="flex flex-col justify-between rounded-xl bg-card p-6 outline-ring transition-[background-color] hover:bg-hover-background focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
          aria-label={t`View invited users`}
        >
          <div>
            <div className="font-medium text-foreground text-sm">
              <Trans>Invited users</Trans>
            </div>
            <div className="mt-1 text-muted-foreground text-sm">
              <Trans>Users who haven't confirmed their email</Trans>
            </div>
          </div>
          <div className="mt-4 font-semibold text-2xl text-foreground">{usersSummary.pendingUsers}</div>
        </Link>
      </div>
    </AppLayout>
  );
}

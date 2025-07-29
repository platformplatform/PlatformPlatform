import FederatedSideMenu from "@/federated-modules/sideMenu/FederatedSideMenu";
import { TopMenu } from "@/shared/components/topMenu";
import { UserStatus, api } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { getDateDaysAgo, getTodayIsoDate } from "@repo/utils/date/formatDate";
import { Link, createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/admin/")({
  component: Home
});

export default function Home() {
  const { data: usersSummary } = api.useQuery("get", "/api/account-management/users/summary");
  const userInfo = useUserInfo();

  return (
    <>
      <FederatedSideMenu currentSystem="account-management" />
      <AppLayout topMenu={<TopMenu />}>
        <h1>{userInfo?.firstName ? <Trans>Welcome home, {userInfo.firstName}</Trans> : <Trans>Welcome home</Trans>}</h1>
        <p>
          <Trans>Here's your overview of what's happening.</Trans>
        </p>
        <div className="grid w-full grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
          <Link
            to="/admin/users"
            className="flex flex-col justify-between rounded-xl bg-input-background p-6 transition-colors hover:bg-hover-background focus-visible:outline focus-visible:outline-2 focus-visible:outline-ring"
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
            <div className="mt-4 font-semibold text-2xl text-foreground">{usersSummary?.totalUsers ?? "-"}</div>
          </Link>
          <Link
            to="/admin/users"
            search={{
              userStatus: UserStatus.Active,
              startDate: getDateDaysAgo(30),
              endDate: getTodayIsoDate()
            }}
            className="flex flex-col justify-between rounded-xl bg-input-background p-6 transition-colors hover:bg-hover-background focus-visible:outline focus-visible:outline-2 focus-visible:outline-ring"
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
            <div className="mt-4 font-semibold text-2xl text-foreground">{usersSummary?.activeUsers ?? "-"}</div>
          </Link>
          <Link
            to="/admin/users"
            search={{ userStatus: UserStatus.Pending }}
            className="flex flex-col justify-between rounded-xl bg-input-background p-6 transition-colors hover:bg-hover-background focus-visible:outline focus-visible:outline-2 focus-visible:outline-ring"
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
            <div className="mt-4 font-semibold text-2xl text-foreground">{usersSummary?.pendingUsers ?? "-"}</div>
          </Link>
        </div>
      </AppLayout>
    </>
  );
}

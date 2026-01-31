import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@repo/ui/components/Card";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { getDateDaysAgo, getTodayIsoDate } from "@repo/utils/date/formatDate";
import { createFileRoute, Link } from "@tanstack/react-router";
import FederatedSideMenu from "@/federated-modules/sideMenu/FederatedSideMenu";
import { TopMenu } from "@/shared/components/topMenu";
import { api, UserStatus } from "@/shared/lib/api/client";

export const Route = createFileRoute("/admin/")({
  component: Home
});

export default function Home() {
  const { data: usersSummary, isLoading } = api.useQuery("get", "/api/account-management/users/summary");
  const userInfo = useUserInfo();

  return (
    <>
      <FederatedSideMenu currentSystem="account-management" />
      <AppLayout
        topMenu={<TopMenu />}
        title={userInfo?.firstName ? t`Welcome home, ${userInfo.firstName}` : t`Welcome home`}
        subtitle={t`Here's your overview of what's happening.`}
      >
        <div className="grid w-full grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
          <Link
            to="/admin/users"
            className="rounded-xl outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
            aria-label={t`View users`}
          >
            <Card className="h-full transition-colors hover:bg-hover-background">
              <CardHeader>
                <CardTitle>
                  <Trans>Total users</Trans>
                </CardTitle>
                <CardDescription>
                  <Trans>Add more in the Users menu</Trans>
                </CardDescription>
              </CardHeader>
              <CardContent>
                {isLoading ? (
                  <Skeleton className="h-8 w-12" />
                ) : (
                  <div className="font-semibold text-2xl text-foreground">{usersSummary?.totalUsers ?? "-"}</div>
                )}
              </CardContent>
            </Card>
          </Link>
          <Link
            to="/admin/users"
            search={{
              userStatus: UserStatus.Active,
              startDate: getDateDaysAgo(30),
              endDate: getTodayIsoDate()
            }}
            className="rounded-xl outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
            aria-label={t`View active users`}
          >
            <Card className="h-full transition-colors hover:bg-hover-background">
              <CardHeader>
                <CardTitle>
                  <Trans>Active users</Trans>
                </CardTitle>
                <CardDescription>
                  <Trans>Active users in the past 30 days</Trans>
                </CardDescription>
              </CardHeader>
              <CardContent>
                {isLoading ? (
                  <Skeleton className="h-8 w-12" />
                ) : (
                  <div className="font-semibold text-2xl text-foreground">{usersSummary?.activeUsers ?? "-"}</div>
                )}
              </CardContent>
            </Card>
          </Link>
          <Link
            to="/admin/users"
            search={{ userStatus: UserStatus.Pending }}
            className="rounded-xl outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
            aria-label={t`View invited users`}
          >
            <Card className="h-full transition-colors hover:bg-hover-background">
              <CardHeader>
                <CardTitle>
                  <Trans>Invited users</Trans>
                </CardTitle>
                <CardDescription>
                  <Trans>Users who haven't confirmed their email</Trans>
                </CardDescription>
              </CardHeader>
              <CardContent>
                {isLoading ? (
                  <Skeleton className="h-8 w-12" />
                ) : (
                  <div className="font-semibold text-2xl text-foreground">{usersSummary?.pendingUsers ?? "-"}</div>
                )}
              </CardContent>
            </Card>
          </Link>
        </div>
      </AppLayout>
    </>
  );
}

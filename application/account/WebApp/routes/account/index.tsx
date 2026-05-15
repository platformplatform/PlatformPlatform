import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { isFeatureFlagEnabled } from "@repo/infrastructure/featureFlags/useFeatureFlag";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { LinkCard } from "@repo/ui/components/LinkCard";
import { getDateDaysAgo, getTodayIsoDate } from "@repo/utils/date/formatDate";
import { createFileRoute, redirect } from "@tanstack/react-router";

import { api, UserStatus } from "@/shared/lib/api/client";

export const Route = createFileRoute("/account/")({
  beforeLoad: () => {
    // The /account dashboard is gated by the account-overview feature flag. When the flag is off
    // the AccountSideMenu hides the entry, but the route remains reachable by typing the URL or
    // following a stale bookmark. Redirect to /account/users so direct navigation lands on a
    // surface the user can actually use.
    if (!isFeatureFlagEnabled("account-overview")) {
      throw redirect({ to: "/account/users" });
    }
  },
  staticData: { trackingTitle: "Account overview" },
  component: Home
});

export default function Home() {
  const { data: usersSummary } = api.useQuery("get", "/api/account/users/summary");

  return (
    <AppLayout
      variant="center"
      maxWidth="64rem"
      title={t`Account overview`}
      subtitle={t`A quick summary of your account activity.`}
    >
      <div className="grid w-full grid-cols-1 gap-4 pt-8 md:grid-cols-2 lg:grid-cols-3">
        <LinkCard to="/account/users" aria-label={t`View users`} className="justify-between">
          <div>
            <div className="text-sm font-medium text-foreground">
              <Trans>Total users</Trans>
            </div>
            <div className="mt-1 text-sm text-muted-foreground">
              <Trans>Add more in the Users menu</Trans>
            </div>
          </div>
          <div className="mt-4 text-2xl font-semibold text-foreground">{usersSummary?.totalUsers ?? "-"}</div>
        </LinkCard>
        <LinkCard
          to="/account/users"
          search={{
            userStatus: UserStatus.Active,
            startDate: getDateDaysAgo(30),
            endDate: getTodayIsoDate()
          }}
          aria-label={t`View active users`}
          className="justify-between"
        >
          <div>
            <div className="text-sm font-medium text-foreground">
              <Trans>Active users</Trans>
            </div>
            <div className="mt-1 text-sm text-muted-foreground">
              <Trans>Active users in the past 30 days</Trans>
            </div>
          </div>
          <div className="mt-4 text-2xl font-semibold text-foreground">{usersSummary?.activeUsers ?? "-"}</div>
        </LinkCard>
        <LinkCard
          to="/account/users"
          search={{ userStatus: UserStatus.Pending }}
          aria-label={t`View invited users`}
          className="justify-between"
        >
          <div>
            <div className="text-sm font-medium text-foreground">
              <Trans>Invited users</Trans>
            </div>
            <div className="mt-1 text-sm text-muted-foreground">
              <Trans>Users who haven't confirmed their email</Trans>
            </div>
          </div>
          <div className="mt-4 text-2xl font-semibold text-foreground">{usersSummary?.pendingUsers ?? "-"}</div>
        </LinkCard>
      </div>
    </AppLayout>
  );
}

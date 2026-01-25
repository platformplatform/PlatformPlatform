import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { createFileRoute } from "@tanstack/react-router";
import { LayoutDashboardIcon } from "lucide-react";
import { MainSideMenu } from "@/shared/components/MainSideMenu";
import { TopMenu } from "@/shared/components/topMenu";

export const Route = createFileRoute("/dashboard/")({
  component: DashboardPage
});

function EmptyDashboard() {
  return (
    <div className="flex flex-1 flex-col items-center justify-center gap-4 text-center">
      <div className="flex size-16 items-center justify-center rounded-full bg-muted">
        <LayoutDashboardIcon className="size-8 text-muted-foreground" />
      </div>
      <div className="flex flex-col gap-2">
        <h2 className="text-muted-foreground">
          <Trans>Your dashboard is empty</Trans>
        </h2>
        <p className="max-w-md text-muted-foreground text-sm">
          <Trans>This is where your personalized dashboard content will appear. Stay tuned for updates.</Trans>
        </p>
      </div>
    </div>
  );
}

function getTimeBasedGreeting(firstName: string | undefined) {
  const hour = new Date().getHours();
  if (hour >= 0 && hour < 5) {
    return firstName ? t`Burning the midnight oil, ${firstName}?` : t`Burning the midnight oil?`;
  }
  if (hour >= 5 && hour < 12) {
    return firstName ? t`Good morning, ${firstName}` : t`Good morning`;
  }
  if (hour >= 12 && hour < 17) {
    return firstName ? t`Good afternoon, ${firstName}` : t`Good afternoon`;
  }
  return firstName ? t`Good evening, ${firstName}` : t`Good evening`;
}

function DashboardPage() {
  const userInfo = useUserInfo();

  return (
    <>
      <MainSideMenu />
      <AppLayout
        topMenu={<TopMenu />}
        title={getTimeBasedGreeting(userInfo?.firstName)}
        subtitle={t`Here's your overview of what's happening.`}
      >
        <EmptyDashboard />
      </AppLayout>
    </>
  );
}

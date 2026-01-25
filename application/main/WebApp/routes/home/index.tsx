import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { createFileRoute } from "@tanstack/react-router";
import { LayoutDashboardIcon } from "lucide-react";
import { MainAppLayout } from "@/shared/components/MainAppLayout";
import { MainSideMenu } from "@/shared/components/MainSideMenu";

export const Route = createFileRoute("/home/")({
  component: HomePage
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

function HomePage() {
  const userInfo = useUserInfo();
  const accountName = userInfo?.tenantName ?? "PlatformPlatform";

  return (
    <>
      <MainSideMenu />
      <MainAppLayout title={t`Welcome to ${accountName}`}>
        <EmptyDashboard />
      </MainAppLayout>
    </>
  );
}

import { SharedSideMenu } from "@/shared/components/SharedSideMenu";
import { TopMenu } from "@/shared/components/topMenu";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/back-office/")({
  component: Home
});

export default function Home() {
  return (
    <div className="flex h-full w-full gap-4">
      <SharedSideMenu ariaLabel={t`Toggle collapsed menu`} />
      <div className="flex w-full flex-col gap-4 px-4 py-3">
        <TopMenu />
        <div className="mb-4 flex h-20 w-full items-center justify-between space-x-2 sm:mt-4">
          <div className="mt-3 flex flex-col gap-2 font-semibold text-3xl text-foreground">
            <h1>
              <Trans>Welcome to the Back Office</Trans>
            </h1>
            <p className="font-normal text-muted-foreground text-sm">
              <Trans>
                Manage tenants, view system data, see exceptions, and perform various tasks for operational and support
                teams.
              </Trans>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}

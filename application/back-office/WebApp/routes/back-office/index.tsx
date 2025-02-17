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
    <div className="flex gap-4 w-full h-full">
      <SharedSideMenu ariaLabel={t`Toggle collapsed menu`} />
      <div className="flex flex-col gap-4 py-3 px-4 w-full">
        <TopMenu />
        <div className="flex h-20 w-full items-center justify-between space-x-2 sm:mt-4 mb-4">
          <div className="text-foreground text-3xl font-semibold flex gap-2 flex-col mt-3">
            <h1>
              <Trans>Welcome to the Back Office</Trans>
            </h1>
            <p className="text-muted-foreground text-sm font-normal">
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

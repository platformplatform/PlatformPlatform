import { TopMenu } from "@/shared/components/topMenu";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { createFileRoute } from "@tanstack/react-router";
import FederatedSideMenu from "account-management/FederatedSideMenu";

export const Route = createFileRoute("/back-office/")({
  component: Home
});

export default function Home() {
  return (
    <>
      <FederatedSideMenu currentSystem="back-office" />
      <AppLayout topMenu={<TopMenu />}>
        <h1>
          <Trans>Welcome to the Back Office</Trans>
        </h1>
        <p>
          <Trans>
            Manage tenants, view system data, see exceptions, and perform various tasks for operational and support
            teams.
          </Trans>
        </p>
      </AppLayout>
    </>
  );
}

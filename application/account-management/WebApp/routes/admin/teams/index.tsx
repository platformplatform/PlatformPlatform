import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Breadcrumb } from "@repo/ui/components/Breadcrumbs";
import { createFileRoute } from "@tanstack/react-router";
import { z } from "zod";
import FederatedSideMenu from "@/federated-modules/sideMenu/FederatedSideMenu";
import { TopMenu } from "@/shared/components/topMenu";

const teamsPageSearchSchema = z.object({
  teamId: z.string().optional()
});

export const Route = createFileRoute("/admin/teams/")({
  component: TeamsPage,
  validateSearch: teamsPageSearchSchema
});

export default function TeamsPage() {
  return (
    <>
      <FederatedSideMenu currentSystem="account-management" />
      <AppLayout
        topMenu={
          <TopMenu>
            <Breadcrumb href="/admin/teams">
              <Trans>Teams</Trans>
            </Breadcrumb>
            <Breadcrumb>
              <Trans>All teams</Trans>
            </Breadcrumb>
          </TopMenu>
        }
        title={t`Teams`}
        subtitle={t`Manage your teams and team members here.`}
        scrollAwayHeader={true}
      ></AppLayout>
    </>
  );
}

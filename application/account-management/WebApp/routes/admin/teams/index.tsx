import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Breadcrumb } from "@repo/ui/components/Breadcrumbs";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useEffect, useState } from "react";
import { z } from "zod";
import FederatedSideMenu from "@/federated-modules/sideMenu/FederatedSideMenu";
import { TopMenu } from "@/shared/components/topMenu";
import { TeamDetailsSidePane } from "./-components/TeamDetailsSidePane";
import { TeamsTable } from "./-components/TeamsTable";
import type { TeamDetails } from "./-data/mockTeams";
import { mockTeams } from "./-data/mockTeams";

const teamsPageSearchSchema = z.object({
  teamId: z.string().optional()
});

export const Route = createFileRoute("/admin/teams/")({
  component: TeamsPage,
  validateSearch: teamsPageSearchSchema
});

export default function TeamsPage() {
  const [selectedTeam, setSelectedTeam] = useState<TeamDetails | null>(null);
  const navigate = useNavigate({ from: Route.fullPath });
  const { teamId } = Route.useSearch();

  useEffect(() => {
    if (teamId) {
      const team = mockTeams.find((t) => t.id === teamId);
      if (team) {
        setSelectedTeam(team);
      }
    }
  }, [teamId]);

  const handleSelectedTeamChange = (team: TeamDetails | null) => {
    setSelectedTeam(team);
    if (team) {
      navigate({ search: (previous) => ({ ...previous, teamId: team.id }) });
    } else {
      navigate({ search: (previous) => ({ ...previous, teamId: undefined }) });
    }
  };

  const handleCloseTeamDetails = () => {
    setSelectedTeam(null);
    navigate({ search: (previous) => ({ ...previous, teamId: undefined }) });
  };

  return (
    <>
      <FederatedSideMenu currentSystem="account-management" />
      <AppLayout
        sidePane={
          selectedTeam ? (
            <TeamDetailsSidePane team={selectedTeam} isOpen={!!selectedTeam} onClose={handleCloseTeamDetails} />
          ) : undefined
        }
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
      >
        <div className="flex min-h-0 flex-1 flex-col">
          <TeamsTable
            selectedTeam={selectedTeam}
            onSelectedTeamChange={handleSelectedTeamChange}
            isTeamDetailsPaneOpen={!!selectedTeam}
          />
        </div>
      </AppLayout>
    </>
  );
}

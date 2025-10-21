import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Breadcrumb } from "@repo/ui/components/Breadcrumbs";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useEffect, useState } from "react";
import { z } from "zod";
import FederatedSideMenu from "@/federated-modules/sideMenu/FederatedSideMenu";
import { TopMenu } from "@/shared/components/topMenu";
import { api, type components } from "@/shared/lib/api/client";
import { DeleteTeamDialog } from "./-components/DeleteTeamDialog";
import { EditTeamDialog } from "./-components/EditTeamDialog";
import { TeamDetailsSidePane } from "./-components/TeamDetailsSidePane";
import { TeamsTable } from "./-components/TeamsTable";
import { TeamsToolbar } from "./-components/TeamsToolbar";

type TeamSummary = components["schemas"]["TeamSummary"];

const teamsPageSearchSchema = z.object({
  teamId: z.string().optional()
});

export const Route = createFileRoute("/admin/teams/")({
  component: TeamsPage,
  validateSearch: teamsPageSearchSchema
});

export default function TeamsPage() {
  const [selectedTeam, setSelectedTeam] = useState<TeamSummary | null>(null);
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const navigate = useNavigate({ from: Route.fullPath });
  const { teamId } = Route.useSearch();

  const { data: teamsData } = api.useQuery("get", "/api/account-management/teams");
  const teams = teamsData?.teams || [];

  useEffect(() => {
    if (teamId) {
      const team = teams.find((t) => t.id === teamId);
      if (team) {
        setSelectedTeam(team);
      }
    }
  }, [teamId, teams]);

  const handleSelectedTeamChange = (team: TeamSummary | null) => {
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

  const handleEditTeam = () => {
    setIsEditDialogOpen(true);
  };

  const handleDeleteTeam = () => {
    setIsDeleteDialogOpen(true);
  };

  const handleTeamDeleted = () => {
    handleCloseTeamDetails();
  };

  return (
    <>
      <FederatedSideMenu currentSystem="account-management" />
      <AppLayout
        sidePane={
          selectedTeam ? (
            <TeamDetailsSidePane
              team={selectedTeam}
              isOpen={!!selectedTeam}
              onClose={handleCloseTeamDetails}
              onEditTeam={handleEditTeam}
              onDeleteTeam={handleDeleteTeam}
            />
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
        <div className="max-sm:sticky max-sm:top-12 max-sm:z-30">
          <TeamsToolbar />
        </div>
        <div className="flex min-h-0 flex-1 flex-col">
          <TeamsTable
            teams={teams}
            selectedTeam={selectedTeam}
            onSelectedTeamChange={handleSelectedTeamChange}
            isTeamDetailsPaneOpen={!!selectedTeam}
          />
        </div>
      </AppLayout>

      <EditTeamDialog team={selectedTeam} isOpen={isEditDialogOpen} onOpenChange={setIsEditDialogOpen} />

      <DeleteTeamDialog
        team={selectedTeam}
        isOpen={isDeleteDialogOpen}
        onOpenChange={setIsDeleteDialogOpen}
        onTeamDeleted={handleTeamDeleted}
      />
    </>
  );
}

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Breadcrumb } from "@repo/ui/components/Breadcrumbs";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useEffect, useState } from "react";
import { z } from "zod";
import FederatedSideMenu from "@/federated-modules/sideMenu/FederatedSideMenu";
import { TopMenu } from "@/shared/components/topMenu";
import { DeleteTeamDialog } from "./-components/DeleteTeamDialog";
import { EditTeamDialog } from "./-components/EditTeamDialog";
import { EditTeamMembersDialog } from "./-components/EditTeamMembersDialog";
import { TeamDetailsSidePane } from "./-components/TeamDetailsSidePane";
import { TeamsTable } from "./-components/TeamsTable";
import { TeamsToolbar } from "./-components/TeamsToolbar";
import type { TeamMemberDetails } from "./-data/mockTeamMembers";
import { mockTeamMembers } from "./-data/mockTeamMembers";
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
  const [teams, setTeams] = useState<TeamDetails[]>(mockTeams);
  const [selectedTeam, setSelectedTeam] = useState<TeamDetails | null>(null);
  const [teamMembers, setTeamMembers] = useState<Record<string, TeamMemberDetails[]>>(mockTeamMembers);
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const [isEditMembersDialogOpen, setIsEditMembersDialogOpen] = useState(false);
  const navigate = useNavigate({ from: Route.fullPath });
  const { teamId } = Route.useSearch();

  useEffect(() => {
    if (teamId) {
      const team = teams.find((t) => t.id === teamId);
      if (team) {
        setSelectedTeam(team);
      }
    }
  }, [teamId, teams]);

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

  const handleTeamCreated = (team: TeamDetails) => {
    setTeams((previousTeams) => [...previousTeams, team]);
  };

  const handleTeamUpdated = (updatedTeam: TeamDetails) => {
    setTeams((previousTeams) => previousTeams.map((team) => (team.id === updatedTeam.id ? updatedTeam : team)));
    setSelectedTeam(updatedTeam);
  };

  const handleTeamDeleted = () => {
    if (selectedTeam) {
      setTeams((previousTeams) => previousTeams.filter((team) => team.id !== selectedTeam.id));
      handleCloseTeamDetails();
    }
  };

  const handleEditTeam = () => {
    setIsEditDialogOpen(true);
  };

  const handleDeleteTeam = () => {
    setIsDeleteDialogOpen(true);
  };

  const handleEditMembers = () => {
    setIsEditMembersDialogOpen(true);
  };

  const handleMembersUpdated = (members: TeamMemberDetails[]) => {
    if (selectedTeam) {
      setTeamMembers((prev) => ({
        ...prev,
        [selectedTeam.id]: members
      }));
      setTeams((previousTeams) =>
        previousTeams.map((team) => (team.id === selectedTeam.id ? { ...team, memberCount: members.length } : team))
      );
    }
  };

  return (
    <>
      <FederatedSideMenu currentSystem="account-management" />
      <AppLayout
        sidePane={
          selectedTeam ? (
            <TeamDetailsSidePane
              team={selectedTeam}
              teamMembers={teamMembers[selectedTeam.id] || []}
              isOpen={!!selectedTeam}
              onClose={handleCloseTeamDetails}
              onEditTeam={handleEditTeam}
              onDeleteTeam={handleDeleteTeam}
              onEditMembers={handleEditMembers}
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
          <TeamsToolbar onTeamCreated={handleTeamCreated} />
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

      <EditTeamDialog
        team={selectedTeam}
        isOpen={isEditDialogOpen}
        onOpenChange={setIsEditDialogOpen}
        onTeamUpdated={handleTeamUpdated}
      />

      <DeleteTeamDialog
        team={selectedTeam}
        isOpen={isDeleteDialogOpen}
        onOpenChange={setIsDeleteDialogOpen}
        onTeamDeleted={handleTeamDeleted}
      />

      <EditTeamMembersDialog
        team={selectedTeam}
        currentMembers={selectedTeam ? teamMembers[selectedTeam.id] || [] : []}
        isOpen={isEditMembersDialogOpen}
        onOpenChange={setIsEditMembersDialogOpen}
        onMembersUpdated={handleMembersUpdated}
      />
    </>
  );
}

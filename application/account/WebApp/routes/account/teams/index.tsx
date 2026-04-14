import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Button } from "@repo/ui/components/Button";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { PlusIcon } from "lucide-react";
import { useState } from "react";
import { z } from "zod";

import { api, type Schemas } from "@/shared/lib/api/client";

import { CreateTeamDialog } from "./-components/CreateTeamDialog";
import { DeleteTeamConfirm } from "./-components/DeleteTeamConfirm";
import { EditTeamDialog } from "./-components/EditTeamDialog";
import { EditTeamMembersDialog } from "./-components/EditTeamMembersDialog";
import { TeamDetailsPane } from "./-components/TeamDetailsPane";
import { TeamsTable } from "./-components/TeamsTable";

type Team = Schemas["TeamResponse"];

const teamsPageSearchSchema = z.object({
  teamId: z.string().optional()
});

export const Route = createFileRoute("/account/teams/")({
  staticData: { trackingTitle: "Teams" },
  component: TeamsPage,
  validateSearch: teamsPageSearchSchema
});

export default function TeamsPage() {
  const navigate = useNavigate({ from: Route.fullPath });
  const { teamId } = Route.useSearch();
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [teamToEdit, setTeamToEdit] = useState<Team | null>(null);
  const [teamToEditMembers, setTeamToEditMembers] = useState<Team | null>(null);
  const [teamToDelete, setTeamToDelete] = useState<Team | null>(null);

  const { data, isLoading } = api.useQuery("get", "/api/account/teams");
  const teams: Team[] = data?.teams ?? [];
  const selectedTeam = teamId ? (teams.find((team) => team.id === teamId) ?? null) : null;

  const openDetails = (team: Team) => {
    navigate({ search: (prev) => ({ ...prev, teamId: team.id }) });
  };

  const closeDetails = () => {
    navigate({ search: (prev) => ({ ...prev, teamId: undefined }) });
  };

  const sidePane = selectedTeam ? (
    <TeamDetailsPane
      team={selectedTeam}
      isOpen={true}
      onClose={closeDetails}
      onEdit={() => setTeamToEdit(selectedTeam)}
      onEditMembers={() => setTeamToEditMembers(selectedTeam)}
      onDelete={() => setTeamToDelete(selectedTeam)}
    />
  ) : undefined;

  return (
    <>
      <AppLayout
        variant="center"
        maxWidth="64rem"
        title={t`Teams`}
        subtitle={t`Manage your organization's teams here.`}
        sidePane={sidePane}
      >
        {teams.length > 0 && (
          <div className="mb-4 flex items-center justify-end gap-2">
            <Button onClick={() => setIsCreateOpen(true)}>
              <PlusIcon className="size-5" />
              <Trans>Create team</Trans>
            </Button>
          </div>
        )}
        <TeamsTable
          teams={teams}
          isLoading={isLoading}
          onCreateTeam={() => setIsCreateOpen(true)}
          onRowClick={openDetails}
          onEditTeam={setTeamToEdit}
          onDeleteTeam={setTeamToDelete}
        />
      </AppLayout>

      <CreateTeamDialog isOpen={isCreateOpen} onOpenChange={setIsCreateOpen} />

      <EditTeamDialog
        team={teamToEdit}
        isOpen={teamToEdit !== null}
        onOpenChange={(open) => !open && setTeamToEdit(null)}
      />

      <EditTeamMembersDialog
        team={teamToEditMembers}
        isOpen={teamToEditMembers !== null}
        onOpenChange={(open) => !open && setTeamToEditMembers(null)}
      />

      <DeleteTeamConfirm
        team={teamToDelete}
        isOpen={teamToDelete !== null}
        onOpenChange={(open) => !open && setTeamToDelete(null)}
        onDeleted={() => {
          setTeamToDelete(null);
          if (teamId === teamToDelete?.id) {
            closeDetails();
          }
        }}
      />
    </>
  );
}

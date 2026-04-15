import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
import { Empty, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TeamsIcon } from "@repo/ui/icons/TeamsIcon";
import { useQueries } from "@tanstack/react-query";
import { EllipsisVerticalIcon, PencilIcon, PlusIcon, Trash2Icon } from "lucide-react";

import { api, type Schemas } from "@/shared/lib/api/client";

type Team = Schemas["TeamResponse"];

interface TeamsTableProps {
  teams: Team[];
  isLoading: boolean;
  onCreateTeam: () => void;
  onRowClick: (team: Team) => void;
  onEditTeam: (team: Team) => void;
  onDeleteTeam: (team: Team) => void;
}

export function TeamsTable({
  teams,
  isLoading,
  onCreateTeam,
  onRowClick,
  onEditTeam,
  onDeleteTeam
}: Readonly<TeamsTableProps>) {
  const memberCountQueries = useQueries({
    queries: teams.map((team) =>
      api.queryOptions("get", "/api/account/teams/{teamId}/members", { params: { path: { teamId: team.id } } })
    )
  });
  const memberCountByTeamId = new Map<string, number>();
  for (const [index, team] of teams.entries()) {
    const result = memberCountQueries[index];
    if (result?.data) {
      memberCountByTeamId.set(team.id, result.data.members.length);
    }
  }

  if (isLoading) {
    return (
      <div className="flex flex-col gap-2">
        <Skeleton className="h-10 w-full rounded-md" />
        <Skeleton className="h-14 w-full rounded-md" />
        <Skeleton className="h-14 w-full rounded-md" />
        <Skeleton className="h-14 w-full rounded-md" />
      </div>
    );
  }

  if (teams.length === 0) {
    return (
      <Empty>
        <EmptyHeader>
          <EmptyMedia variant="icon">
            <TeamsIcon />
          </EmptyMedia>
          <EmptyTitle>
            <Trans>No teams yet</Trans>
          </EmptyTitle>
          <EmptyDescription>
            <Trans>Create your first team to organize users.</Trans>
          </EmptyDescription>
        </EmptyHeader>
        <Button onClick={onCreateTeam}>
          <PlusIcon className="size-4" />
          <Trans>Create team</Trans>
        </Button>
      </Empty>
    );
  }

  return (
    <Table rowSize="compact" aria-label={t`Teams`}>
      <TableHeader>
        <TableRow>
          <TableHead>
            <Trans>Name</Trans>
          </TableHead>
          <TableHead className="w-[8rem]">
            <Trans>Members</Trans>
          </TableHead>
          <TableHead className="w-[3rem]" />
        </TableRow>
      </TableHeader>
      <TableBody>
        {teams.map((team) => (
          <TableRow
            key={team.id}
            className="cursor-pointer select-none hover:bg-hover-background"
            onClick={() => onRowClick(team)}
          >
            <TableCell>
              <div className="flex h-14 flex-col justify-center">
                <span className="truncate font-medium text-foreground">{team.name}</span>
                {team.description && (
                  <span className="block truncate text-sm text-muted-foreground">{team.description}</span>
                )}
              </div>
            </TableCell>
            <TableCell className="text-muted-foreground">{memberCountByTeamId.get(team.id) ?? "—"}</TableCell>
            <TableCell className="text-right" onClick={(event) => event.stopPropagation()}>
              <DropdownMenu>
                <DropdownMenuTrigger
                  render={
                    <Button variant="ghost" size="icon" aria-label={t`Team actions`}>
                      <EllipsisVerticalIcon className="size-5 text-muted-foreground" />
                    </Button>
                  }
                />
                <DropdownMenuContent className="w-auto">
                  <DropdownMenuItem onClick={() => onEditTeam(team)}>
                    <PencilIcon className="size-4" />
                    <Trans>Edit</Trans>
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem variant="destructive" onClick={() => onDeleteTeam(team)}>
                    <Trash2Icon className="size-4" />
                    <Trans>Delete</Trans>
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}

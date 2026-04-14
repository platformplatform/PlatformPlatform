import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { Item, ItemActions, ItemContent, ItemGroup, ItemTitle } from "@repo/ui/components/Item";
import { Separator } from "@repo/ui/components/Separator";
import { SidePane, SidePaneBody, SidePaneFooter, SidePaneHeader } from "@repo/ui/components/SidePane";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { PencilIcon, Trash2Icon, UserCogIcon } from "lucide-react";
import { toast } from "sonner";

import { api, queryClient, type Schemas, type TeamMemberRole, UserRole } from "@/shared/lib/api/client";

import { TeamMemberRow } from "./TeamMemberRow";

type Team = Schemas["TeamResponse"];

interface TeamDetailsPaneProps {
  team: Team;
  isOpen: boolean;
  onClose: () => void;
  onEdit: () => void;
  onEditMembers: () => void;
  onDelete: () => void;
}

export function TeamDetailsPane({
  team,
  isOpen,
  onClose,
  onEdit,
  onEditMembers,
  onDelete
}: Readonly<TeamDetailsPaneProps>) {
  const formatDate = useFormatDate();
  const userInfo = useUserInfo();
  const canChangeRoles = userInfo?.role === UserRole.Owner || userInfo?.role === UserRole.Admin;
  const { data: membersData } = api.useQuery("get", "/api/account/teams/{teamId}/members", {
    params: { path: { teamId: team.id } }
  });
  const members = membersData?.members ?? [];
  const memberCount = membersData?.members.length;

  const changeRoleMutation = api.useMutation("put", "/api/account/teams/{teamId}/members/{userId}/role", {
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/teams/{teamId}/members"] });
      toast.success(t`Role updated`);
    },
    onError: () => {
      toast.error(t`Failed to update role`);
    }
  });

  const handleChangeRole = (userId: string, role: TeamMemberRole) => {
    changeRoleMutation.mutate({ params: { path: { teamId: team.id, userId } }, body: { userId, role } });
  };

  return (
    <SidePane
      isOpen={isOpen}
      onOpenChange={(open) => !open && onClose()}
      trackingTitle="Team details"
      trackingKey={team.id}
      aria-label={t`Team details`}
    >
      <SidePaneHeader closeButtonLabel={t`Close team details`}>
        <Trans>Team details</Trans>
      </SidePaneHeader>

      <SidePaneBody>
        <div className="mb-6">
          <h4>{team.name}</h4>
          {team.description && <p className="mt-1 text-sm text-muted-foreground">{team.description}</p>}
        </div>

        <ItemGroup>
          <Item size="xs">
            <ItemContent>
              <ItemTitle className="text-sm font-normal">
                <Trans>Created</Trans>
              </ItemTitle>
            </ItemContent>
            <ItemActions>
              <span className="text-sm">{formatDate(team.createdAt, true)}</span>
            </ItemActions>
          </Item>
          <Item size="xs">
            <ItemContent>
              <ItemTitle className="text-sm font-normal">
                <Trans>Updated</Trans>
              </ItemTitle>
            </ItemContent>
            <ItemActions>
              <span className="text-sm">{team.modifiedAt ? formatDate(team.modifiedAt, true) : "—"}</span>
            </ItemActions>
          </Item>
          <Item size="xs">
            <ItemContent>
              <ItemTitle className="text-sm font-normal">
                <Trans>Members</Trans>
              </ItemTitle>
            </ItemContent>
            <ItemActions>
              <span className="text-sm">{memberCount ?? "—"}</span>
            </ItemActions>
          </Item>
        </ItemGroup>

        {members.length > 0 && (
          <>
            <Separator className="my-4" />
            <div className="flex max-h-[20rem] flex-col gap-1 overflow-y-auto">
              {members.map((member) => (
                <TeamMemberRow
                  key={member.userId}
                  member={member}
                  onChangeRole={canChangeRoles ? handleChangeRole : undefined}
                />
              ))}
            </div>
          </>
        )}

        <Button variant="secondary" onClick={onEditMembers} className="mt-4 w-full justify-center">
          <UserCogIcon className="size-4" />
          <Trans>Edit team members</Trans>
        </Button>
      </SidePaneBody>

      <SidePaneFooter className="flex flex-col gap-2">
        <Button variant="secondary" onClick={onEdit} className="w-full justify-center">
          <PencilIcon className="size-4" />
          <Trans>Edit team</Trans>
        </Button>
        <Button variant="destructive" onClick={onDelete} className="w-full justify-center">
          <Trash2Icon className="size-4" />
          <Trans>Delete team</Trans>
        </Button>
      </SidePaneFooter>
    </SidePane>
  );
}

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { AlertDialog } from "@repo/ui/components/AlertDialog";
import { Avatar } from "@repo/ui/components/Avatar";
import { Button } from "@repo/ui/components/Button";
import { Dialog } from "@repo/ui/components/Dialog";
import { DialogContent, DialogFooter, DialogHeader } from "@repo/ui/components/DialogFooter";
import { Form } from "@repo/ui/components/Form";
import { FormErrorMessage } from "@repo/ui/components/FormErrorMessage";
import { Heading } from "@repo/ui/components/Heading";
import { Modal } from "@repo/ui/components/Modal";
import { SearchField } from "@repo/ui/components/SearchField";
import { Select, SelectItem } from "@repo/ui/components/Select";
import { Text } from "@repo/ui/components/Text";
import { toastQueue } from "@repo/ui/components/Toast";
import { Tooltip, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { useDebounce } from "@repo/ui/hooks/useDebounce";
import { getInitials } from "@repo/utils/string/getInitials";
import { useQueryClient } from "@tanstack/react-query";
import { ArrowLeftIcon, ArrowRightIcon, XIcon } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { api, type components } from "@/shared/lib/api/client";
import type { TeamMemberDetails } from "../-data/mockTeamMembers";
import type { TeamDetails } from "../-data/mockTeams";
import type { TenantUser } from "../-data/mockTenantUsers";
import { mockTenantUsers } from "../-data/mockTenantUsers";

type TeamMemberRole = components["schemas"]["TeamMemberRole"];

interface EditTeamMembersDialogProps {
  team: TeamDetails | null;
  currentMembers: TeamMemberDetails[];
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
}

interface TeamMemberWithRole {
  userId: string;
  memberId?: string;
  name: string;
  email: string;
  title: string;
  avatarUrl: string | null;
  role: TeamMemberRole;
}

export function EditTeamMembersDialog({
  team,
  currentMembers,
  isOpen,
  onOpenChange
}: Readonly<EditTeamMembersDialogProps>) {
  const userInfo = useUserInfo();
  const queryClient = useQueryClient();
  const [searchQuery, setSearchQuery] = useState("");
  const debouncedSearchQuery = useDebounce(searchQuery, 300);
  const [teamMembers, setTeamMembers] = useState<TeamMemberWithRole[]>([]);
  const [selectedAvailableUsers, setSelectedAvailableUsers] = useState<Set<string>>(new Set());
  const [selectedTeamMembers, setSelectedTeamMembers] = useState<Set<string>>(new Set());
  const [changedRoles, setChangedRoles] = useState<Map<string, "Admin" | "Member">>(new Map());
  const [memberToRemove, setMemberToRemove] = useState<TeamMemberWithRole | null>(null);

  const updateTeamMembersMutation = api.useMutation("put", "/api/account-management/teams/{teamId}/members", {
    onSuccess: () => {
      if (team?.id) {
        queryClient.invalidateQueries({
          queryKey: ["/api/account-management/teams/{teamId}/members", { teamId: team.id }]
        });

        queryClient.invalidateQueries({
          queryKey: ["/api/account-management/teams"]
        });
      }

      toastQueue.add({
        title: t`Success`,
        description: t`Team members updated successfully`,
        variant: "success"
      });

      onOpenChange(false);
    }
  });

  useEffect(() => {
    if (isOpen && team) {
      const membersWithRole: TeamMemberWithRole[] = currentMembers.map((member) => ({
        userId: member.userId,
        memberId: member.id,
        name: member.name,
        email: member.email,
        title: member.title,
        avatarUrl: member.avatarUrl,
        role: member.role as TeamMemberRole
      }));
      setTeamMembers(membersWithRole);
      setSelectedAvailableUsers(new Set());
      setSelectedTeamMembers(new Set());
      setSearchQuery("");
      setChangedRoles(new Map());
    }
  }, [isOpen, team, currentMembers]);

  const availableUsers = useMemo(() => {
    const memberUserIds = new Set(teamMembers.map((m) => m.userId));
    return mockTenantUsers.filter((user) => !memberUserIds.has(user.userId));
  }, [teamMembers]);

  const filterUser = (user: TenantUser | TeamMemberWithRole, query: string) => {
    if (!query) {
      return true;
    }
    const lowerQuery = query.toLowerCase();
    return (
      user.name.toLowerCase().includes(lowerQuery) ||
      user.email.toLowerCase().includes(lowerQuery) ||
      user.title.toLowerCase().includes(lowerQuery)
    );
  };

  const filteredAvailableUsers = useMemo(
    () => availableUsers.filter((user) => filterUser(user, debouncedSearchQuery)),
    [availableUsers, debouncedSearchQuery]
  );

  const filteredTeamMembers = useMemo(
    () => teamMembers.filter((member) => filterUser(member, debouncedSearchQuery)),
    [teamMembers, debouncedSearchQuery]
  );

  const currentUserMember = teamMembers.find((m) => m.email === userInfo?.email);
  const isCurrentUserAdmin = currentUserMember?.role === "Admin";

  const handleAddMembers = () => {
    const usersToAdd: TeamMemberWithRole[] = mockTenantUsers
      .filter((user) => selectedAvailableUsers.has(user.userId))
      .map((user) => ({
        userId: user.userId,
        name: user.name,
        email: user.email,
        title: user.title,
        avatarUrl: user.avatarUrl,
        role: "Member" as TeamMemberRole
      }));

    setTeamMembers((prev) => [...prev, ...usersToAdd]);
    setSelectedAvailableUsers(new Set());
  };

  const handleRemoveMembers = () => {
    const userIdsToRemove = new Set(selectedTeamMembers);
    setTeamMembers((prev) => prev.filter((member) => !userIdsToRemove.has(member.userId)));
    setSelectedTeamMembers(new Set());
  };

  const handleRoleChange = (userId: string, newRole: TeamMemberRole) => {
    setTeamMembers((prev) => prev.map((member) => (member.userId === userId ? { ...member, role: newRole } : member)));
    setChangedRoles((prev) => new Map(prev).set(userId, newRole));
  };

  const handleRemoveMemberClick = (member: TeamMemberWithRole) => {
    setMemberToRemove(member);
  };

  const handleRemoveMemberConfirm = () => {
    if (!memberToRemove) {
      return;
    }

    setTeamMembers((prev) => prev.filter((member) => member.userId !== memberToRemove.userId));
    setMemberToRemove(null);
  };

  const canRemove = (userId: string) => {
    const isTenantOwner = userInfo?.role === "Owner";
    const member = teamMembers.find((m) => m.userId === userId);
    const isCurrentUser = member?.email === userInfo?.email;

    if (isTenantOwner) {
      return true;
    }

    if (!isCurrentUserAdmin) {
      return false;
    }

    return !isCurrentUser;
  };

  const canChangeRole = (userId: string) => {
    if (!isCurrentUserAdmin) {
      return false;
    }
    const member = teamMembers.find((m) => m.userId === userId);
    return member?.email !== userInfo?.email;
  };

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!team?.id) {
      return;
    }

    const membersToAdd = Array.from(selectedAvailableUsers).map((userId) => ({
      userId,
      role: "Member" as TeamMemberRole
    }));

    const memberIdsToRemove = Array.from(selectedTeamMembers);

    updateTeamMembersMutation.mutate({
      params: { path: { teamId: team.id } },
      body: {
        membersToAdd,
        memberIdsToRemove
      }
    });
  };

  const handleCancel = () => {
    setSearchQuery("");
    setSelectedAvailableUsers(new Set());
    setSelectedTeamMembers(new Set());
    setChangedRoles(new Map());
    onOpenChange(false);
  };

  const toggleAvailableUser = (userId: string) => {
    setSelectedAvailableUsers((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(userId)) {
        newSet.delete(userId);
      } else {
        newSet.add(userId);
      }
      return newSet;
    });
  };

  const toggleTeamMember = (userId: string) => {
    if (!canRemove(userId)) {
      return;
    }
    setSelectedTeamMembers((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(userId)) {
        newSet.delete(userId);
      } else {
        newSet.add(userId);
      }
      return newSet;
    });
  };

  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} isDismissable={true}>
      <Dialog className="sm:w-dialog-lg">
        <XIcon onClick={handleCancel} className="absolute top-2 right-2 h-10 w-10 cursor-pointer p-2 hover:bg-muted" />
        <DialogHeader>
          <Heading slot="title" className="text-2xl">
            <Trans>Edit Team Members</Trans>
          </Heading>
        </DialogHeader>

        <Form onSubmit={handleSubmit} className="flex flex-col max-sm:h-full">
          <DialogContent className="flex flex-col gap-4">
            <SearchField
              label={t`Search`}
              placeholder={t`Search by name, email, or title`}
              value={searchQuery}
              onChange={setSearchQuery}
            />

            <div className="grid grid-cols-[1fr_auto_1fr] gap-4">
              <div className="flex flex-col gap-2">
                <Heading level={4} className="font-medium text-sm">
                  <Trans>Available Users</Trans>{" "}
                  {debouncedSearchQuery && filteredAvailableUsers.length !== availableUsers.length
                    ? `(${filteredAvailableUsers.length} of ${availableUsers.length})`
                    : `(${availableUsers.length})`}
                </Heading>
                <div className="h-80 overflow-y-auto rounded-md border border-border bg-background">
                  {filteredAvailableUsers.length === 0 ? (
                    <div className="flex h-full items-center justify-center p-4">
                      <Text className="text-muted-foreground text-sm">
                        {debouncedSearchQuery ? <Trans>No results found</Trans> : <Trans>No available users</Trans>}
                      </Text>
                    </div>
                  ) : (
                    <div className="space-y-1 p-2">
                      {filteredAvailableUsers.map((user) => (
                        <button
                          key={user.userId}
                          type="button"
                          onClick={() => toggleAvailableUser(user.userId)}
                          className={`flex w-full items-center gap-3 rounded-md p-2 text-left transition-colors ${
                            selectedAvailableUsers.has(user.userId)
                              ? "bg-primary text-primary-foreground"
                              : "hover:bg-muted"
                          }`}
                        >
                          <Avatar
                            initials={getInitials(user.name.split(" ")[0], user.name.split(" ")[1], user.email)}
                            avatarUrl={user.avatarUrl}
                            size="sm"
                            isRound={true}
                          />
                          <div className="min-w-0 flex-1">
                            <Text className="truncate text-sm">{user.name}</Text>
                            <Text className="truncate text-xs opacity-80">{user.email}</Text>
                            <Text className="truncate text-xs opacity-80">{user.title}</Text>
                          </div>
                        </button>
                      ))}
                    </div>
                  )}
                </div>
              </div>

              <div className="flex flex-col items-center justify-center gap-2">
                <Button
                  type="button"
                  variant="outline"
                  onPress={handleAddMembers}
                  isDisabled={selectedAvailableUsers.size === 0 || updateTeamMembersMutation.isPending}
                  className="w-10 p-0"
                  aria-label={t`Add selected users to team`}
                >
                  <ArrowRightIcon className="h-4 w-4" />
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onPress={handleRemoveMembers}
                  isDisabled={selectedTeamMembers.size === 0 || updateTeamMembersMutation.isPending}
                  className="w-10 p-0"
                  aria-label={t`Remove selected members from team`}
                >
                  <ArrowLeftIcon className="h-4 w-4" />
                </Button>
              </div>

              <div className="flex flex-col gap-2">
                <Heading level={4} className="font-medium text-sm">
                  <Trans>Team Members</Trans>{" "}
                  {debouncedSearchQuery && filteredTeamMembers.length !== teamMembers.length
                    ? `(${filteredTeamMembers.length} of ${teamMembers.length})`
                    : `(${teamMembers.length})`}
                </Heading>
                <div className="h-80 overflow-y-auto rounded-md border border-border bg-background">
                  {filteredTeamMembers.length === 0 ? (
                    <div className="flex h-full items-center justify-center p-4">
                      <Text className="text-muted-foreground text-sm">
                        {debouncedSearchQuery ? <Trans>No results found</Trans> : <Trans>No team members</Trans>}
                      </Text>
                    </div>
                  ) : (
                    <div className="space-y-1 p-2">
                      {filteredTeamMembers.map((member) => (
                        <div
                          key={member.userId}
                          className={`flex w-full items-center gap-3 rounded-md p-2 ${
                            selectedTeamMembers.has(member.userId) ? "bg-primary text-primary-foreground" : ""
                          }`}
                        >
                          <button
                            type="button"
                            onClick={() => toggleTeamMember(member.userId)}
                            disabled={!canRemove(member.userId)}
                            className="flex min-w-0 flex-1 items-center gap-3 text-left disabled:cursor-not-allowed"
                          >
                            <Avatar
                              initials={getInitials(member.name.split(" ")[0], member.name.split(" ")[1], member.email)}
                              avatarUrl={member.avatarUrl}
                              size="sm"
                              isRound={true}
                            />
                            <div className="min-w-0 flex-1">
                              <Text className="truncate text-sm">{member.name}</Text>
                              <Text className="truncate text-xs opacity-80">{member.email}</Text>
                              <Text className="truncate text-xs opacity-80">{member.title}</Text>
                            </div>
                          </button>
                          <TooltipTrigger>
                            <Select
                              aria-label={t`Role for ${member.name}`}
                              selectedKey={member.role}
                              onSelectionChange={(key) => handleRoleChange(member.userId, key as TeamMemberRole)}
                              isDisabled={!canChangeRole(member.userId) || updateTeamMembersMutation.isPending}
                              className={`w-28 ${changedRoles.has(member.userId) ? "ring-2 ring-success ring-offset-2" : ""}`}
                            >
                              <SelectItem id="Member">
                                <Trans>Member</Trans>
                              </SelectItem>
                              <SelectItem id="Admin">
                                <Trans>Admin</Trans>
                              </SelectItem>
                            </Select>
                            {!canChangeRole(member.userId) && !updateTeamMembersMutation.isPending && (
                              <Tooltip>
                                <Trans>Team Admins cannot change their own role</Trans>
                              </Tooltip>
                            )}
                          </TooltipTrigger>
                          <TooltipTrigger>
                            <button
                              type="button"
                              onClick={() => handleRemoveMemberClick(member)}
                              disabled={!canRemove(member.userId) || updateTeamMembersMutation.isPending}
                              className="rounded p-1 hover:bg-destructive/10 disabled:cursor-not-allowed disabled:opacity-50"
                              aria-label={t`Remove ${member.name} from team`}
                            >
                              <XIcon className="h-4 w-4 text-destructive" />
                            </button>
                            {!canRemove(member.userId) && !updateTeamMembersMutation.isPending && (
                              <Tooltip>
                                <Trans>Team Admins cannot remove themselves</Trans>
                              </Tooltip>
                            )}
                          </TooltipTrigger>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            </div>
          </DialogContent>
          {updateTeamMembersMutation.error && (
            <div className="px-4 py-3">
              <FormErrorMessage error={updateTeamMembersMutation.error} />
            </div>
          )}
          <DialogFooter>
            <Button
              type="reset"
              onPress={handleCancel}
              variant="secondary"
              isDisabled={updateTeamMembersMutation.isPending}
            >
              <Trans>Cancel</Trans>
            </Button>
            <Button type="submit" isDisabled={updateTeamMembersMutation.isPending}>
              {updateTeamMembersMutation.isPending ? <Trans>Saving...</Trans> : <Trans>Save Changes</Trans>}
            </Button>
          </DialogFooter>
        </Form>
      </Dialog>

      <Modal isOpen={!!memberToRemove} onOpenChange={() => setMemberToRemove(null)} blur={false} isDismissable={true}>
        <AlertDialog
          title={t`Remove member`}
          variant="destructive"
          actionLabel={t`Remove`}
          cancelLabel={t`Cancel`}
          onAction={handleRemoveMemberConfirm}
        >
          {memberToRemove && (
            <Trans>
              Are you sure you want to remove <b>{memberToRemove.name}</b> from this team?
            </Trans>
          )}
        </AlertDialog>
      </Modal>
    </Modal>
  );
}

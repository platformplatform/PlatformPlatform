import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@repo/ui/components/Dialog";
import { Field, FieldLabel } from "@repo/ui/components/Field";
import { InputGroup, InputGroupAddon, InputGroupButton, InputGroupInput } from "@repo/ui/components/InputGroup";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { ChevronDownIcon, ChevronLeftIcon, ChevronRightIcon, ChevronUpIcon, SearchIcon, XIcon } from "lucide-react";

import type { Schemas } from "@/shared/lib/api/client";

import { TeamMemberPickerColumn } from "./TeamMemberPickerColumn";
import { useTeamMemberPicker } from "./useTeamMemberPicker";

type Team = Schemas["TeamResponse"];

interface EditTeamMembersDialogProps {
  team: Team | null;
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
}

export function EditTeamMembersDialog({ team, isOpen, onOpenChange }: Readonly<EditTeamMembersDialogProps>) {
  const picker = useTeamMemberPicker({
    teamId: team?.id ?? null,
    isOpen,
    onClose: () => onOpenChange(false)
  });

  if (!team) {
    return null;
  }

  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Edit team members">
      <DialogContent className="sm:h-[40rem] sm:max-h-[calc(100dvh-4rem)] sm:w-dialog-2xl">
        <DialogHeader>
          <DialogTitle>
            <Trans>Edit team members - {team.name}</Trans>
          </DialogTitle>
          <DialogDescription>
            <Trans>Add or remove users to manage who belongs to this team.</Trans>
          </DialogDescription>
        </DialogHeader>

        <DialogBody className="flex flex-col gap-4">
          <Field>
            <FieldLabel className="sr-only">{t`Search users`}</FieldLabel>
            <InputGroup>
              <InputGroupAddon>
                <SearchIcon />
              </InputGroupAddon>
              <InputGroupInput
                type="text"
                role="searchbox"
                placeholder={t`Search by name, email, or title`}
                value={picker.search}
                onChange={(event) => picker.setSearch(event.target.value)}
                onKeyDown={(event) => event.key === "Escape" && picker.search && picker.setSearch("")}
              />
              {picker.search && (
                <InputGroupAddon align="inline-end">
                  <InputGroupButton onClick={() => picker.setSearch("")} size="icon-xs" aria-label={t`Clear search`}>
                    <XIcon />
                  </InputGroupButton>
                </InputGroupAddon>
              )}
            </InputGroup>
          </Field>

          {picker.isLoading ? (
            <div className="flex flex-1 gap-4">
              <Skeleton className="h-full w-full" />
              <Skeleton className="h-full w-full" />
            </div>
          ) : (
            <div className="flex min-h-0 flex-1 flex-col gap-3 sm:flex-row sm:items-stretch">
              <TeamMemberPickerColumn
                title={<Trans>Users</Trans>}
                count={picker.availableUsers.length}
                emptyTitle={<Trans>No users to add</Trans>}
                emptyDescription={
                  picker.search ? <Trans>Try a different search.</Trans> : <Trans>Everyone is already a member.</Trans>
                }
                users={picker.availableUsers}
                selectedIds={picker.selectedAvailable}
                onActivate={picker.onAvailableActivate}
                onDoubleActivate={picker.addUser}
              />

              <div className="flex flex-row items-center justify-center gap-2 sm:flex-col">
                <Button
                  variant="outline"
                  size="icon"
                  onClick={picker.addSelected}
                  disabled={picker.selectedAvailable.size === 0}
                  aria-label={t`Add selected users to team`}
                >
                  <ChevronRightIcon className="size-4 max-sm:hidden" />
                  <ChevronDownIcon className="size-4 sm:hidden" />
                </Button>
                <Button
                  variant="outline"
                  size="icon"
                  onClick={picker.removeSelected}
                  disabled={picker.selectedMembers.size === 0}
                  aria-label={t`Remove selected users from team`}
                >
                  <ChevronLeftIcon className="size-4 max-sm:hidden" />
                  <ChevronUpIcon className="size-4 sm:hidden" />
                </Button>
              </div>

              <TeamMemberPickerColumn
                title={<Trans>Team members</Trans>}
                count={picker.teamMembers.length}
                emptyTitle={<Trans>No team members</Trans>}
                emptyDescription={
                  picker.search ? (
                    <Trans>Try a different search.</Trans>
                  ) : (
                    <Trans>Add users from the left to get started.</Trans>
                  )
                }
                users={picker.teamMembers}
                selectedIds={picker.selectedMembers}
                onActivate={picker.onMemberActivate}
                onDoubleActivate={picker.removeUser}
                showAdminBadge={true}
              />
            </div>
          )}
        </DialogBody>

        <DialogFooter>
          <DialogClose render={<Button type="reset" variant="secondary" disabled={picker.isSaving} />}>
            <Trans>Cancel</Trans>
          </DialogClose>
          <Button onClick={picker.save} disabled={picker.isSaving || !picker.hasPendingChanges}>
            {picker.isSaving ? <Trans>Saving...</Trans> : <Trans>Save changes</Trans>}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

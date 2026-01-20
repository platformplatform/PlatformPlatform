import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { PlusIcon, Trash2Icon } from "lucide-react";
import { useState } from "react";
import type { components } from "@/shared/lib/api/client";
import { api, UserRole } from "@/shared/lib/api/client";
import { DeleteUserDialog } from "./DeleteUserDialog";
import InviteUserDialog from "./InviteUserDialog";
import { TenantNameRequiredDialog } from "./TenantNameRequiredDialog";
import { UserQuerying } from "./UserQuerying";

type UserDetails = components["schemas"]["UserDetails"];

interface UserToolbarProps {
  selectedUsers: UserDetails[];
  onSelectedUsersChange: (users: UserDetails[]) => void;
}

export function UserToolbar({ selectedUsers, onSelectedUsersChange }: Readonly<UserToolbarProps>) {
  const { data: currentUser } = api.useQuery("get", "/api/account-management/users/me");
  const { data: tenant } = api.useQuery("get", "/api/account-management/tenants/current");
  const [isInviteModalOpen, setIsInviteModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [showTenantNameRequiredDialog, setShowTenantNameRequiredDialog] = useState(false);
  const [isFilterBarExpanded, setIsFilterBarExpanded] = useState(false);
  const [shouldUseCompactButtons, setShouldUseCompactButtons] = useState(false);

  const isOwner = currentUser?.role === UserRole.Owner;
  const hasSelectedSelf = selectedUsers.some((user) => user.id === currentUser?.id);
  const hasTenantName = tenant?.name && tenant.name.trim() !== "";

  const handleFilterStateChange = (isExpanded: boolean, _hasFilters: boolean, useCompact: boolean) => {
    setIsFilterBarExpanded(isExpanded);
    setShouldUseCompactButtons(useCompact);
  };

  const handleInviteClick = () => {
    if (!hasTenantName) {
      setShowTenantNameRequiredDialog(true);
      return;
    }
    setIsInviteModalOpen(true);
  };

  const inviteButton = (
    <Button variant="default" onClick={handleInviteClick} aria-label={t`Invite user`}>
      <PlusIcon className="size-5" />
      <span className={shouldUseCompactButtons ? "hidden" : "hidden sm:inline"}>
        <Trans>Invite user</Trans>
      </span>
    </Button>
  );

  const deleteButton = (
    <Button
      variant="destructive"
      onClick={() => setIsDeleteModalOpen(true)}
      disabled={hasSelectedSelf}
      aria-label={t`Delete ${selectedUsers.length} users`}
    >
      <Trash2Icon className="size-5" />
      <span className={shouldUseCompactButtons ? "hidden" : "hidden sm:inline"}>
        <Trans>Delete {selectedUsers.length} users</Trans>
      </span>
    </Button>
  );

  return (
    <div className="mb-4 flex items-center justify-between gap-2">
      <UserQuerying onFilterStateChange={handleFilterStateChange} onFiltersUpdated={() => onSelectedUsersChange([])} />
      <div className={isFilterBarExpanded ? "mt-auto flex items-center gap-2" : "mt-8 flex items-center gap-2"}>
        {selectedUsers.length < 2 &&
          isOwner &&
          (shouldUseCompactButtons ? (
            <Tooltip>
              <TooltipTrigger render={inviteButton} />
              <TooltipContent>
                <Trans>Invite user</Trans>
              </TooltipContent>
            </Tooltip>
          ) : (
            inviteButton
          ))}
        {selectedUsers.length > 1 &&
          isOwner &&
          (shouldUseCompactButtons ? (
            <Tooltip>
              <TooltipTrigger render={deleteButton} />
              <TooltipContent>
                <Trans>Delete {selectedUsers.length} users</Trans>
              </TooltipContent>
            </Tooltip>
          ) : (
            deleteButton
          ))}
      </div>
      {isOwner && <InviteUserDialog isOpen={isInviteModalOpen} onOpenChange={setIsInviteModalOpen} />}
      <TenantNameRequiredDialog isOpen={showTenantNameRequiredDialog} onOpenChange={setShowTenantNameRequiredDialog} />
      <DeleteUserDialog
        users={selectedUsers}
        isOpen={isDeleteModalOpen}
        onOpenChange={setIsDeleteModalOpen}
        onUsersDeleted={() => onSelectedUsersChange([])}
      />
    </div>
  );
}

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
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
  const { data: currentUser } = api.useQuery("get", "/api/account/users/me");
  const { data: tenant } = api.useQuery("get", "/api/account/tenants/current");
  const [isInviteModalOpen, setIsInviteModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [showTenantNameRequiredDialog, setShowTenantNameRequiredDialog] = useState(false);

  const isOwner = currentUser?.role === UserRole.Owner;
  const hasSelectedSelf = selectedUsers.some((user) => user.id === currentUser?.id);
  const hasTenantName = tenant?.name && tenant.name.trim() !== "";

  const handleInviteClick = () => {
    if (!hasTenantName) {
      setShowTenantNameRequiredDialog(true);
      return;
    }
    setIsInviteModalOpen(true);
  };

  return (
    <div className="mb-4 flex items-center justify-between gap-2">
      <UserQuerying onFiltersUpdated={() => onSelectedUsersChange([])} />
      <div className="mt-auto flex items-center gap-2">
        {selectedUsers.length < 2 && isOwner && (
          <Button variant="default" onClick={handleInviteClick} aria-label={t`Invite user`}>
            <PlusIcon className="size-5" />
            <span className="hidden 2xl:inline">
              <Trans>Invite user</Trans>
            </span>
          </Button>
        )}
        {selectedUsers.length > 1 && isOwner && (
          <Button
            variant="destructive"
            onClick={() => setIsDeleteModalOpen(true)}
            disabled={hasSelectedSelf}
            aria-label={t`Delete ${selectedUsers.length} users`}
          >
            <Trash2Icon className="size-5" />
            <span className="hidden 2xl:inline">
              <Trans>Delete {selectedUsers.length} users</Trans>
            </span>
          </Button>
        )}
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

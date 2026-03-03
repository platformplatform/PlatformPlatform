import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useTenant, useUser } from "@repo/infrastructure/sync/hooks";
import { Button } from "@repo/ui/components/Button";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { PlusIcon, Trash2Icon } from "lucide-react";
import { useState } from "react";

import type { components } from "@/shared/lib/api/client";

import { UserRole } from "@/shared/lib/api/client";

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
  const { id: userId, tenantId } = import.meta.user_info_env;
  const { data: currentUser } = useUser(userId ?? "");
  const { data: tenant } = useTenant(tenantId ?? "");
  const [isInviteModalOpen, setIsInviteModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [showTenantNameRequiredDialog, setShowTenantNameRequiredDialog] = useState(false);
  const [areFiltersExpanded, setAreFiltersExpanded] = useState(false);

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

  // When filters are expanded, the filter bar needs much more space (~52rem),
  // so buttons stay icon-only regardless of container width to avoid overflow.
  const buttonExpandClass = areFiltersExpanded ? "" : "@[30rem]:w-fit @[30rem]:gap-1.5 @[30rem]:px-6";
  const textVisibilityClass = areFiltersExpanded ? "hidden" : "hidden @[30rem]:inline";

  return (
    <div className="@container mb-4 flex items-center justify-between gap-2">
      <UserQuerying
        onFiltersUpdated={() => onSelectedUsersChange([])}
        onFiltersExpandedChange={setAreFiltersExpanded}
      />
      <div className="mt-auto flex items-center gap-2">
        {selectedUsers.length < 2 && isOwner && (
          <Tooltip>
            <TooltipTrigger
              render={
                <Button
                  variant="default"
                  size="icon"
                  className={buttonExpandClass}
                  onClick={handleInviteClick}
                  aria-label={t`Invite user`}
                >
                  <PlusIcon className="size-5" />
                  <span className={textVisibilityClass}>
                    <Trans>Invite user</Trans>
                  </span>
                </Button>
              }
            />
            <TooltipContent>
              <Trans>Invite user</Trans>
            </TooltipContent>
          </Tooltip>
        )}
        {selectedUsers.length > 1 && isOwner && (
          <Tooltip>
            <TooltipTrigger
              render={
                <Button
                  variant="destructive"
                  size="icon"
                  className={buttonExpandClass}
                  onClick={() => setIsDeleteModalOpen(true)}
                  disabled={hasSelectedSelf}
                  aria-label={t`Delete ${selectedUsers.length} users`}
                >
                  <Trash2Icon className="size-5" />
                  <span className={textVisibilityClass}>
                    <Trans>Delete {selectedUsers.length} users</Trans>
                  </span>
                </Button>
              }
            />
            <TooltipContent>
              <Trans>Delete users</Trans>
            </TooltipContent>
          </Tooltip>
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

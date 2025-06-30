import type { components } from "@/shared/lib/api/client";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { PlusIcon, Trash2Icon } from "lucide-react";
import { useState } from "react";
import { DeleteUserDialog } from "./DeleteUserDialog";
import InviteUserDialog from "./InviteUserDialog";
import { UserQuerying } from "./UserQuerying";

type UserDetails = components["schemas"]["UserDetails"];

interface UserToolbarProps {
  selectedUsers: UserDetails[];
  onSelectedUsersChange: (users: UserDetails[]) => void;
}

export function UserToolbar({ selectedUsers, onSelectedUsersChange }: Readonly<UserToolbarProps>) {
  const [isInviteModalOpen, setIsInviteModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [isFilterBarExpanded, setIsFilterBarExpanded] = useState(false);
  const [hasActiveFilters, setHasActiveFilters] = useState(false);

  const handleFilterStateChange = (isExpanded: boolean, hasFilters: boolean) => {
    setIsFilterBarExpanded(isExpanded);
    setHasActiveFilters(hasFilters);
  };

  return (
    <div className="mt-4 mb-4 flex items-center justify-between gap-2">
      <UserQuerying onFilterStateChange={handleFilterStateChange} />
      <div className="mt-6 flex items-center gap-2">
        {selectedUsers.length < 2 && (
          <Button variant="primary" onPress={() => setIsInviteModalOpen(true)}>
            <PlusIcon className="h-5 w-5" />
            <span className={isFilterBarExpanded || hasActiveFilters ? "hidden" : "hidden sm:inline"}>
              <Trans>Invite user</Trans>
            </span>
          </Button>
        )}
        {selectedUsers.length > 1 && (
          <Button variant="destructive" onPress={() => setIsDeleteModalOpen(true)}>
            <Trash2Icon className="h-5 w-5" />
            <span className={isFilterBarExpanded || hasActiveFilters ? "hidden" : "hidden sm:inline"}>
              <Trans>Delete {selectedUsers.length} users</Trans>
            </span>
          </Button>
        )}
      </div>
      <InviteUserDialog isOpen={isInviteModalOpen} onOpenChange={setIsInviteModalOpen} />
      <DeleteUserDialog
        users={selectedUsers}
        isOpen={isDeleteModalOpen}
        onOpenChange={setIsDeleteModalOpen}
        onUsersDeleted={() => onSelectedUsersChange([])}
      />
    </div>
  );
}

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
}

export function UserToolbar({ selectedUsers }: Readonly<UserToolbarProps>) {
  const [isInviteModalOpen, setIsInviteModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);

  return (
    <div className="mt-4 mb-4 flex items-center justify-between gap-2">
      <UserQuerying />
      <div className="mt-6 flex items-center gap-2">
        {selectedUsers.length === 0 && (
          <Button variant="primary" onPress={() => setIsInviteModalOpen(true)}>
            <PlusIcon className="h-5 w-5" />
            <span className="hidden sm:inline">
              <Trans>Invite users</Trans>
            </span>
          </Button>
        )}
        {selectedUsers.length > 0 && (
          <Button variant="destructive" onPress={() => setIsDeleteModalOpen(true)}>
            <Trash2Icon className="h-5 w-5" />
            <span className="hidden sm:inline">
              <Trans>Delete {selectedUsers.length === 1 ? "user" : `${selectedUsers.length} users`}</Trans>
            </span>
          </Button>
        )}
      </div>
      <InviteUserDialog isOpen={isInviteModalOpen} onOpenChange={setIsInviteModalOpen} />
      <DeleteUserDialog users={selectedUsers} isOpen={isDeleteModalOpen} onOpenChange={setIsDeleteModalOpen} />
    </div>
  );
}

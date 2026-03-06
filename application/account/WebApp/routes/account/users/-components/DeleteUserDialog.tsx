import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { userCollection } from "@repo/infrastructure/sync/collections";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogMedia,
  AlertDialogTitle
} from "@repo/ui/components/AlertDialog";
import { Trash2Icon } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";

import { api } from "@/shared/lib/api/client";

interface UserData {
  id: string;
  firstName: string | null;
  lastName: string | null;
  email: string;
}

interface DeleteUserDialogProps {
  users: UserData[];
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  onUsersDeleted?: () => void;
}

export function DeleteUserDialog({ users, isOpen, onOpenChange, onUsersDeleted }: Readonly<DeleteUserDialogProps>) {
  const isSingleUser = users.length === 1;
  const user = users[0];

  const deleteUserMutation = api.useMutation("delete", "/api/account/users/{id}", {
    meta: { skipQueryInvalidation: true }
  });
  const bulkDeleteUsersMutation = api.useMutation("post", "/api/account/users/bulk-delete", {
    meta: { skipQueryInvalidation: true }
  });
  const userDisplayName = isSingleUser ? `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim() || user.email : "";

  const [isPending, setIsPending] = useState(false);

  const handleDelete = async () => {
    setIsPending(true);
    try {
      const now = new Date().toISOString();
      if (isSingleUser) {
        await deleteUserMutation.mutateAsync({ params: { path: { id: user.id } } });
        userCollection.update(user.id, (draft) => {
          draft.deletedAt = now;
        });
        toast.success(t`User deleted successfully: ${userDisplayName}`);
      } else {
        const userIds = users.map((user) => user.id);
        await bulkDeleteUsersMutation.mutateAsync({ body: { userIds: userIds } });
        for (const userId of userIds) {
          userCollection.update(userId, (draft) => {
            draft.deletedAt = now;
          });
        }
        toast.success(t`${users.length} users deleted successfully`);
      }

      onUsersDeleted?.();
      onOpenChange(false);
    } finally {
      setIsPending(false);
    }
  };

  return (
    <AlertDialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Delete user">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogMedia className="bg-destructive/10">
            <Trash2Icon className="text-destructive" />
          </AlertDialogMedia>
          <AlertDialogTitle>{isSingleUser ? t`Delete user` : t`Delete users`}</AlertDialogTitle>
          <AlertDialogDescription>
            {isSingleUser ? (
              <Trans>
                Are you sure you want to delete <b>{userDisplayName}</b>?
              </Trans>
            ) : (
              <Trans>
                Are you sure you want to delete <b>{users.length} users</b>?
              </Trans>
            )}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel variant="secondary" disabled={isPending}>
            <Trans>Cancel</Trans>
          </AlertDialogCancel>
          <AlertDialogAction variant="destructive" disabled={isPending} onClick={handleDelete}>
            <Trans>Delete</Trans>
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import {
  AlertDialog,
  AlertDialogClose,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle
} from "@repo/ui/components/AlertDialog";
import { Button } from "@repo/ui/components/Button";
import { useState } from "react";
import { toast } from "sonner";
import { api, type components } from "@/shared/lib/api/client";

type UserDetails = components["schemas"]["UserDetails"];

interface DeleteUserDialogProps {
  users: UserDetails[];
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  onUsersDeleted?: () => void;
}

export function DeleteUserDialog({ users, isOpen, onOpenChange, onUsersDeleted }: Readonly<DeleteUserDialogProps>) {
  const isSingleUser = users.length === 1;
  const user = users[0];

  const deleteUserMutation = api.useMutation("delete", "/api/account-management/users/{id}");
  const bulkDeleteUsersMutation = api.useMutation("post", "/api/account-management/users/bulk-delete");
  const userDisplayName = isSingleUser ? `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim() || user.email : "";

  const [isPending, setIsPending] = useState(false);

  const handleDelete = async () => {
    setIsPending(true);
    try {
      if (isSingleUser) {
        await deleteUserMutation.mutateAsync({ params: { path: { id: user.id } } });
        toast.success(t`Success`, {
          description: t`User deleted successfully: ${userDisplayName}`
        });
      } else {
        const userIds = users.map((user) => user.id);
        await bulkDeleteUsersMutation.mutateAsync({ body: { userIds: userIds } });
        toast.success(t`Success`, {
          description: t`${users.length} users deleted successfully`
        });
      }

      onUsersDeleted?.();
      onOpenChange(false);
    } finally {
      setIsPending(false);
    }
  };

  return (
    <AlertDialog open={isOpen} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
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
          <AlertDialogClose render={<Button variant="secondary" disabled={isPending} />}>
            <Trans>Cancel</Trans>
          </AlertDialogClose>
          <Button variant="destructive" disabled={isPending} onClick={handleDelete}>
            <Trans>Delete</Trans>
          </Button>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

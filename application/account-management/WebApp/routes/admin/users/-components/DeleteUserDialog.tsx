import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AlertDialog } from "@repo/ui/components/AlertDialog";
import { Modal } from "@repo/ui/components/Modal";
import { toastQueue } from "@repo/ui/components/Toast";
import { useCallback } from "react";
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

  const handleDelete = useCallback(async () => {
    if (isSingleUser) {
      deleteUserMutation.mutateAsync({ params: { path: { id: user.id } } }).then(() => {
        toastQueue.add({
          title: t`Success`,
          description: t`User deleted successfully: ${userDisplayName}`,
          variant: "success"
        });

        onUsersDeleted?.();
        onOpenChange(false);
      });
    } else {
      const userIds = users.map((user) => user.id);
      await bulkDeleteUsersMutation.mutateAsync({ body: { userIds: userIds } }).then(() => {
        toastQueue.add({
          title: t`Success`,
          description: t`${users.length} users deleted successfully`,
          variant: "success"
        });

        onUsersDeleted?.();
        onOpenChange(false);
      });
    }
  }, [
    isSingleUser,
    userDisplayName,
    bulkDeleteUsersMutation,
    deleteUserMutation,
    user,
    users,
    onUsersDeleted,
    onOpenChange
  ]);

  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} blur={false} isDismissable={true}>
      <AlertDialog
        title={isSingleUser ? t`Delete user` : t`Delete users`}
        variant="destructive"
        actionLabel={t`Delete`}
        cancelLabel={t`Cancel`}
        onAction={handleDelete}
      >
        {isSingleUser ? (
          <Trans>
            Are you sure you want to delete <b>{userDisplayName}</b>?
          </Trans>
        ) : (
          <Trans>
            Are you sure you want to delete <b>{users.length} users</b>?
          </Trans>
        )}
      </AlertDialog>
    </Modal>
  );
}

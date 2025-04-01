import { api, type components } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AlertDialog } from "@repo/ui/components/AlertDialog";
import { Modal } from "@repo/ui/components/Modal";
import { useCallback } from "react";

type UserDetails = components["schemas"]["UserDetails"];

interface DeleteUserDialogProps {
  users: UserDetails[];
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
}

export function DeleteUserDialog({ users, isOpen, onOpenChange }: Readonly<DeleteUserDialogProps>) {
  const isSingleUser = users.length === 1;
  const user = users[0];

  const bulkDeleteUsersMutation = api.useMutation("post", "/api/account-management/users/bulk-delete");
  const deleteUserMutation = api.useMutation("delete", "/api/account-management/users/{id}");

  const handleDelete = useCallback(async () => {
    if (isSingleUser) {
      await deleteUserMutation.mutateAsync({ params: { path: { id: user.id } } });
    } else {
      const userIds = users.map((user) => user.id);
      await bulkDeleteUsersMutation.mutateAsync({ body: { userIds: userIds } });
    }

    onOpenChange(false);
  }, [users, isSingleUser, user, deleteUserMutation, bulkDeleteUsersMutation, onOpenChange]);

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
            Are you sure you want to delete{" "}
            <b>{`${user.firstName ?? ""} ${user.lastName ?? ""}`.trim() || user.email}?</b>
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

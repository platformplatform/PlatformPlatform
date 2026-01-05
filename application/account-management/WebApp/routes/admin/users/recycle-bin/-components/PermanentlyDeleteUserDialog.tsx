import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AlertDialog } from "@repo/ui/components/AlertDialog";
import { Modal } from "@repo/ui/components/Modal";
import { Text } from "@repo/ui/components/Text";
import { toastQueue } from "@repo/ui/components/Toast";
import { useQueryClient } from "@tanstack/react-query";
import { useCallback } from "react";
import { api, type components } from "@/shared/lib/api/client";

type DeletedUserDetails = components["schemas"]["DeletedUserDetails"];

interface PermanentlyDeleteUserDialogProps {
  users: DeletedUserDetails[];
  isEmptyRecycleBin: boolean;
  totalDeletedUsersCount: number;
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  onUsersDeleted?: () => void;
}

export function PermanentlyDeleteUserDialog({
  users,
  isEmptyRecycleBin,
  totalDeletedUsersCount,
  isOpen,
  onOpenChange,
  onUsersDeleted
}: Readonly<PermanentlyDeleteUserDialogProps>) {
  const queryClient = useQueryClient();
  const purgeUserMutation = api.useMutation("delete", "/api/account-management/users/{id}/purge");
  const bulkPurgeUsersMutation = api.useMutation("post", "/api/account-management/users/deleted/bulk-purge");
  const emptyRecycleBinMutation = api.useMutation("post", "/api/account-management/users/deleted/empty-recycle-bin");

  const isSingleUser = users.length === 1;
  const user = users[0];
  const userDisplayName = isSingleUser ? `${user?.firstName ?? ""} ${user?.lastName ?? ""}`.trim() || user?.email : "";
  const isPending =
    purgeUserMutation.isPending || bulkPurgeUsersMutation.isPending || emptyRecycleBinMutation.isPending;

  const handleDelete = useCallback(async () => {
    if (isEmptyRecycleBin) {
      const deletedCount = await emptyRecycleBinMutation.mutateAsync({});
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account-management/users/deleted"] });
      toastQueue.add({
        title: t`Success`,
        description: deletedCount === 1 ? t`1 user permanently deleted` : t`${deletedCount} users permanently deleted`,
        variant: "success"
      });
      onUsersDeleted?.();
      onOpenChange(false);
      return;
    }

    if (users.length === 0) {
      return;
    }

    if (isSingleUser) {
      await purgeUserMutation.mutateAsync({ params: { path: { id: user.id } } });
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account-management/users/deleted"] });
      toastQueue.add({
        title: t`Success`,
        description: t`User permanently deleted: ${userDisplayName}`,
        variant: "success"
      });
      onUsersDeleted?.();
      onOpenChange(false);
    } else {
      await bulkPurgeUsersMutation.mutateAsync({ body: { userIds: users.map((u) => u.id) } });
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account-management/users/deleted"] });
      toastQueue.add({
        title: t`Success`,
        description: t`${users.length} users permanently deleted`,
        variant: "success"
      });
      onUsersDeleted?.();
      onOpenChange(false);
    }
  }, [
    isEmptyRecycleBin,
    emptyRecycleBinMutation,
    bulkPurgeUsersMutation,
    users,
    isSingleUser,
    user,
    userDisplayName,
    purgeUserMutation,
    queryClient,
    onUsersDeleted,
    onOpenChange
  ]);

  const getDialogTitle = () => {
    if (isEmptyRecycleBin) {
      return t`Empty recycle bin`;
    }
    if (isSingleUser) {
      return t`Permanently delete user`;
    }
    return t`Permanently delete users`;
  };

  const getDialogContent = () => {
    if (isEmptyRecycleBin) {
      return (
        <>
          {totalDeletedUsersCount === 1 ? (
            <Trans>
              This will permanently delete <b>1 user</b> in the recycle bin.
            </Trans>
          ) : (
            <Trans>
              This will permanently delete <b>{totalDeletedUsersCount} users</b> in the recycle bin.
            </Trans>
          )}
          <Text className="mt-2">
            <Trans>This action cannot be undone.</Trans>
          </Text>
        </>
      );
    }
    if (isSingleUser) {
      return (
        <>
          <Trans>
            Are you sure you want to permanently delete <b>{userDisplayName}</b>?
          </Trans>
          <Text className="mt-2">
            <Trans>This action cannot be undone.</Trans>
          </Text>
        </>
      );
    }
    return (
      <>
        <Trans>
          Are you sure you want to permanently delete <b>{users.length} users</b>?
        </Trans>
        <Text className="mt-2">
          <Trans>This action cannot be undone.</Trans>
        </Text>
      </>
    );
  };

  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} blur={false} isDismissable={!isPending}>
      <AlertDialog
        title={getDialogTitle()}
        variant="destructive"
        actionLabel={isEmptyRecycleBin ? t`Empty recycle bin` : t`Delete permanently`}
        cancelLabel={t`Cancel`}
        onAction={handleDelete}
      >
        {getDialogContent()}
      </AlertDialog>
    </Modal>
  );
}

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
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
import { useQueryClient } from "@tanstack/react-query";
import { AlertTriangleIcon } from "lucide-react";
import { useCallback } from "react";
import { toast } from "sonner";
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

  const handleDelete = useCallback(async () => {
    if (isEmptyRecycleBin) {
      const deletedCount = await emptyRecycleBinMutation.mutateAsync({});
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account-management/users/deleted"] });
      toast.success(deletedCount === 1 ? t`1 user permanently deleted` : t`${deletedCount} users permanently deleted`);
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
      toast.success(t`User permanently deleted: ${userDisplayName}`);
      onUsersDeleted?.();
      onOpenChange(false);
    } else {
      await bulkPurgeUsersMutation.mutateAsync({ body: { userIds: users.map((u) => u.id) } });
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account-management/users/deleted"] });
      toast.success(t`${users.length} users permanently deleted`);
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
          <p className="mt-2">
            <Trans>This action cannot be undone.</Trans>
          </p>
        </>
      );
    }
    if (isSingleUser) {
      return (
        <>
          <Trans>
            Are you sure you want to permanently delete <b>{userDisplayName}</b>?
          </Trans>
          <p className="mt-2">
            <Trans>This action cannot be undone.</Trans>
          </p>
        </>
      );
    }
    return (
      <>
        <Trans>
          Are you sure you want to permanently delete <b>{users.length} users</b>?
        </Trans>
        <p className="mt-2">
          <Trans>This action cannot be undone.</Trans>
        </p>
      </>
    );
  };

  return (
    <AlertDialog open={isOpen} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogMedia className="bg-destructive/10">
            <AlertTriangleIcon className="text-destructive" />
          </AlertDialogMedia>
          <AlertDialogTitle>{getDialogTitle()}</AlertDialogTitle>
          <AlertDialogDescription>{getDialogContent()}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel variant="secondary">
            <Trans>Cancel</Trans>
          </AlertDialogCancel>
          <AlertDialogAction variant="destructive" onClick={handleDelete}>
            {isEmptyRecycleBin ? <Trans>Empty recycle bin</Trans> : <Trans>Delete permanently</Trans>}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

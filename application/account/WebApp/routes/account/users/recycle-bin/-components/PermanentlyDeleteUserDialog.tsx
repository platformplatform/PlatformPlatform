import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { userCollection } from "@repo/infrastructure/sync/collections";
import { useDeletedUsers } from "@repo/infrastructure/sync/hooks";
import { useElectricMutation } from "@repo/infrastructure/sync/useElectricMutation";
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
import { AlertTriangleIcon } from "lucide-react";
import { toast } from "sonner";
import { apiClient } from "@/shared/lib/api/client";

interface DeletedUserData {
  id: string;
  firstName: string | null;
  lastName: string | null;
  email: string;
}

interface PermanentlyDeleteUserDialogProps {
  users: DeletedUserData[];
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
  const { data: allDeletedUsers } = useDeletedUsers();

  const isSingleUser = users.length === 1;
  const user = users[0];
  const userDisplayName = isSingleUser ? `${user?.firstName ?? ""} ${user?.lastName ?? ""}`.trim() || user?.email : "";

  interface PurgeVars {
    mode: "single" | "bulk" | "empty";
    userIds: string[];
  }

  const purgeMutation = useElectricMutation<number | undefined, PurgeVars>({
    mutationFn: async (vars) => {
      if (vars.mode === "empty") {
        const { data, error } = await apiClient.POST("/api/account/users/deleted/empty-recycle-bin", {});
        if (error) {
          throw error;
        }
        return data;
      }
      if (vars.mode === "single") {
        const { error } = await apiClient.DELETE("/api/account/users/{id}/purge", {
          params: { path: { id: vars.userIds[0] } }
        });
        if (error) {
          throw error;
        }
      } else {
        const { error } = await apiClient.POST("/api/account/users/deleted/bulk-purge", {
          body: { userIds: vars.userIds }
        });
        if (error) {
          throw error;
        }
      }
      return undefined;
    },
    utils: userCollection.utils,
    onMutate: (vars) => {
      for (const userId of vars.userIds) {
        userCollection.delete(userId);
      }
    },
    onSuccess: (data, vars) => {
      if (vars.mode === "empty") {
        const deletedCount = data ?? vars.userIds.length;
        toast.success(
          deletedCount === 1 ? t`1 user permanently deleted` : t`${deletedCount} users permanently deleted`
        );
      } else if (vars.mode === "single") {
        toast.success(t`User permanently deleted: ${userDisplayName}`);
      } else {
        toast.success(t`${vars.userIds.length} users permanently deleted`);
      }
      onUsersDeleted?.();
      onOpenChange(false);
    }
  });

  const handleDelete = () => {
    if (isEmptyRecycleBin) {
      const userIds = allDeletedUsers.map((u) => u.id);
      purgeMutation.mutate({ mode: "empty", userIds });
      return;
    }
    if (users.length === 0) {
      return;
    }
    const userIds = users.map((u) => u.id);
    purgeMutation.mutate({ mode: isSingleUser ? "single" : "bulk", userIds });
  };

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
    <AlertDialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Permanently delete user">
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

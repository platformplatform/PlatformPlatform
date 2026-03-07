import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { userCollection } from "@repo/infrastructure/sync/collections";
import { useDeletedUsers } from "@repo/infrastructure/sync/hooks";
import { useElectricMutation } from "@repo/infrastructure/sync/useElectricMutation";
import { Button } from "@repo/ui/components/Button";
import { RotateCcwIcon, Trash2Icon } from "lucide-react";
import { toast } from "sonner";
import { apiClient } from "@/shared/lib/api/client";

type ElectricDeletedUser = ReturnType<typeof useDeletedUsers>["data"][number];

interface DeletedUsersToolbarProps {
  selectedUsers: ElectricDeletedUser[];
  onSelectedUsersChange: (users: ElectricDeletedUser[]) => void;
  onPermanentlyDelete: (users: ElectricDeletedUser[]) => void;
  onEmptyRecycleBin: (totalCount: number) => void;
}

export function DeletedUsersToolbar({
  selectedUsers,
  onSelectedUsersChange,
  onPermanentlyDelete,
  onEmptyRecycleBin
}: Readonly<DeletedUsersToolbarProps>) {
  const { data: deletedUsers } = useDeletedUsers();

  const restoreMutation = useElectricMutation({
    mutationFn: async (vars: { userIds: string[] }) => {
      for (const userId of vars.userIds) {
        const { error } = await apiClient.POST("/api/account/users/{id}/restore", {
          params: { path: { id: userId } }
        });
        if (error) {
          throw error;
        }
      }
    },
    utils: userCollection.utils,
    onMutate: (vars) => {
      for (const userId of vars.userIds) {
        userCollection.update(userId, (draft) => {
          draft.deletedAt = null;
        });
      }
    },
    onSuccess: (_data, vars) => {
      if (vars.userIds.length === 1) {
        const user = selectedUsers[0];
        const userName = user.firstName || user.lastName ? `${user.firstName} ${user.lastName}`.trim() : user.email;
        toast.success(t`User restored successfully: ${userName}`);
      } else {
        toast.success(t`${vars.userIds.length} users restored successfully`);
      }
      onSelectedUsersChange([]);
    }
  });

  const isRestoring = restoreMutation.isPending;
  const hasDeletedUsers = deletedUsers.length > 0;
  const hasSelection = selectedUsers.length > 0;

  const handleRestore = () => {
    if (selectedUsers.length === 0) {
      return;
    }
    const userIds = selectedUsers.map((u) => u.id);
    restoreMutation.mutate({ userIds });
  };

  if (!hasDeletedUsers) {
    return null;
  }

  return (
    <div className="mb-4 flex items-center justify-end gap-2">
      {hasSelection ? (
        <>
          <Button
            variant="secondary"
            className="max-sm:grow"
            onClick={handleRestore}
            disabled={isRestoring}
            aria-label={selectedUsers.length === 1 ? t`Restore user` : t`Restore ${selectedUsers.length} users`}
          >
            <RotateCcwIcon />
            {selectedUsers.length === 1 ? (
              isRestoring ? (
                <Trans>Restoring...</Trans>
              ) : (
                <Trans>Restore</Trans>
              )
            ) : isRestoring ? (
              <Trans>Restoring...</Trans>
            ) : (
              <Trans>Restore {selectedUsers.length} users</Trans>
            )}
          </Button>
          <Button
            variant="destructive"
            className="max-sm:grow"
            onClick={() => onPermanentlyDelete(selectedUsers)}
            disabled={isRestoring}
            aria-label={
              selectedUsers.length === 1
                ? t`Permanently delete user`
                : t`Permanently delete ${selectedUsers.length} users`
            }
          >
            <Trash2Icon />
            {selectedUsers.length === 1 ? <Trans>Delete</Trans> : <Trans>Delete {selectedUsers.length} users</Trans>}
          </Button>
        </>
      ) : (
        <Button
          variant="destructive"
          className="max-sm:grow"
          onClick={() => onEmptyRecycleBin(deletedUsers.length)}
          aria-label={t`Empty recycle bin`}
        >
          <Trash2Icon />
          <Trans>Empty recycle bin</Trans>
        </Button>
      )}
    </div>
  );
}

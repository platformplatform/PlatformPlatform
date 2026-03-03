import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useDeletedUsers } from "@repo/infrastructure/sync/hooks";
import { Button } from "@repo/ui/components/Button";
import { useQueryClient } from "@tanstack/react-query";
import { RotateCcwIcon, Trash2Icon } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";
import { api } from "@/shared/lib/api/client";

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
  const queryClient = useQueryClient();
  const [isRestoring, setIsRestoring] = useState(false);
  const { data: deletedUsers } = useDeletedUsers();

  const restoreUserMutation = api.useMutation("post", "/api/account/users/{id}/restore", {
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/users/deleted"] });
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/users"] });
    }
  });

  const hasDeletedUsers = deletedUsers.length > 0;
  const hasSelection = selectedUsers.length > 0;

  const handleRestore = async () => {
    if (selectedUsers.length === 0) {
      return;
    }

    setIsRestoring(true);

    if (selectedUsers.length === 1) {
      const user = selectedUsers[0];
      const userName = user.firstName || user.lastName ? `${user.firstName} ${user.lastName}`.trim() : user.email;
      await restoreUserMutation.mutateAsync({ params: { path: { id: user.id } } });
      toast.success(t`User restored successfully: ${userName}`);
    } else {
      for (const user of selectedUsers) {
        await restoreUserMutation.mutateAsync({ params: { path: { id: user.id } } });
      }
      toast.success(t`${selectedUsers.length} users restored successfully`);
    }

    setIsRestoring(false);
    onSelectedUsersChange([]);
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

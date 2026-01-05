import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { toastQueue } from "@repo/ui/components/Toast";
import { useQueryClient } from "@tanstack/react-query";
import { RotateCcwIcon, Trash2Icon } from "lucide-react";
import { useState } from "react";
import { api, type components } from "@/shared/lib/api/client";

type DeletedUserDetails = components["schemas"]["DeletedUserDetails"];

interface DeletedUsersToolbarProps {
  selectedUsers: DeletedUserDetails[];
  onSelectedUsersChange: (users: DeletedUserDetails[]) => void;
  onPermanentlyDelete: (users: DeletedUserDetails[]) => void;
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

  const { data: deletedUsersData } = api.useQuery("get", "/api/account-management/users/deleted", {
    params: { query: { PageOffset: 0, PageSize: 25 } }
  });

  const restoreUserMutation = api.useMutation("post", "/api/account-management/users/{id}/restore", {
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account-management/users/deleted"] });
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account-management/users"] });
    }
  });

  const hasDeletedUsers = (deletedUsersData?.users?.length ?? 0) > 0;
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
      toastQueue.add({
        title: t`Success`,
        description: t`User restored successfully: ${userName}`,
        variant: "success"
      });
    } else {
      for (const user of selectedUsers) {
        await restoreUserMutation.mutateAsync({ params: { path: { id: user.id } } });
      }
      toastQueue.add({
        title: t`Success`,
        description: t`${selectedUsers.length} users restored successfully`,
        variant: "success"
      });
    }

    setIsRestoring(false);
    onSelectedUsersChange([]);
  };

  if (!hasDeletedUsers) {
    return null;
  }

  return (
    <div className="mb-4 flex items-center justify-end gap-2 bg-background/95 backdrop-blur-sm">
      {hasSelection ? (
        <>
          <Button
            variant="secondary"
            onPress={handleRestore}
            isDisabled={isRestoring}
            isPending={isRestoring}
            aria-label={selectedUsers.length === 1 ? t`Restore user` : t`Restore ${selectedUsers.length} users`}
          >
            <RotateCcwIcon className="h-5 w-5" />
            <span className="hidden sm:inline">
              {selectedUsers.length === 1 ? (
                <Trans>Restore</Trans>
              ) : (
                <Trans>Restore {selectedUsers.length} users</Trans>
              )}
            </span>
          </Button>
          <Button
            variant="destructive"
            onPress={() => onPermanentlyDelete(selectedUsers)}
            isDisabled={isRestoring}
            aria-label={
              selectedUsers.length === 1
                ? t`Permanently delete user`
                : t`Permanently delete ${selectedUsers.length} users`
            }
          >
            <Trash2Icon className="h-5 w-5" />
            <span className="hidden sm:inline">
              {selectedUsers.length === 1 ? <Trans>Delete</Trans> : <Trans>Delete {selectedUsers.length} users</Trans>}
            </span>
          </Button>
        </>
      ) : (
        <Button
          variant="destructive"
          onPress={() => onEmptyRecycleBin(deletedUsersData?.totalCount ?? 0)}
          aria-label={t`Empty recycle bin`}
        >
          <Trash2Icon className="h-5 w-5" />
          <span className="hidden sm:inline">
            <Trans>Empty recycle bin</Trans>
          </span>
        </Button>
      )}
    </div>
  );
}

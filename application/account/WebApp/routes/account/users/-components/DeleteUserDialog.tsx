import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { userCollection } from "@repo/infrastructure/sync/collections";
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
import { Trash2Icon } from "lucide-react";
import { toast } from "sonner";
import { apiClient } from "@/shared/lib/api/client";

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

  const userDisplayName = isSingleUser ? `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim() || user.email : "";

  const deleteUserMutation = useElectricMutation({
    mutationFn: async (vars: { userIds: string[] }) => {
      if (vars.userIds.length === 1) {
        const { error } = await apiClient.DELETE("/api/account/users/{id}", {
          params: { path: { id: vars.userIds[0] } }
        });
        if (error) {
          throw error;
        }
      } else {
        const { error } = await apiClient.POST("/api/account/users/bulk-delete", {
          body: { userIds: vars.userIds }
        });
        if (error) {
          throw error;
        }
      }
    },
    utils: userCollection.utils,
    onMutate: (vars) => {
      const now = new Date().toISOString();
      for (const userId of vars.userIds) {
        userCollection.update(userId, (draft) => {
          draft.deletedAt = now;
        });
      }
    },
    onSuccess: (_data, vars) => {
      if (vars.userIds.length === 1) {
        toast.success(t`User deleted successfully: ${userDisplayName}`);
      } else {
        toast.success(t`${vars.userIds.length} users deleted successfully`);
      }
      onUsersDeleted?.();
      onOpenChange(false);
    }
  });

  const handleDelete = () => {
    const userIds = users.map((u) => u.id);
    deleteUserMutation.mutate({ userIds });
  };

  const isPending = deleteUserMutation.isPending;

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

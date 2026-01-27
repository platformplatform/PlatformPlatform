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
import { Trash2Icon } from "lucide-react";
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
  const queryClient = useQueryClient();

  const deleteUserMutation = api.useMutation("delete", "/api/account-management/users/{id}", {
    meta: { skipQueryInvalidation: true }
  });
  const bulkDeleteUsersMutation = api.useMutation("post", "/api/account-management/users/bulk-delete", {
    meta: { skipQueryInvalidation: true }
  });
  const userDisplayName = isSingleUser ? `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim() || user.email : "";

  const [isPending, setIsPending] = useState(false);

  const handleDelete = async () => {
    setIsPending(true);
    try {
      if (isSingleUser) {
        await deleteUserMutation.mutateAsync({ params: { path: { id: user.id } } });
        toast.success(t`User deleted successfully: ${userDisplayName}`);
      } else {
        const userIds = users.map((user) => user.id);
        await bulkDeleteUsersMutation.mutateAsync({ body: { userIds: userIds } });
        toast.success(t`${users.length} users deleted successfully`);
      }

      // Invalidate user list queries (but not individual user queries to avoid 404)
      await queryClient.invalidateQueries({
        predicate: (query) => {
          const key = query.queryKey;
          return (
            Array.isArray(key) &&
            key[0] === "get" &&
            (key[1] === "/api/account-management/users" || key[1] === "/api/account-management/users/deleted")
          );
        }
      });

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

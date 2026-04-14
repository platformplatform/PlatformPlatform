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
import { Trash2Icon } from "lucide-react";
import { toast } from "sonner";

import { api, queryClient, type Schemas } from "@/shared/lib/api/client";

type Team = Schemas["TeamResponse"];

interface DeleteTeamConfirmProps {
  team: Team | null;
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  onDeleted?: () => void;
}

export function DeleteTeamConfirm({ team, isOpen, onOpenChange, onDeleted }: Readonly<DeleteTeamConfirmProps>) {
  const deleteTeamMutation = api.useMutation("delete", "/api/account/teams/{id}", {
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/teams"] });
      toast.success(t`Team deleted`);
      onDeleted?.();
      onOpenChange(false);
    }
  });

  const handleDelete = () => {
    if (!team) {
      return;
    }
    deleteTeamMutation.mutate({ params: { path: { id: team.id } } });
  };

  return (
    <AlertDialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Delete team">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogMedia className="bg-destructive/10">
            <Trash2Icon className="text-destructive" />
          </AlertDialogMedia>
          <AlertDialogTitle>
            <Trans>Delete team</Trans>
          </AlertDialogTitle>
          <AlertDialogDescription>
            {team && (
              <Trans>
                Are you sure you want to delete <b>{team.name}</b>? This cannot be undone.
              </Trans>
            )}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel variant="secondary" disabled={deleteTeamMutation.isPending}>
            <Trans>Cancel</Trans>
          </AlertDialogCancel>
          <AlertDialogAction variant="destructive" disabled={deleteTeamMutation.isPending} onClick={handleDelete}>
            <Trans>Delete</Trans>
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

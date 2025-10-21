import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AlertDialog } from "@repo/ui/components/AlertDialog";
import { Modal } from "@repo/ui/components/Modal";
import { toastQueue } from "@repo/ui/components/Toast";
import { useQueryClient } from "@tanstack/react-query";
import { useCallback, useEffect } from "react";
import { api, type components } from "@/shared/lib/api/client";

type TeamSummary = components["schemas"]["TeamSummary"];

interface DeleteTeamDialogProps {
  team: TeamSummary | null;
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  onTeamDeleted: () => void;
}

export function DeleteTeamDialog({ team, isOpen, onOpenChange, onTeamDeleted }: Readonly<DeleteTeamDialogProps>) {
  const queryClient = useQueryClient();

  const deleteTeamMutation = api.useMutation("delete", "/api/account-management/teams/{id}", {
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["/api/account-management/teams"] });

      toastQueue.add({
        title: t`Success`,
        description: t`Team deleted successfully`,
        variant: "success"
      });

      onOpenChange(false);
      onTeamDeleted();
    }
  });

  useEffect(() => {
    if (!isOpen) {
      deleteTeamMutation.reset();
    }
  }, [isOpen, deleteTeamMutation]);

  const handleDelete = useCallback(async () => {
    if (!team || deleteTeamMutation.isPending) {
      return;
    }

    deleteTeamMutation.mutate({
      params: {
        path: {
          id: team.id
        }
      }
    });
  }, [team, deleteTeamMutation]);

  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} blur={false} isDismissable={true}>
      <AlertDialog
        title={t`Delete team`}
        variant="destructive"
        actionLabel={deleteTeamMutation.isPending ? t`Deleting...` : t`Delete`}
        cancelLabel={t`Cancel`}
        onAction={handleDelete}
      >
        {team && (
          <Trans>
            Are you sure you want to delete <b>{team.name}</b>?
          </Trans>
        )}
      </AlertDialog>
    </Modal>
  );
}

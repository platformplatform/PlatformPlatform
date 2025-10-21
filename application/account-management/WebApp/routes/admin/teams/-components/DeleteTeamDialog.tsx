import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AlertDialog } from "@repo/ui/components/AlertDialog";
import { Modal } from "@repo/ui/components/Modal";
import { toastQueue } from "@repo/ui/components/Toast";
import { useCallback } from "react";
import type { components } from "@/shared/lib/api/client";

type TeamSummary = components["schemas"]["TeamSummary"];

interface DeleteTeamDialogProps {
  team: TeamSummary | null;
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  onTeamDeleted?: () => void;
}

export function DeleteTeamDialog({ team, isOpen, onOpenChange, onTeamDeleted }: Readonly<DeleteTeamDialogProps>) {
  const handleDelete = useCallback(async () => {
    if (!team) {
      return;
    }

    setTimeout(() => {
      toastQueue.add({
        title: t`Success`,
        description: t`Team deleted successfully`,
        variant: "success"
      });

      if (onTeamDeleted) {
        onTeamDeleted();
      }
      onOpenChange(false);
    }, 300);
  }, [team, onTeamDeleted, onOpenChange]);

  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} blur={false} isDismissable={true}>
      <AlertDialog
        title={t`Delete team`}
        variant="destructive"
        actionLabel={t`Delete`}
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

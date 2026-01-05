import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { UnsavedChangesAlertDialog } from "@repo/ui/components/UnsavedChangesAlertDialog";

type UnsavedChangesDialogProps = {
  isOpen: boolean;
  onConfirmLeave: () => void;
  onCancel: () => void;
};

/**
 * UnsavedChangesDialog with translations for account-management.
 * Use this for pages (non-modal contexts) with useUnsavedChangesGuard hook.
 */
export function UnsavedChangesDialog({ isOpen, onConfirmLeave, onCancel }: Readonly<UnsavedChangesDialogProps>) {
  return (
    <UnsavedChangesAlertDialog
      isOpen={isOpen}
      onConfirmLeave={onConfirmLeave}
      onCancel={onCancel}
      title={t`Unsaved changes`}
      actionLabel={t`Leave`}
      cancelLabel={t`Stay`}
    >
      <Trans>You have unsaved changes. If you leave now, your changes will be lost.</Trans>
    </UnsavedChangesAlertDialog>
  );
}

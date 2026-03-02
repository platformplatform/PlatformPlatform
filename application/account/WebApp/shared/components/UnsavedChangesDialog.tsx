import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { UnsavedChangesAlertDialog } from "@repo/ui/components/UnsavedChangesAlertDialog";

type UnsavedChangesDialogProps = {
  isOpen: boolean;
  onConfirmLeave: () => void;
  onCancel: () => void;
  parentTrackingTitle: string;
};

/**
 * UnsavedChangesDialog with translations for account.
 * Use this for pages (non-modal contexts) with useUnsavedChangesGuard hook.
 */
export function UnsavedChangesDialog({
  isOpen,
  onConfirmLeave,
  onCancel,
  parentTrackingTitle
}: Readonly<UnsavedChangesDialogProps>) {
  return (
    <UnsavedChangesAlertDialog
      isOpen={isOpen}
      onConfirmLeave={onConfirmLeave}
      onCancel={onCancel}
      title={t`Unsaved changes`}
      actionLabel={t`Leave`}
      cancelLabel={t`Stay`}
      parentTrackingTitle={parentTrackingTitle}
    >
      <Trans>You have unsaved changes. If you leave now, your changes will be lost.</Trans>
    </UnsavedChangesAlertDialog>
  );
}

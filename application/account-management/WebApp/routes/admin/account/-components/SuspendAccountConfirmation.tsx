import { TenantState, api } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AlertDialog } from "@repo/ui/components/AlertDialog";
import { Modal } from "@repo/ui/components/Modal";
import { useToast } from "@repo/ui/hooks/useToast";
import { useState } from "react";

type SuspendAccountConfirmationProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  onSuspendComplete?: () => void;
};

export default function SuspendAccountConfirmation({
  isOpen,
  onOpenChange,
  onSuspendComplete
}: Readonly<SuspendAccountConfirmationProps>) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const changeTenantStateMutation = api.useMutation("put", "/api/account-management/tenants/current/state");
  const { toast } = useToast();
  const handleSuspend = async () => {
    setIsSubmitting(true);
    await changeTenantStateMutation.mutateAsync({ body: { newState: TenantState.Suspended } });

    toast({
      title: t`Account suspended`,
      description: t`Your account has been suspended successfully.`,
      variant: "success"
    });

    onSuspendComplete?.();
    onOpenChange(false);
    setIsSubmitting(false);
    window.location.reload();
  };
  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} isDismissable={!isSubmitting}>
      <AlertDialog
        variant="destructive"
        actionLabel={isSubmitting ? t`Suspending...` : t`Suspend account`}
        title={t`Suspend account`}
        onAction={handleSuspend}
      >
        <Trans>
          You are about to suspend your account. This will have the following consequences:
          <br />
          <br />• All product features will be disabled
          <br />• Only tenant owners will be able to log in
          <br />• Account suspension is required before deletion
          <br />
          <br />
          You can reactivate your account at any time.
        </Trans>
      </AlertDialog>
    </Modal>
  );
}

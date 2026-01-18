import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Dialog, DialogBody, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@repo/ui/components/Dialog";
import type { components } from "@/shared/lib/api/api.generated";
import { api } from "@/shared/lib/api/client";

type TenantInfo = components["schemas"]["TenantInfo"];

interface AcceptInvitationDialogProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  tenant: TenantInfo | null;
  onAccept: () => void;
  isLoading?: boolean;
}

export function AcceptInvitationDialog({
  isOpen,
  onOpenChange,
  tenant,
  onAccept,
  isLoading = false
}: Readonly<AcceptInvitationDialogProps>) {
  const declineInvitationMutation = api.useMutation("post", "/api/account-management/users/decline-invitation", {
    onSuccess: () => {
      onOpenChange(false);
      window.location.reload();
    },
    onError: (error) => {
      // Close dialog on error (e.g., when invitation is already revoked)
      onOpenChange(false);
      // Re-throw to trigger the global error handler which will show the toast
      // Use setTimeout to ensure it's an unhandled rejection
      setTimeout(() => {
        Promise.reject(error);
      }, 0);
    }
  });

  const handleDeclineInvitation = () => {
    if (tenant) {
      declineInvitationMutation.mutate({ body: { tenantId: tenant.tenantId } });
    }
  };

  if (!tenant) {
    return null;
  }

  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent className="sm:w-dialog-md">
        <DialogHeader>
          <DialogTitle>
            <Trans>Accept invitation</Trans>
          </DialogTitle>
        </DialogHeader>
        <DialogBody>
          <p>
            <Trans>
              You have been invited to join <strong>{tenant.tenantName}</strong>.
            </Trans>
          </p>
          <p className="text-muted-foreground text-sm">
            <Trans>
              When you accept this invitation, your profile information (name, title, and avatar) from your current
              account will be copied to the new account.
            </Trans>
          </p>
        </DialogBody>
        <DialogFooter>
          <Button
            variant="destructive"
            onClick={handleDeclineInvitation}
            disabled={isLoading || declineInvitationMutation.isPending}
          >
            {declineInvitationMutation.isPending ? <Trans>Declining...</Trans> : <Trans>Decline</Trans>}
          </Button>
          <Button variant="default" onClick={onAccept} disabled={isLoading || declineInvitationMutation.isPending}>
            {isLoading ? <Trans>Accepting...</Trans> : <Trans>Accept invitation</Trans>}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

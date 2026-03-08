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
import { LogOutIcon } from "lucide-react";

export function RevokeSessionDialog({
  isOpen,
  onOpenChange,
  onRevoke
}: Readonly<{
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  onRevoke: () => void;
}>) {
  return (
    <AlertDialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Revoke session">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogMedia className="bg-destructive/10">
            <LogOutIcon className="text-destructive" />
          </AlertDialogMedia>
          <AlertDialogTitle>{t`Revoke session`}</AlertDialogTitle>
          <AlertDialogDescription>
            <Trans>
              Are you sure you want to revoke this session? The device will be logged out and will need to log in again.
            </Trans>
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel variant="secondary">
            <Trans>Cancel</Trans>
          </AlertDialogCancel>
          <AlertDialogAction variant="destructive" onClick={onRevoke}>
            <Trans>Revoke</Trans>
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

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
import { RotateCcwIcon } from "lucide-react";

interface ReopenConfirmDialogProps {
  isOpen: boolean;
  onCancel: () => void;
  onConfirm: () => void;
}

// Confirms a public staff reply on a terminal ticket. Sending reopens the ticket and emails the
// user, so staff must opt in rather than reopen a closed thread by accident.
export function ReopenConfirmDialog({ isOpen, onCancel, onConfirm }: Readonly<ReopenConfirmDialogProps>) {
  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && onCancel()} trackingTitle="Reopen ticket on reply">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogMedia className="bg-amber-100">
            <RotateCcwIcon className="text-amber-600" />
          </AlertDialogMedia>
          <AlertDialogTitle>
            <Trans>Reopen this ticket?</Trans>
          </AlertDialogTitle>
          <AlertDialogDescription>
            <Trans>
              Sending this reply reopens the ticket and emails the user. Use an internal note instead if you don't want
              to notify them.
            </Trans>
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel variant="secondary">
            <Trans>Cancel</Trans>
          </AlertDialogCancel>
          <AlertDialogAction onClick={onConfirm}>
            <Trans>Reopen and send</Trans>
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

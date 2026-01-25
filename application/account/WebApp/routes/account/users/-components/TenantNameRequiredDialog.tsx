import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Alert, AlertDescription } from "@repo/ui/components/Alert";
import { Button } from "@repo/ui/components/Button";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@repo/ui/components/Dialog";
import { Link } from "@repo/ui/components/Link";
import { AlertCircleIcon } from "lucide-react";

interface TenantNameRequiredDialogProps {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
}

export function TenantNameRequiredDialog({ isOpen, onOpenChange }: Readonly<TenantNameRequiredDialogProps>) {
  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent className="sm:w-dialog-md">
        <DialogHeader>
          <DialogTitle>
            <Trans>Add your account name</Trans>
          </DialogTitle>
          <DialogDescription>{t`Help your team recognize your invites`}</DialogDescription>
        </DialogHeader>

        <DialogBody>
          <Alert variant="warning">
            <AlertCircleIcon />
            <AlertDescription>
              <Trans>Your team needs to know who's inviting them. Add an account name to get started.</Trans>
            </AlertDescription>
          </Alert>
        </DialogBody>

        <DialogFooter>
          <DialogClose render={<Button variant="secondary" />}>
            <Trans>Cancel</Trans>
          </DialogClose>
          <Link href="/account/settings" variant="button-primary">
            <Trans>Go to account settings</Trans>
          </Link>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

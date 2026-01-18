import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
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
import { Link } from "@tanstack/react-router";
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
          <div className="flex items-center gap-3 rounded-lg border border-border bg-muted/50 p-4">
            <AlertCircleIcon className="h-5 w-5 text-warning" />
            <p className="text-sm">
              <Trans>Your team needs to know who's inviting them. Add an account name to get started.</Trans>
            </p>
          </div>
        </DialogBody>

        <DialogFooter>
          <DialogClose render={<Button variant="secondary" />}>
            <Trans>Cancel</Trans>
          </DialogClose>
          <DialogClose
            render={
              <Link to="/admin/account" className="max-sm:w-full">
                <Button variant="default" className="max-sm:w-full">
                  <Trans>Go to account settings</Trans>
                </Button>
              </Link>
            }
          />
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

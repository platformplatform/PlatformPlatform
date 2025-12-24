import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Dialog } from "@repo/ui/components/Dialog";
import { DialogContent, DialogFooter, DialogHeader } from "@repo/ui/components/DialogFooter";
import { Heading } from "@repo/ui/components/Heading";
import { Modal } from "@repo/ui/components/Modal";
import { Link } from "@tanstack/react-router";
import { AlertCircleIcon, XIcon } from "lucide-react";

interface TenantNameRequiredDialogProps {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
}

export function TenantNameRequiredDialog({ isOpen, onOpenChange }: Readonly<TenantNameRequiredDialogProps>) {
  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} isDismissable={true}>
      <Dialog className="sm:w-dialog-md">
        {({ close }) => (
          <>
            <XIcon onClick={close} className="absolute top-2 right-2 h-10 w-10 cursor-pointer p-2 hover:bg-muted" />
            <DialogHeader description={t`Help your team recognize your invites`}>
              <Heading slot="title" className="text-2xl">
                <Trans>Add your account name</Trans>
              </Heading>
            </DialogHeader>

            <DialogContent className="flex flex-col gap-4">
              <div className="flex items-center gap-3 rounded-lg border border-border bg-muted/50 p-4">
                <AlertCircleIcon className="h-5 w-5 text-warning" />
                <p className="text-sm">
                  <Trans>Your team needs to know who's inviting them. Add an account name to get started.</Trans>
                </p>
              </div>
            </DialogContent>

            <DialogFooter>
              <Button variant="secondary" onClick={close}>
                <Trans>Cancel</Trans>
              </Button>
              <Link to="/admin/account">
                <Button variant="default" onClick={close}>
                  <Trans>Go to account settings</Trans>
                </Button>
              </Link>
            </DialogFooter>
          </>
        )}
      </Dialog>
    </Modal>
  );
}

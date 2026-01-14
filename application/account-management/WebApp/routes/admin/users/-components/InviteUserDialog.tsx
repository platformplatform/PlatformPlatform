import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Dialog } from "@repo/ui/components/Dialog";
import { DialogContent, DialogFooter, DialogHeader } from "@repo/ui/components/DialogFooter";
import { Form } from "@repo/ui/components/Form";
import { Heading } from "@repo/ui/components/Heading";
import { TextField } from "@repo/ui/components/TextField";
import { toastQueue } from "@repo/ui/components/Toast";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { XIcon } from "lucide-react";
import { useState } from "react";
import { DirtyModal } from "@/shared/components/DirtyModal";
import { api } from "@/shared/lib/api/client";

interface InviteUserDialogProps {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
}

export default function InviteUserDialog({ isOpen, onOpenChange }: Readonly<InviteUserDialogProps>) {
  const [isFormDirty, setIsFormDirty] = useState(false);
  const inviteUserMutation = api.useMutation("post", "/api/account-management/users/invite", {
    onSuccess: () => {
      setIsFormDirty(false);
      toastQueue.add({
        title: t`Success`,
        description: t`User invited successfully`,
        variant: "success"
      });
      onOpenChange(false);
    }
  });

  const handleCloseComplete = () => {
    setIsFormDirty(false);
  };

  const handleCancel = () => {
    setIsFormDirty(false);
    onOpenChange(false);
  };

  return (
    <DirtyModal
      isOpen={isOpen}
      onOpenChange={onOpenChange}
      hasUnsavedChanges={isFormDirty}
      isDismissable={!inviteUserMutation.isPending}
      onCloseComplete={handleCloseComplete}
    >
      <Dialog className="sm:w-dialog-md">
        {({ close }) => (
          <>
            <XIcon onClick={close} className="absolute top-2 right-2 h-10 w-10 cursor-pointer p-2 hover:bg-muted" />
            <DialogHeader description={<Trans>An email with login instructions will be sent to the user.</Trans>}>
              <Heading slot="title" className="text-2xl">
                <Trans>Invite user</Trans>
              </Heading>
            </DialogHeader>

            <Form
              onSubmit={mutationSubmitter(inviteUserMutation)}
              validationErrors={inviteUserMutation.error?.errors}
              validationBehavior="aria"
              className="flex flex-col max-sm:h-full"
            >
              <DialogContent className="flex flex-col gap-4">
                <TextField
                  autoFocus={true}
                  isRequired={true}
                  name="email"
                  label={t`Email`}
                  placeholder={t`user@email.com`}
                  className="flex-grow"
                  onChange={() => setIsFormDirty(true)}
                />
              </DialogContent>
              <DialogFooter>
                <Button
                  type="reset"
                  onPress={handleCancel}
                  variant="secondary"
                  isDisabled={inviteUserMutation.isPending}
                >
                  <Trans>Cancel</Trans>
                </Button>
                <Button type="submit" isDisabled={inviteUserMutation.isPending}>
                  {inviteUserMutation.isPending ? <Trans>Sending...</Trans> : <Trans>Send invite</Trans>}
                </Button>
              </DialogFooter>
            </Form>
          </>
        )}
      </Dialog>
    </DirtyModal>
  );
}

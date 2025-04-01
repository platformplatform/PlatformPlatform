import { api } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Dialog } from "@repo/ui/components/Dialog";
import { Form } from "@repo/ui/components/Form";
import { FormErrorMessage } from "@repo/ui/components/FormErrorMessage";
import { Heading } from "@repo/ui/components/Heading";
import { Modal } from "@repo/ui/components/Modal";
import { TextField } from "@repo/ui/components/TextField";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { XIcon } from "lucide-react";
import { useEffect } from "react";

interface InviteUserDialogProps {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
}

export default function InviteUserDialog({ isOpen, onOpenChange }: Readonly<InviteUserDialogProps>) {
  const inviteUserMutation = api.useMutation("post", "/api/account-management/users/invite");

  useEffect(() => {
    if (inviteUserMutation.isSuccess) {
      onOpenChange(false);
    }
  }, [inviteUserMutation.isSuccess, onOpenChange]);

  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} isDismissable={true}>
      <Dialog>
        <XIcon onClick={() => onOpenChange(false)} className="absolute top-2 right-2 h-10 w-10 p-2 hover:bg-muted" />
        <Heading slot="title" className="text-2xl">
          <Trans>Invite user</Trans>
        </Heading>
        <p className="text-muted-foreground text-sm">
          <Trans>Invite users and assign them roles. They will appear once they log in.</Trans>
        </p>

        <Form
          onSubmit={mutationSubmitter(inviteUserMutation)}
          validationErrors={inviteUserMutation.error?.errors}
          validationBehavior="aria"
          className="mt-4 flex flex-col gap-4"
        >
          <TextField
            autoFocus={true}
            isRequired={true}
            name="email"
            label={t`Email`}
            placeholder={t`user@email.com`}
            className="flex-grow"
          />
          <FormErrorMessage error={inviteUserMutation.error} />
          <div className="mt-6 flex justify-end gap-4">
            <Button type="reset" onPress={() => onOpenChange(false)} variant="secondary">
              <Trans>Cancel</Trans>
            </Button>
            <Button type="submit" isDisabled={inviteUserMutation.isPending}>
              <Trans>Send invite</Trans>
            </Button>
          </div>
        </Form>
      </Dialog>
    </Modal>
  );
}

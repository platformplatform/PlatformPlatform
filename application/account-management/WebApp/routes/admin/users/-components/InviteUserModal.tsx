import { useCallback, useEffect } from "react";
import { useFormState } from "react-dom";
import { Button } from "@repo/ui/components/Button";
import { TextField } from "@repo/ui/components/TextField";
import { Heading } from "@repo/ui/components/Heading";
import { Form } from "@repo/ui/components/Form";
import { XIcon } from "lucide-react";
import { Dialog } from "@repo/ui/components/Dialog";
import { FormErrorMessage } from "@repo/ui/components/FormErrorMessage";
import { Modal } from "@repo/ui/components/Modal";
import { api } from "@/shared/lib/api/client";

type InviteUserModalProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
};

export default function InviteUserModal({ isOpen, onOpenChange }: Readonly<InviteUserModalProps>) {
  const closeDialog = useCallback(() => {
    onOpenChange(false);
  }, [onOpenChange]);

  let [{ success, errors, title, message }, action, isPending] = useFormState(
    api.actionPost("/api/account-management/users/invite"),
    { success: null }
  );

  useEffect(() => {
    if (isPending) {
      success = undefined;
    }

    if (success) {
      closeDialog();
      window.location.reload();
    }
  }, [success, isPending, closeDialog]);

  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} isDismissable={true}>
      <Dialog>
        <XIcon onClick={closeDialog} className="h-10 w-10 absolute top-2 right-2 p-2 hover:bg-muted" />
        <Heading slot="title" className="text-2xl">
          Invite User
        </Heading>
        <p className="text-muted-foreground text-sm">
          Invite users and assign them roles. They will appear once they've signed in.
        </p>

        <Form action={action} validationErrors={errors} validationBehavior="aria" className="flex flex-col gap-4 mt-4">
          <TextField
            autoFocus
            isRequired
            name="email"
            label="Email"
            placeholder="user@email.com"
            className="flex-grow"
          />
          <FormErrorMessage title={title} message={message} />

          <div className="flex justify-end gap-4 mt-6">
            <Button type="reset" onPress={closeDialog} variant="secondary">
              Cancel
            </Button>
            <Button type="submit" isDisabled={isPending}>
              Send invite
            </Button>
          </div>
        </Form>
      </Dialog>
    </Modal>
  );
}

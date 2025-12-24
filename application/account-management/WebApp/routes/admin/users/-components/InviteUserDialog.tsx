import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@repo/ui/components/Dialog";
import { Form } from "@repo/ui/components/Form";
import { TextField } from "@repo/ui/components/TextField";
import { toastQueue } from "@repo/ui/components/Toast";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { api } from "@/shared/lib/api/client";

interface InviteUserDialogProps {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
}

export default function InviteUserDialog({ isOpen, onOpenChange }: Readonly<InviteUserDialogProps>) {
  const inviteUserMutation = api.useMutation("post", "/api/account-management/users/invite", {
    onSuccess: () => {
      toastQueue.add({
        title: t`Success`,
        description: t`User invited successfully`,
        variant: "success"
      });
      onOpenChange(false);
    }
  });

  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent className="sm:w-dialog-md">
        <DialogHeader>
          <DialogTitle>
            <Trans>Invite user</Trans>
          </DialogTitle>
          <DialogDescription>
            <Trans>An email with login instructions will be sent to the user.</Trans>
          </DialogDescription>
        </DialogHeader>

        <Form
          onSubmit={mutationSubmitter(inviteUserMutation)}
          validationErrors={inviteUserMutation.error?.errors}
          validationBehavior="aria"
          className="flex flex-col max-sm:h-full"
        >
          <div className="flex flex-col gap-4">
            <TextField
              autoFocus={true}
              isRequired={true}
              name="email"
              label={t`Email`}
              placeholder={t`user@email.com`}
              className="flex-grow"
            />
          </div>
          <DialogFooter>
            <DialogClose render={<Button type="reset" variant="secondary" disabled={inviteUserMutation.isPending} />}>
              <Trans>Cancel</Trans>
            </DialogClose>
            <Button type="submit" disabled={inviteUserMutation.isPending}>
              <Trans>Send invite</Trans>
            </Button>
          </DialogFooter>
        </Form>
      </DialogContent>
    </Dialog>
  );
}

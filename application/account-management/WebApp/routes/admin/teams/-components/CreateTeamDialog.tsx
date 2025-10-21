import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Dialog } from "@repo/ui/components/Dialog";
import { DialogContent, DialogFooter, DialogHeader } from "@repo/ui/components/DialogFooter";
import { Form } from "@repo/ui/components/Form";
import { FormErrorMessage } from "@repo/ui/components/FormErrorMessage";
import { Heading } from "@repo/ui/components/Heading";
import { Modal } from "@repo/ui/components/Modal";
import { TextArea } from "@repo/ui/components/TextArea";
import { TextField } from "@repo/ui/components/TextField";
import { toastQueue } from "@repo/ui/components/Toast";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { useQueryClient } from "@tanstack/react-query";
import { XIcon } from "lucide-react";
import { api } from "@/shared/lib/api/client";

interface CreateTeamDialogProps {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
}

export function CreateTeamDialog({ isOpen, onOpenChange }: Readonly<CreateTeamDialogProps>) {
  const queryClient = useQueryClient();

  const createTeamMutation = api.useMutation("post", "/api/account-management/teams", {
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["/api/account-management/teams"] });

      toastQueue.add({
        title: t`Success`,
        description: t`Team created successfully`,
        variant: "success"
      });

      onOpenChange(false);
    }
  });

  const handleCancel = () => {
    onOpenChange(false);
  };

  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} isDismissable={true}>
      <Dialog className="sm:w-dialog-md">
        <XIcon onClick={handleCancel} className="absolute top-2 right-2 h-10 w-10 cursor-pointer p-2 hover:bg-muted" />
        <DialogHeader>
          <Heading slot="title" className="text-2xl">
            <Trans>Create Team</Trans>
          </Heading>
        </DialogHeader>

        <Form
          onSubmit={mutationSubmitter(createTeamMutation)}
          validationBehavior="aria"
          validationErrors={createTeamMutation.error?.errors}
          className="flex flex-col max-sm:h-full"
        >
          <DialogContent className="flex flex-col gap-4">
            <TextField
              autoFocus={true}
              isRequired={true}
              name="name"
              label={t`Name`}
              maxLength={100}
              className="flex-grow"
            />
            <TextArea name="description" label={t`Description`} maxLength={500} className="flex-grow" />
          </DialogContent>
          <DialogFooter>
            <FormErrorMessage error={createTeamMutation.error} />
            <Button type="reset" onPress={handleCancel} variant="secondary" isDisabled={createTeamMutation.isPending}>
              <Trans>Cancel</Trans>
            </Button>
            <Button type="submit" isDisabled={createTeamMutation.isPending}>
              {createTeamMutation.isPending ? <Trans>Creating...</Trans> : <Trans>Create Team</Trans>}
            </Button>
          </DialogFooter>
        </Form>
      </Dialog>
    </Modal>
  );
}

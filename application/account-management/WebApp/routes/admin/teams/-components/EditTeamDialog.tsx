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
import { useEffect } from "react";
import { api, type components } from "@/shared/lib/api/client";

type TeamSummary = components["schemas"]["TeamSummary"];

interface EditTeamDialogProps {
  team: TeamSummary | null;
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
}

export function EditTeamDialog({ team, isOpen, onOpenChange }: Readonly<EditTeamDialogProps>) {
  const queryClient = useQueryClient();

  const updateTeamMutation = api.useMutation("put", "/api/account-management/teams/{id}", {
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["/api/account-management/teams"] });
      if (team?.id) {
        queryClient.invalidateQueries({ queryKey: ["/api/account-management/teams/{id}", { id: team.id }] });
      }

      toastQueue.add({
        title: t`Success`,
        description: t`Team updated successfully`,
        variant: "success"
      });

      onOpenChange(false);
    }
  });

  useEffect(() => {
    if (!isOpen) {
      updateTeamMutation.reset();
    }
  }, [isOpen, updateTeamMutation]);

  const handleCancel = () => {
    onOpenChange(false);
  };

  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} isDismissable={true}>
      <Dialog className="sm:w-dialog-md">
        <XIcon onClick={handleCancel} className="absolute top-2 right-2 h-10 w-10 cursor-pointer p-2 hover:bg-muted" />
        <DialogHeader>
          <Heading slot="title" className="text-2xl">
            <Trans>Edit Team</Trans>
          </Heading>
        </DialogHeader>

        <Form
          onSubmit={mutationSubmitter(updateTeamMutation, {
            path: { id: team?.id || "" }
          })}
          validationBehavior="aria"
          validationErrors={updateTeamMutation.error?.errors}
          className="flex flex-col max-sm:h-full"
        >
          <DialogContent className="flex flex-col gap-4">
            <TextField
              autoFocus={true}
              isRequired={true}
              name="name"
              label={t`Name`}
              defaultValue={team?.name}
              maxLength={100}
              className="flex-grow"
            />
            <TextArea
              name="description"
              label={t`Description`}
              defaultValue={team?.description}
              maxLength={500}
              className="flex-grow"
            />
          </DialogContent>
          <DialogFooter>
            <FormErrorMessage error={updateTeamMutation.error} />
            <Button type="reset" onPress={handleCancel} variant="secondary" isDisabled={updateTeamMutation.isPending}>
              <Trans>Cancel</Trans>
            </Button>
            <Button type="submit" isDisabled={updateTeamMutation.isPending}>
              {updateTeamMutation.isPending ? <Trans>Saving...</Trans> : <Trans>Save Changes</Trans>}
            </Button>
          </DialogFooter>
        </Form>
      </Dialog>
    </Modal>
  );
}

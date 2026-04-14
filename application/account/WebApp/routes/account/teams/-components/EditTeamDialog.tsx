import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import {
  DialogBody,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@repo/ui/components/Dialog";
import { DirtyDialog } from "@repo/ui/components/DirtyDialog";
import { useDialogSetDirty } from "@repo/ui/components/DirtyDialogContext";
import { Form } from "@repo/ui/components/Form";
import { TextAreaField } from "@repo/ui/components/TextAreaField";
import { TextField } from "@repo/ui/components/TextField";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { api, type Schemas } from "@/shared/lib/api/client";

type Team = Schemas["TeamResponse"];

interface EditTeamDialogProps {
  team: Team | null;
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
}

export function EditTeamDialog({ team, isOpen, onOpenChange }: Readonly<EditTeamDialogProps>) {
  if (!team) {
    return null;
  }

  const handleClose = () => onOpenChange(false);

  return (
    <DirtyDialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Edit team">
      <DialogContent className="sm:w-dialog-md">
        <DialogHeader>
          <DialogTitle>
            <Trans>Edit team</Trans>
          </DialogTitle>
          <DialogDescription>
            <Trans>Update the team name or description.</Trans>
          </DialogDescription>
        </DialogHeader>
        <EditTeamDialogBody team={team} onClose={handleClose} />
      </DialogContent>
    </DirtyDialog>
  );
}

function EditTeamDialogBody({ team, onClose }: { team: Team; onClose: () => void }) {
  const setDirty = useDialogSetDirty();
  const queryClient = useQueryClient();

  const updateTeamMutation = api.useMutation("put", "/api/account/teams/{id}", {
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/teams"] });
      toast.success(t`Team updated`);
      onClose();
    }
  });

  return (
    <Form
      onSubmit={mutationSubmitter(updateTeamMutation, { path: { id: team.id } })}
      validationErrors={updateTeamMutation.error?.errors}
      validationBehavior="aria"
      className="flex flex-col max-sm:h-full"
    >
      <DialogBody className="flex flex-col gap-4">
        <TextField
          autoFocus={true}
          required={true}
          name="name"
          label={t`Name`}
          defaultValue={team.name}
          placeholder={t`E.g., Engineering`}
          onChange={() => setDirty(true)}
        />
        <TextAreaField
          name="description"
          label={t`Description`}
          defaultValue={team.description ?? ""}
          placeholder={t`What is this team responsible for?`}
          onChange={() => setDirty(true)}
        />
      </DialogBody>
      <DialogFooter>
        <DialogClose render={<Button type="reset" variant="secondary" disabled={updateTeamMutation.isPending} />}>
          <Trans>Cancel</Trans>
        </DialogClose>
        <Button type="submit" disabled={updateTeamMutation.isPending}>
          {updateTeamMutation.isPending ? <Trans>Saving...</Trans> : <Trans>Save changes</Trans>}
        </Button>
      </DialogFooter>
    </Form>
  );
}

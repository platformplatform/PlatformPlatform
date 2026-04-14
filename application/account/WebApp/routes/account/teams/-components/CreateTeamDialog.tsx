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

import { api } from "@/shared/lib/api/client";

interface CreateTeamDialogProps {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
}

export function CreateTeamDialog({ isOpen, onOpenChange }: Readonly<CreateTeamDialogProps>) {
  const handleClose = () => onOpenChange(false);

  return (
    <DirtyDialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Create team">
      <DialogContent className="sm:w-dialog-md">
        <DialogHeader>
          <DialogTitle>
            <Trans>Create team</Trans>
          </DialogTitle>
          <DialogDescription>
            <Trans>Teams group users together so you can manage access and responsibilities.</Trans>
          </DialogDescription>
        </DialogHeader>
        <CreateTeamDialogBody onClose={handleClose} />
      </DialogContent>
    </DirtyDialog>
  );
}

function CreateTeamDialogBody({ onClose }: { onClose: () => void }) {
  const setDirty = useDialogSetDirty();
  const queryClient = useQueryClient();

  const createTeamMutation = api.useMutation("post", "/api/account/teams", {
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/teams"] });
      toast.success(t`Team created`);
      onClose();
    }
  });

  return (
    <Form
      onSubmit={mutationSubmitter(createTeamMutation)}
      validationErrors={createTeamMutation.error?.errors}
      validationBehavior="aria"
      className="flex flex-col max-sm:h-full"
    >
      <DialogBody className="flex flex-col gap-4">
        <TextField
          autoFocus={true}
          required={true}
          name="name"
          label={t`Name`}
          placeholder={t`E.g., Engineering`}
          onChange={() => setDirty(true)}
        />
        <TextAreaField
          name="description"
          label={t`Description`}
          placeholder={t`What is this team responsible for?`}
          onChange={() => setDirty(true)}
        />
      </DialogBody>
      <DialogFooter>
        <DialogClose render={<Button type="reset" variant="secondary" disabled={createTeamMutation.isPending} />}>
          <Trans>Cancel</Trans>
        </DialogClose>
        <Button type="submit" disabled={createTeamMutation.isPending}>
          {createTeamMutation.isPending ? <Trans>Creating...</Trans> : <Trans>Create team</Trans>}
        </Button>
      </DialogFooter>
    </Form>
  );
}

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Dialog } from "@repo/ui/components/Dialog";
import { DialogContent, DialogFooter, DialogHeader } from "@repo/ui/components/DialogFooter";
import { Form } from "@repo/ui/components/Form";
import { Heading } from "@repo/ui/components/Heading";
import { Modal } from "@repo/ui/components/Modal";
import { TextArea } from "@repo/ui/components/TextArea";
import { TextField } from "@repo/ui/components/TextField";
import { toastQueue } from "@repo/ui/components/Toast";
import { XIcon } from "lucide-react";
import { useEffect, useState } from "react";
import type { components } from "@/shared/lib/api/client";

type TeamSummary = components["schemas"]["TeamSummary"];

interface EditTeamDialogProps {
  team: TeamSummary | null;
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  onTeamUpdated?: (team: TeamSummary) => void;
}

export function EditTeamDialog({ team, isOpen, onOpenChange, onTeamUpdated }: Readonly<EditTeamDialogProps>) {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (team && isOpen) {
      setName(team.name);
      setDescription(team.description);
    }
  }, [team, isOpen]);

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!name.trim() || !team) {
      return;
    }

    if (name.length > 100) {
      return;
    }

    if (description.length > 500) {
      return;
    }

    setIsSubmitting(true);

    setTimeout(() => {
      const updatedTeam: TeamSummary = {
        ...team,
        name: name.trim(),
        description: description.trim()
      };

      if (onTeamUpdated) {
        onTeamUpdated(updatedTeam);
      }

      toastQueue.add({
        title: t`Success`,
        description: t`Team updated successfully`,
        variant: "success"
      });

      setIsSubmitting(false);
      onOpenChange(false);
    }, 300);
  };

  const handleCancel = () => {
    if (team) {
      setName(team.name);
      setDescription(team.description);
    }
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

        <Form onSubmit={handleSubmit} validationBehavior="native" className="flex flex-col max-sm:h-full">
          <DialogContent className="flex flex-col gap-4">
            <TextField
              autoFocus={true}
              isRequired={true}
              name="name"
              label={t`Name`}
              value={name}
              onChange={setName}
              maxLength={100}
              className="flex-grow"
            />
            <TextArea
              name="description"
              label={t`Description`}
              value={description}
              onChange={setDescription}
              maxLength={500}
              className="flex-grow"
            />
          </DialogContent>
          <DialogFooter>
            <Button type="reset" onPress={handleCancel} variant="secondary" isDisabled={isSubmitting}>
              <Trans>Cancel</Trans>
            </Button>
            <Button type="submit" isDisabled={isSubmitting}>
              <Trans>Save Changes</Trans>
            </Button>
          </DialogFooter>
        </Form>
      </Dialog>
    </Modal>
  );
}

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
import { useState } from "react";

interface CreateTeamDialogProps {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  onTeamCreated: (team: { id: string; name: string; description: string; memberCount: number }) => void;
}

export function CreateTeamDialog({ isOpen, onOpenChange, onTeamCreated }: Readonly<CreateTeamDialogProps>) {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!name.trim()) {
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
      const newTeam = {
        id: `team-${Date.now()}`,
        name: name.trim(),
        description: description.trim(),
        memberCount: 0
      };

      onTeamCreated(newTeam);

      toastQueue.add({
        title: t`Success`,
        description: t`Team created successfully`,
        variant: "success"
      });

      setName("");
      setDescription("");
      setIsSubmitting(false);
      onOpenChange(false);
    }, 300);
  };

  const handleCancel = () => {
    setName("");
    setDescription("");
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
              <Trans>Create Team</Trans>
            </Button>
          </DialogFooter>
        </Form>
      </Dialog>
    </Modal>
  );
}

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { userCollection } from "@repo/infrastructure/sync/collections";
import { useElectricMutation } from "@repo/infrastructure/sync/useElectricMutation";
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
import { TextField } from "@repo/ui/components/TextField";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { toast } from "sonner";

import { apiClient } from "@/shared/lib/api/client";

interface InviteUserDialogProps {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
}

export default function InviteUserDialog({ isOpen, onOpenChange }: Readonly<InviteUserDialogProps>) {
  const handleClose = () => onOpenChange(false);

  return (
    <DirtyDialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Invite user">
      <DialogContent className="sm:w-dialog-md">
        <DialogHeader>
          <DialogTitle>
            <Trans>Invite user</Trans>
          </DialogTitle>
          <DialogDescription>
            <Trans>An email with login instructions will be sent to the user.</Trans>
          </DialogDescription>
        </DialogHeader>
        <InviteUserDialogBody onClose={handleClose} />
      </DialogContent>
    </DirtyDialog>
  );
}

function InviteUserDialogBody({ onClose }: { onClose: () => void }) {
  const setDirty = useDialogSetDirty();
  const inviteUserMutation = useElectricMutation({
    mutationFn: async (vars: { body: { email: string } }) => {
      const { data, error } = await apiClient.POST("/api/account/users/invite", vars);
      if (error) {
        throw error;
      }
      return data;
    },
    utils: userCollection.utils,
    onInsert: (data) => {
      if (data && typeof data === "object" && "id" in data) {
        const userData = data as Record<string, unknown>;
        if (!userCollection.has(userData.id as string)) {
          userCollection.insert({
            id: userData.id as string,
            createdAt: (userData.createdAt as string) ?? new Date().toISOString(),
            modifiedAt: (userData.modifiedAt as string | null) ?? null,
            email: (userData.email as string) ?? "",
            firstName: (userData.firstName as string | null) ?? null,
            lastName: (userData.lastName as string | null) ?? null,
            title: (userData.title as string | null) ?? null,
            role: (userData.role as string) ?? "Member",
            emailConfirmed: (userData.emailConfirmed as boolean) ?? false,
            avatar: userData.avatar ? JSON.stringify(userData.avatar) : "",
            locale: (userData.locale as string) ?? "",
            lastSeenAt: (userData.lastSeenAt as string | null) ?? null,
            deletedAt: null
          });
        }
      }
    },
    onSuccess: () => {
      toast.success(t`User invited successfully`);
      onClose();
    }
  });

  return (
    <Form
      onSubmit={mutationSubmitter(inviteUserMutation)}
      validationErrors={inviteUserMutation.validationErrors}
      validationBehavior="aria"
      className="flex flex-col max-sm:h-full"
    >
      <DialogBody>
        <TextField
          autoFocus={true}
          required={true}
          name="email"
          label={t`Email`}
          placeholder={t`user@email.com`}
          className="flex-grow"
          onChange={() => setDirty(true)}
        />
      </DialogBody>
      <DialogFooter>
        <DialogClose render={<Button type="reset" variant="secondary" disabled={inviteUserMutation.isPending} />}>
          <Trans>Cancel</Trans>
        </DialogClose>
        <Button type="submit" disabled={inviteUserMutation.isPending}>
          {inviteUserMutation.isPending ? <Trans>Sending...</Trans> : <Trans>Send invite</Trans>}
        </Button>
      </DialogFooter>
    </Form>
  );
}

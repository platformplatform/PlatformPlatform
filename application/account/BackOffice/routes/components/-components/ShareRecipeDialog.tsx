import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { CheckboxField } from "@repo/ui/components/CheckboxField";
import {
  DialogBody,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogForm,
  DialogHeader,
  DialogTitle
} from "@repo/ui/components/Dialog";
import { DirtyDialog } from "@repo/ui/components/DirtyDialog";
import { useDialogSetDirty } from "@repo/ui/components/DirtyDialogContext";
import { Field, FieldContent, FieldDescription, FieldLabel, FieldTitle } from "@repo/ui/components/Field";
import { InlineFieldGroup } from "@repo/ui/components/InlineFieldGroup";
import { RadioGroup, RadioGroupItem } from "@repo/ui/components/RadioGroup";
import { TextField } from "@repo/ui/components/TextField";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { useMutation } from "@tanstack/react-query";
import { ChefHatIcon, EyeIcon, PencilIcon } from "lucide-react";
import { toast } from "sonner";

import { type DialogSize, getDialogSizeClassName } from "./dialogSize";

export interface ShareRecipeDialogProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  dirtyDialog: boolean;
  showToasts: boolean;
  simulateErrors: boolean;
  size: DialogSize;
}

export function ShareRecipeDialog({
  isOpen,
  onOpenChange,
  dirtyDialog,
  showToasts,
  simulateErrors,
  size
}: Readonly<ShareRecipeDialogProps>) {
  const handleClose = () => onOpenChange(false);
  return (
    <DirtyDialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Share recipe">
      <DialogContent className={getDialogSizeClassName(size)}>
        <DialogHeader>
          <DialogTitle>
            <Trans>Share recipe</Trans>
          </DialogTitle>
          <DialogDescription>
            <Trans>Share this recipe with a friend or family member.</Trans>
          </DialogDescription>
        </DialogHeader>
        <ShareRecipeDialogBody
          onClose={handleClose}
          dirtyDialog={dirtyDialog}
          showToasts={showToasts}
          simulateErrors={simulateErrors}
        />
      </DialogContent>
    </DirtyDialog>
  );
}

function ShareRecipeDialogBody({
  onClose,
  dirtyDialog,
  showToasts,
  simulateErrors
}: {
  onClose: () => void;
  dirtyDialog: boolean;
  showToasts: boolean;
  simulateErrors: boolean;
}) {
  const setDirty = useDialogSetDirty();
  const mutation = useMutation({
    mutationFn: async (_data: { body?: unknown }) => {
      await new Promise<void>((resolve) => setTimeout(resolve, 500));
    },
    onSuccess: () => {
      if (showToasts) toast.success(t`Recipe shared`);
      onClose();
    }
  });

  const markDirty = () => {
    if (dirtyDialog) setDirty(true);
  };

  return (
    <DialogForm
      onSubmit={mutationSubmitter(mutation)}
      validationErrors={simulateErrors ? { email: [t`Please enter a valid email address`] } : undefined}
    >
      <DialogBody>
        <div className="flex flex-col gap-6">
          <TextField
            autoFocus
            name="email"
            label={t`Email address`}
            type="email"
            placeholder={t`friend@example.com`}
            onChange={markDirty}
          />
          <div className="flex flex-col gap-2">
            <p className="text-sm font-medium">
              <Trans>Share permission</Trans>
            </p>
            <RadioGroup defaultValue="view" onValueChange={markDirty}>
              <FieldLabel>
                <Field orientation="horizontal">
                  <RadioGroupItem value="coauthor" />
                  <FieldContent>
                    <FieldTitle>
                      <ChefHatIcon />
                      <Trans>Co-author</Trans>
                    </FieldTitle>
                    <FieldDescription>
                      <Trans>Full access, can edit and share with others</Trans>
                    </FieldDescription>
                  </FieldContent>
                </Field>
              </FieldLabel>
              <FieldLabel>
                <Field orientation="horizontal">
                  <RadioGroupItem value="edit" />
                  <FieldContent>
                    <FieldTitle>
                      <PencilIcon />
                      <Trans>Can edit</Trans>
                    </FieldTitle>
                    <FieldDescription>
                      <Trans>Can change ingredients and instructions</Trans>
                    </FieldDescription>
                  </FieldContent>
                </Field>
              </FieldLabel>
              <FieldLabel>
                <Field orientation="horizontal">
                  <RadioGroupItem value="view" />
                  <FieldContent>
                    <FieldTitle>
                      <EyeIcon />
                      <Trans>Can view</Trans>
                    </FieldTitle>
                    <FieldDescription>
                      <Trans>Read only access to the recipe</Trans>
                    </FieldDescription>
                  </FieldContent>
                </Field>
              </FieldLabel>
            </RadioGroup>
          </div>
          <InlineFieldGroup>
            <CheckboxField
              name="includeNotes"
              label={t`Include cooking notes`}
              defaultChecked
              onCheckedChange={markDirty}
            />
          </InlineFieldGroup>
        </div>
      </DialogBody>
      <DialogFooter>
        <DialogClose render={<Button type="reset" variant="secondary" disabled={mutation.isPending} />}>
          <Trans>Cancel</Trans>
        </DialogClose>
        <Button type="submit" isPending={mutation.isPending}>
          {mutation.isPending ? <Trans>Sharing...</Trans> : <Trans>Share recipe</Trans>}
        </Button>
      </DialogFooter>
    </DialogForm>
  );
}

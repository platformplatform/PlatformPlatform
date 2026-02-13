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
import { Form } from "@repo/ui/components/Form";
import { TextField } from "@repo/ui/components/TextField";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { toast } from "sonner";
import type { components } from "@/shared/lib/api/api.generated";
import { api } from "@/shared/lib/api/client";

type BillingInfo = components["schemas"]["BillingInfo"];

interface EditBillingInfoDialogProps {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  billingInfo: BillingInfo | null | undefined;
}

export function EditBillingInfoDialog({ isOpen, onOpenChange, billingInfo }: Readonly<EditBillingInfoDialogProps>) {
  const [isFormDirty, setIsFormDirty] = useState(false);
  const queryClient = useQueryClient();

  const mutation = api.useMutation("put", "/api/account/subscriptions/billing-info", {
    onSuccess: () => {
      setIsFormDirty(false);
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/subscriptions/current"] });
      toast.success(t`Billing information updated`);
      onOpenChange(false);
    }
  });

  const handleCloseComplete = () => {
    setIsFormDirty(false);
  };

  const markDirty = () => setIsFormDirty(true);

  return (
    <DirtyDialog
      trackingTitle="Edit billing info"
      open={isOpen}
      onOpenChange={onOpenChange}
      hasUnsavedChanges={isFormDirty}
      unsavedChangesTitle={t`Unsaved changes`}
      unsavedChangesMessage={<Trans>You have unsaved changes. If you leave now, your changes will be lost.</Trans>}
      leaveLabel={t`Leave`}
      stayLabel={t`Stay`}
      onCloseComplete={handleCloseComplete}
    >
      <DialogContent className="sm:w-dialog-md">
        <DialogHeader>
          <DialogTitle>
            <Trans>Edit billing information</Trans>
          </DialogTitle>
          <DialogDescription>
            <Trans>Update the billing details associated with your subscription.</Trans>
          </DialogDescription>
        </DialogHeader>

        <Form
          onSubmit={mutationSubmitter(mutation)}
          validationErrors={mutation.error?.errors}
          validationBehavior="aria"
          className="flex flex-col max-sm:h-full"
        >
          <DialogBody>
            <div className="flex flex-col gap-4">
              <TextField
                autoFocus={true}
                name="line1"
                label={t`Address line 1`}
                defaultValue={billingInfo?.address?.line1 ?? ""}
                placeholder={t`Street address`}
                onChange={markDirty}
              />
              <TextField
                name="line2"
                label={t`Address line 2`}
                defaultValue={billingInfo?.address?.line2 ?? ""}
                placeholder={t`Apartment, suite, etc.`}
                onChange={markDirty}
              />
              <div className="grid grid-cols-2 gap-4">
                <TextField
                  name="postalCode"
                  label={t`Postal code`}
                  defaultValue={billingInfo?.address?.postalCode ?? ""}
                  onChange={markDirty}
                />
                <TextField
                  name="city"
                  label={t`City`}
                  defaultValue={billingInfo?.address?.city ?? ""}
                  onChange={markDirty}
                />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <TextField
                  name="state"
                  label={t`State / Province`}
                  defaultValue={billingInfo?.address?.state ?? ""}
                  onChange={markDirty}
                />
                <TextField
                  name="country"
                  label={t`Country`}
                  defaultValue={billingInfo?.address?.country ?? ""}
                  onChange={markDirty}
                />
              </div>
              <TextField
                name="email"
                label={t`Email`}
                defaultValue={billingInfo?.email ?? ""}
                placeholder={t`billing@company.com`}
                onChange={markDirty}
              />
            </div>
          </DialogBody>
          <DialogFooter>
            <DialogClose render={<Button type="reset" variant="secondary" disabled={mutation.isPending} />}>
              <Trans>Cancel</Trans>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? <Trans>Saving...</Trans> : <Trans>Save</Trans>}
            </Button>
          </DialogFooter>
        </Form>
      </DialogContent>
    </DirtyDialog>
  );
}

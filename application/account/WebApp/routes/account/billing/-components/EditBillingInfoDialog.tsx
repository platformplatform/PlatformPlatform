import type { BillingInfo } from "@repo/infrastructure/sync/hooks";

import { t } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
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
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { useState } from "react";
import { toast } from "sonner";

import { api } from "@/shared/lib/api/client";

import { BillingInfoFormFields } from "./BillingInfoFormFields";
import { useCountryOptions } from "./CountrySelect";

interface EditBillingInfoDialogProps {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  billingInfo: BillingInfo | null | undefined;
  tenantName: string;
  onSuccess?: () => void;
  submitLabel?: string;
  pendingLabel?: string;
}

export function EditBillingInfoDialog({
  isOpen,
  onOpenChange,
  billingInfo,
  tenantName,
  onSuccess,
  submitLabel,
  pendingLabel
}: Readonly<EditBillingInfoDialogProps>) {
  const [isFormDirty, setIsFormDirty] = useState(false);
  const [selectedCountry, setSelectedCountry] = useState(billingInfo?.address?.country ?? undefined);
  const userInfo = useUserInfo();
  const { i18n } = useLingui();
  const countries = useCountryOptions(i18n.locale);

  const mutation = api.useMutation("put", "/api/account/billing/billing-info", {
    onSuccess: () => {
      setIsFormDirty(false);
      toast.success(t`Billing information updated`);
      onOpenChange(false);
      onSuccess?.();
    }
  });

  const handleCloseComplete = () => {
    setIsFormDirty(false);
    setSelectedCountry(billingInfo?.address?.country ?? undefined);
  };

  const markDirty = () => setIsFormDirty(true);
  const isNewBillingInfo = !billingInfo?.address;

  return (
    <DirtyDialog
      open={isOpen}
      onOpenChange={onOpenChange}
      hasUnsavedChanges={isFormDirty}
      unsavedChangesTitle={t`Unsaved changes`}
      unsavedChangesMessage={<Trans>You have unsaved changes. If you leave now, your changes will be lost.</Trans>}
      leaveLabel={t`Leave`}
      stayLabel={t`Stay`}
      onCloseComplete={handleCloseComplete}
      trackingTitle="Edit billing info"
    >
      <DialogContent className="sm:w-dialog-lg">
        <DialogHeader>
          <DialogTitle>
            {isNewBillingInfo ? <Trans>Add billing information</Trans> : <Trans>Edit billing information</Trans>}
          </DialogTitle>
          <DialogDescription>
            {isNewBillingInfo ? (
              <Trans>Enter the billing details for your subscription.</Trans>
            ) : (
              <Trans>Update the billing details associated with your subscription.</Trans>
            )}
          </DialogDescription>
        </DialogHeader>
        <Form
          onSubmit={mutationSubmitter(mutation)}
          validationErrors={mutation.error?.errors}
          className="flex min-h-0 flex-1 flex-col"
        >
          <DialogBody>
            <BillingInfoFormFields
              billingInfo={billingInfo}
              tenantName={tenantName}
              defaultEmail={billingInfo?.email ?? userInfo?.email ?? ""}
              countries={countries}
              selectedCountry={selectedCountry}
              onCountryChange={(value) => {
                setSelectedCountry(value ?? undefined);
                markDirty();
              }}
              onFieldChange={markDirty}
            />
          </DialogBody>
          <DialogFooter>
            <DialogClose render={<Button type="reset" variant="secondary" disabled={mutation.isPending} />}>
              <Trans>Cancel</Trans>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? (pendingLabel ?? t`Saving...`) : (submitLabel ?? t`Save`)}
            </Button>
          </DialogFooter>
        </Form>
      </DialogContent>
    </DirtyDialog>
  );
}

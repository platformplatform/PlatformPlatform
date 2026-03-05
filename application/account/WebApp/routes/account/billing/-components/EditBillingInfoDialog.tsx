import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import type { BillingInfo } from "@repo/infrastructure/sync/hooks";
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
import { TextAreaField } from "@repo/ui/components/TextAreaField";
import { TextField } from "@repo/ui/components/TextField";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { useState } from "react";
import { toast } from "sonner";
import { CountrySelect, useCountryOptions } from "@/shared/components/CountrySelect";
import { api } from "@/shared/lib/api/client";

const stateRequiredCountries = ["US", "CA", "AU", "IN", "BR", "MX", "JP", "FR", "ES", "IT", "NL", "KR", "NZ", "IE"];

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
  const countries = useCountryOptions();

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
  const showStateField = selectedCountry != null && stateRequiredCountries.includes(selectedCountry);

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
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <TextField
                autoFocus={true}
                name="email"
                label={t`Billing email`}
                defaultValue={billingInfo?.email ?? userInfo?.email ?? ""}
                placeholder={t`billing@company.com`}
                onChange={markDirty}
              />
              <CountrySelect
                countries={countries}
                defaultValue={billingInfo?.address?.country ?? undefined}
                onValueChange={(value) => {
                  setSelectedCountry(value ?? undefined);
                  markDirty();
                }}
              />
              <TextField
                name="name"
                label={t`Name`}
                defaultValue={billingInfo?.name ?? tenantName}
                placeholder={t`Name as it appears on invoices`}
                className="sm:col-span-2"
                onChange={markDirty}
              />
              <TextAreaField
                name="address"
                label={t`Address`}
                defaultValue={[billingInfo?.address?.line1, billingInfo?.address?.line2].filter(Boolean).join("\n")}
                placeholder={t`Street address`}
                textareaClassName="resize-none"
                className="sm:col-span-2"
                onChange={markDirty}
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    const lines = e.currentTarget.value.split("\n");
                    if (lines.length >= 2) {
                      e.preventDefault();
                    }
                  }
                }}
                onPaste={(e) => {
                  const textarea = e.currentTarget;
                  const paste = e.clipboardData.getData("text");
                  const before = textarea.value.slice(0, textarea.selectionStart);
                  const after = textarea.value.slice(textarea.selectionEnd);
                  const result = before + paste + after;
                  const lines = result.split("\n");
                  if (lines.length > 2) {
                    e.preventDefault();
                    textarea.value = lines.slice(0, 2).join("\n");
                    markDirty();
                  }
                }}
              />
              <div className="grid grid-cols-3 gap-4 sm:col-span-2 sm:grid-cols-2">
                <TextField
                  name="postalCode"
                  label={t`Postal code`}
                  defaultValue={billingInfo?.address?.postalCode ?? ""}
                  placeholder={t`Postal code`}
                  onChange={markDirty}
                />
                <TextField
                  name="city"
                  label={t`City`}
                  defaultValue={billingInfo?.address?.city ?? ""}
                  placeholder={t`City`}
                  className="col-span-2 sm:col-span-1"
                  onChange={markDirty}
                />
              </div>
              {showStateField && (
                <TextField
                  name="state"
                  label={t`State / Province`}
                  defaultValue={billingInfo?.address?.state ?? ""}
                  placeholder={t`State or region`}
                  onChange={markDirty}
                />
              )}
              <TextField
                name="taxId"
                label={t`Tax ID (VAT number)`}
                defaultValue={billingInfo?.taxId ?? ""}
                placeholder={t`VAT number`}
                description={t`Please include your country code`}
                className={!showStateField ? "sm:col-span-2" : undefined}
                onChange={markDirty}
              />
            </div>
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

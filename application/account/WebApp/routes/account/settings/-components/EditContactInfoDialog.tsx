import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import type { ContactInfo } from "@repo/infrastructure/sync/hooks";
import { Button } from "@repo/ui/components/Button";
import {
  DialogBody,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@repo/ui/components/Dialog";
import { DirtyDialog } from "@repo/ui/components/DirtyDialog";
import { Form } from "@repo/ui/components/Form";
import { TextAreaField } from "@repo/ui/components/TextAreaField";
import { TextField } from "@repo/ui/components/TextField";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { useEffect, useState } from "react";
import { toast } from "sonner";
import { CountrySelect, useCountryOptions } from "@/shared/components/CountrySelect";
import { api } from "@/shared/lib/api/client";

const stateRequiredCountries = ["US", "CA", "AU", "IN", "BR", "MX", "JP", "FR", "ES", "IT", "NL", "KR", "NZ", "IE"];

interface EditContactInfoDialogProps {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  contactInfo: ContactInfo | null;
}

export default function EditContactInfoDialog({
  isOpen,
  onOpenChange,
  contactInfo
}: Readonly<EditContactInfoDialogProps>) {
  const [isFormDirty, setIsFormDirty] = useState(false);
  const [phoneNumber, setPhoneNumber] = useState("");
  const [address, setAddress] = useState("");
  const [postalCode, setPostalCode] = useState("");
  const [city, setCity] = useState("");
  const [state, setState] = useState("");
  const [country, setCountry] = useState<string | undefined>(undefined);
  const [selectedCountry, setSelectedCountry] = useState<string | undefined>(undefined);
  const countries = useCountryOptions();

  useEffect(() => {
    if (!isFormDirty && contactInfo) {
      setPhoneNumber(contactInfo.phoneNumber ?? "");
      setAddress(contactInfo.address ?? "");
      setPostalCode(contactInfo.postalCode ?? "");
      setCity(contactInfo.city ?? "");
      setState(contactInfo.state ?? "");
      setCountry(contactInfo.country ?? undefined);
      setSelectedCountry(contactInfo.country ?? undefined);
    }
  }, [contactInfo, isFormDirty]);

  const mutation = api.useMutation("put", "/api/account/tenants/current/contact-info", {
    onSuccess: () => {
      setIsFormDirty(false);
      toast.success(t`Contact information updated`);
      onOpenChange(false);
    }
  });

  const handleCloseComplete = () => {
    setIsFormDirty(false);
    setPhoneNumber(contactInfo?.phoneNumber ?? "");
    setAddress(contactInfo?.address ?? "");
    setPostalCode(contactInfo?.postalCode ?? "");
    setCity(contactInfo?.city ?? "");
    setState(contactInfo?.state ?? "");
    setCountry(contactInfo?.country ?? undefined);
    setSelectedCountry(contactInfo?.country ?? undefined);
  };

  const markDirty = () => setIsFormDirty(true);
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
      trackingTitle="Edit contact information"
    >
      <DialogContent className="sm:w-dialog-lg">
        <DialogHeader>
          <DialogTitle>
            <Trans>Edit contact information</Trans>
          </DialogTitle>
        </DialogHeader>

        <Form
          onSubmit={mutationSubmitter(mutation)}
          validationBehavior="aria"
          validationErrors={mutation.error?.errors}
          className="flex min-h-0 flex-1 flex-col"
        >
          <DialogBody>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <TextAreaField
                autoFocus={true}
                name="address"
                label={t`Address`}
                value={address}
                onChange={(value) => {
                  setAddress(value);
                  markDirty();
                }}
                placeholder={t`Street address`}
                textareaClassName="resize-none"
                className="sm:col-span-2"
                disabled={mutation.isPending}
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
              <div className="grid grid-cols-3 gap-4 sm:col-span-2">
                <TextField
                  name="postalCode"
                  label={t`Postal code`}
                  value={postalCode}
                  onChange={(value) => {
                    setPostalCode(value);
                    markDirty();
                  }}
                  placeholder={t`Postal code`}
                  disabled={mutation.isPending}
                />
                <TextField
                  name="city"
                  label={t`City`}
                  value={city}
                  onChange={(value) => {
                    setCity(value);
                    markDirty();
                  }}
                  placeholder={t`City`}
                  className="col-span-2"
                  disabled={mutation.isPending}
                />
              </div>
              {showStateField && (
                <TextField
                  name="state"
                  label={t`State / Province`}
                  value={state}
                  onChange={(value) => {
                    setState(value);
                    markDirty();
                  }}
                  placeholder={t`State or region`}
                  disabled={mutation.isPending}
                />
              )}
              <CountrySelect
                countries={countries}
                value={country ?? null}
                onValueChange={(value) => {
                  setCountry(value ?? undefined);
                  setSelectedCountry(value ?? undefined);
                  markDirty();
                }}
                disabled={mutation.isPending}
                className={!showStateField ? "sm:col-span-2" : undefined}
              />
              <TextField
                name="phoneNumber"
                label={t`Phone number`}
                value={phoneNumber}
                onChange={(value) => {
                  setPhoneNumber(value);
                  markDirty();
                }}
                placeholder={t`E.g., +45 12345678`}
                className="sm:col-span-2"
                disabled={mutation.isPending}
              />
            </div>
          </DialogBody>
          <DialogFooter>
            <DialogClose render={<Button type="reset" variant="secondary" disabled={mutation.isPending} />}>
              <Trans>Cancel</Trans>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? <Trans>Saving...</Trans> : <Trans>Save changes</Trans>}
            </Button>
          </DialogFooter>
        </Form>
      </DialogContent>
    </DirtyDialog>
  );
}

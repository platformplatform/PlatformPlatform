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
import { TextField } from "@repo/ui/components/TextField";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { useEffect, useState } from "react";
import { toast } from "sonner";
import { CountrySelect, useCountryOptions } from "@/shared/components/CountrySelect";
import { api } from "@/shared/lib/api/client";

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
  const [street, setStreet] = useState("");
  const [postalCode, setPostalCode] = useState("");
  const [city, setCity] = useState("");
  const [country, setCountry] = useState<string | undefined>(undefined);
  const countries = useCountryOptions();

  useEffect(() => {
    if (!isFormDirty && contactInfo) {
      setPhoneNumber(contactInfo.phoneNumber ?? "");
      setStreet(contactInfo.street ?? "");
      setPostalCode(contactInfo.postalCode ?? "");
      setCity(contactInfo.city ?? "");
      setCountry(contactInfo.country ?? undefined);
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
    setStreet(contactInfo?.street ?? "");
    setPostalCode(contactInfo?.postalCode ?? "");
    setCity(contactInfo?.city ?? "");
    setCountry(contactInfo?.country ?? undefined);
  };

  const markDirty = () => setIsFormDirty(true);

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
      <DialogContent className="sm:w-dialog-md">
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
              <TextField
                autoFocus={true}
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
              <TextField
                name="street"
                label={t`Address`}
                value={street}
                onChange={(value) => {
                  setStreet(value);
                  markDirty();
                }}
                placeholder={t`Street address`}
                className="sm:col-span-2"
                disabled={mutation.isPending}
              />
              <div className="grid grid-cols-3 gap-4 sm:col-span-2 sm:grid-cols-2">
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
                  className="col-span-2 sm:col-span-1"
                  disabled={mutation.isPending}
                />
              </div>
              <CountrySelect
                countries={countries}
                value={country ?? null}
                onValueChange={(value) => {
                  setCountry(value ?? undefined);
                  markDirty();
                }}
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

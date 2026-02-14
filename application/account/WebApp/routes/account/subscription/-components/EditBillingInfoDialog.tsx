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
import { Field, FieldError, FieldLabel } from "@repo/ui/components/Field";
import { Form, FormValidationContext } from "@repo/ui/components/Form";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@repo/ui/components/Select";
import { TextField } from "@repo/ui/components/TextField";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { useQueryClient } from "@tanstack/react-query";
import { useContext, useMemo, useState } from "react";
import { toast } from "sonner";
import type { components } from "@/shared/lib/api/api.generated";
import { api } from "@/shared/lib/api/client";

type BillingInfo = components["schemas"]["BillingInfo"];

// Hardcoded ISO 3166-1 alpha-2 codes because Intl.DisplayNames resolves historical and pseudo codes that Stripe rejects
const countryCodes = [
  "AD",
  "AE",
  "AF",
  "AG",
  "AI",
  "AL",
  "AM",
  "AO",
  "AQ",
  "AR",
  "AS",
  "AT",
  "AU",
  "AW",
  "AX",
  "AZ",
  "BA",
  "BB",
  "BD",
  "BE",
  "BF",
  "BG",
  "BH",
  "BI",
  "BJ",
  "BL",
  "BM",
  "BN",
  "BO",
  "BQ",
  "BR",
  "BS",
  "BT",
  "BV",
  "BW",
  "BY",
  "BZ",
  "CA",
  "CC",
  "CD",
  "CF",
  "CG",
  "CH",
  "CI",
  "CK",
  "CL",
  "CM",
  "CN",
  "CO",
  "CR",
  "CU",
  "CV",
  "CW",
  "CX",
  "CY",
  "CZ",
  "DE",
  "DJ",
  "DK",
  "DM",
  "DO",
  "DZ",
  "EC",
  "EE",
  "EG",
  "EH",
  "ER",
  "ES",
  "ET",
  "FI",
  "FJ",
  "FK",
  "FM",
  "FO",
  "FR",
  "GA",
  "GB",
  "GD",
  "GE",
  "GF",
  "GG",
  "GH",
  "GI",
  "GL",
  "GM",
  "GN",
  "GP",
  "GQ",
  "GR",
  "GS",
  "GT",
  "GU",
  "GW",
  "GY",
  "HK",
  "HM",
  "HN",
  "HR",
  "HT",
  "HU",
  "ID",
  "IE",
  "IL",
  "IM",
  "IN",
  "IO",
  "IQ",
  "IR",
  "IS",
  "IT",
  "JE",
  "JM",
  "JO",
  "JP",
  "KE",
  "KG",
  "KH",
  "KI",
  "KM",
  "KN",
  "KP",
  "KR",
  "KW",
  "KY",
  "KZ",
  "LA",
  "LB",
  "LC",
  "LI",
  "LK",
  "LR",
  "LS",
  "LT",
  "LU",
  "LV",
  "LY",
  "MA",
  "MC",
  "MD",
  "ME",
  "MF",
  "MG",
  "MH",
  "MK",
  "ML",
  "MM",
  "MN",
  "MO",
  "MP",
  "MQ",
  "MR",
  "MS",
  "MT",
  "MU",
  "MV",
  "MW",
  "MX",
  "MY",
  "MZ",
  "NA",
  "NC",
  "NE",
  "NF",
  "NG",
  "NI",
  "NL",
  "NO",
  "NP",
  "NR",
  "NU",
  "NZ",
  "OM",
  "PA",
  "PE",
  "PF",
  "PG",
  "PH",
  "PK",
  "PL",
  "PM",
  "PN",
  "PR",
  "PS",
  "PT",
  "PW",
  "PY",
  "QA",
  "RE",
  "RO",
  "RS",
  "RU",
  "RW",
  "SA",
  "SB",
  "SC",
  "SD",
  "SE",
  "SG",
  "SH",
  "SI",
  "SJ",
  "SK",
  "SL",
  "SM",
  "SN",
  "SO",
  "SR",
  "SS",
  "ST",
  "SV",
  "SX",
  "SY",
  "SZ",
  "TC",
  "TD",
  "TF",
  "TG",
  "TH",
  "TJ",
  "TK",
  "TL",
  "TM",
  "TN",
  "TO",
  "TR",
  "TT",
  "TV",
  "TW",
  "TZ",
  "UA",
  "UG",
  "UM",
  "US",
  "UY",
  "UZ",
  "VA",
  "VC",
  "VE",
  "VG",
  "VI",
  "VN",
  "VU",
  "WF",
  "WS",
  "YE",
  "YT",
  "ZA",
  "ZM",
  "ZW"
] as const;

function useCountryOptions(locale: string) {
  return useMemo(() => {
    const displayNames = new Intl.DisplayNames([locale], { type: "region" });
    return countryCodes
      .map((code) => ({ code, name: displayNames.of(code) ?? code }))
      .sort((a, b) => a.name.localeCompare(b.name, locale));
  }, [locale]);
}

type CountryOption = { code: string; name: string };

function CountrySelect({
  countries,
  defaultValue,
  onValueChange
}: Readonly<{ countries: CountryOption[]; defaultValue: string | undefined; onValueChange: () => void }>) {
  const formErrors = useContext(FormValidationContext);
  const countryErrors = formErrors.country;
  const errors = countryErrors
    ? Array.isArray(countryErrors)
      ? countryErrors.map((err) => ({ message: err }))
      : [{ message: countryErrors }]
    : undefined;

  return (
    <Field>
      <FieldLabel>{t`Country`}</FieldLabel>
      <Select name="country" defaultValue={defaultValue} onValueChange={onValueChange}>
        <SelectTrigger className="w-full" aria-label={t`Country`}>
          <SelectValue>
            {(value: string | null) => {
              if (!value) {
                return <span className="text-muted-foreground">{t`Select country`}</span>;
              }
              const country = countries.find((c) => c.code === value);
              return country?.name ?? value;
            }}
          </SelectValue>
        </SelectTrigger>
        <SelectContent>
          {countries.map((country) => (
            <SelectItem key={country.code} value={country.code}>
              {country.name}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      <FieldError errors={errors} />
    </Field>
  );
}

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
  const userInfo = useUserInfo();
  const queryClient = useQueryClient();
  const { i18n } = useLingui();
  const countries = useCountryOptions(i18n.locale);

  const mutation = api.useMutation("put", "/api/account/subscriptions/billing-info", {
    onSuccess: async () => {
      setIsFormDirty(false);
      await queryClient.invalidateQueries({ queryKey: ["get", "/api/account/subscriptions/current"] });
      toast.success(t`Billing information updated`);
      onOpenChange(false);
      onSuccess?.();
    }
  });

  const handleCloseComplete = () => {
    setIsFormDirty(false);
  };

  const markDirty = () => setIsFormDirty(true);
  const isNewBillingInfo = !billingInfo?.address;

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
            <div className="flex flex-col gap-4">
              <TextField
                autoFocus={true}
                name="name"
                label={t`Name`}
                defaultValue={billingInfo?.name ?? tenantName}
                placeholder={t`Name as it appears on invoices`}
                onChange={markDirty}
              />
              <TextField
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
                  placeholder={t`E.g., 1234`}
                  onChange={markDirty}
                />
                <TextField
                  name="city"
                  label={t`City`}
                  defaultValue={billingInfo?.address?.city ?? ""}
                  placeholder={t`E.g., Copenhagen`}
                  onChange={markDirty}
                />
              </div>
              <TextField
                name="state"
                label={t`State / Province`}
                defaultValue={billingInfo?.address?.state ?? ""}
                placeholder={t`E.g., Capital Region`}
                onChange={markDirty}
              />
              <CountrySelect
                countries={countries}
                defaultValue={billingInfo?.address?.country ?? undefined}
                onValueChange={markDirty}
              />
              <TextField
                name="email"
                label={t`Email`}
                defaultValue={billingInfo?.email ?? userInfo?.email ?? ""}
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
              {mutation.isPending ? (pendingLabel ?? t`Saving...`) : (submitLabel ?? t`Save`)}
            </Button>
          </DialogFooter>
        </Form>
      </DialogContent>
    </DirtyDialog>
  );
}

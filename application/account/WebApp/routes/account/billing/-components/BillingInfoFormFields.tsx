import { t } from "@lingui/core/macro";
import { TextAreaField } from "@repo/ui/components/TextAreaField";
import { TextField } from "@repo/ui/components/TextField";

import type { components } from "@/shared/lib/api/api.generated";

import type { CountryOption } from "./CountrySelect";

import { stateRequiredCountries } from "./countryCodes";
import { CountrySelect } from "./CountrySelect";

type BillingInfo = components["schemas"]["BillingInfo"];

interface BillingInfoFormFieldsProps {
  billingInfo: BillingInfo | null | undefined;
  tenantName: string;
  defaultEmail: string;
  countries: CountryOption[];
  selectedCountry: string | undefined;
  onCountryChange: (value: string | null) => void;
  onFieldChange: () => void;
}

export function BillingInfoFormFields({
  billingInfo,
  tenantName,
  defaultEmail,
  countries,
  selectedCountry,
  onCountryChange,
  onFieldChange
}: Readonly<BillingInfoFormFieldsProps>) {
  const showStateField = selectedCountry != null && stateRequiredCountries.includes(selectedCountry);

  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
      <TextField
        autoFocus={true}
        name="email"
        label={t`Billing email`}
        defaultValue={defaultEmail}
        placeholder={t`billing@company.com`}
        onChange={onFieldChange}
      />
      <CountrySelect
        countries={countries}
        defaultValue={billingInfo?.address?.country ?? undefined}
        onValueChange={onCountryChange}
      />
      <TextField
        name="name"
        label={t`Name`}
        defaultValue={billingInfo?.name ?? tenantName}
        placeholder={t`Name as it appears on invoices`}
        className="sm:col-span-2"
        onChange={onFieldChange}
      />
      <TextAreaField
        name="address"
        label={t`Address`}
        defaultValue={[billingInfo?.address?.line1, billingInfo?.address?.line2].filter(Boolean).join("\n")}
        placeholder={t`Street address`}
        textareaClassName="resize-none"
        className="sm:col-span-2"
        onChange={onFieldChange}
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
            onFieldChange();
          }
        }}
      />
      <div className="grid grid-cols-3 gap-4 sm:col-span-2 sm:grid-cols-2">
        <TextField
          name="postalCode"
          label={t`Postal code`}
          defaultValue={billingInfo?.address?.postalCode ?? ""}
          placeholder={t`Postal code`}
          onChange={onFieldChange}
        />
        <TextField
          name="city"
          label={t`City`}
          defaultValue={billingInfo?.address?.city ?? ""}
          placeholder={t`City`}
          className="col-span-2 sm:col-span-1"
          onChange={onFieldChange}
        />
      </div>
      {showStateField && (
        <TextField
          name="state"
          label={t`State / Province`}
          defaultValue={billingInfo?.address?.state ?? ""}
          placeholder={t`State or region`}
          onChange={onFieldChange}
        />
      )}
      <TextField
        name="taxId"
        label={t`Tax ID (VAT number)`}
        defaultValue={billingInfo?.taxId ?? ""}
        placeholder={t`VAT number`}
        description={t`Please include your country code`}
        className={!showStateField ? "sm:col-span-2" : undefined}
        onChange={onFieldChange}
      />
    </div>
  );
}

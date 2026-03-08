import { t } from "@lingui/core/macro";
import { Field, FieldError, FieldLabel } from "@repo/ui/components/Field";
import { FormValidationContext } from "@repo/ui/components/Form";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@repo/ui/components/Select";
import { useContext, useMemo } from "react";

import { countryCodes } from "./countryCodes";

export type CountryOption = { code: string; name: string };

export function useCountryOptions(locale: string) {
  return useMemo(() => {
    const displayNames = new Intl.DisplayNames([locale], { type: "region" });
    return countryCodes
      .map((code) => ({ code, name: displayNames.of(code) ?? code }))
      .sort((a, b) => a.name.localeCompare(b.name, locale));
  }, [locale]);
}

export function CountrySelect({
  countries,
  defaultValue,
  onValueChange
}: Readonly<{
  countries: CountryOption[];
  defaultValue: string | undefined;
  onValueChange: (value: string | null) => void;
}>) {
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
      <Select name="country" defaultValue={defaultValue} onValueChange={(value) => onValueChange(value)}>
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

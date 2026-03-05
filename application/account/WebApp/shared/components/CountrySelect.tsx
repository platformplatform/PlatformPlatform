import { t } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { Field, FieldError, FieldLabel } from "@repo/ui/components/Field";
import { FormValidationContext } from "@repo/ui/components/Form";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@repo/ui/components/Select";
import { useContext, useMemo } from "react";

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

type CountryOption = { code: string; name: string };

export function useCountryOptions() {
  const { i18n } = useLingui();
  return useMemo(() => {
    const displayNames = new Intl.DisplayNames([i18n.locale], { type: "region" });
    return countryCodes
      .map((code) => ({ code, name: displayNames.of(code) ?? code }))
      .sort((a, b) => a.name.localeCompare(b.name, i18n.locale));
  }, [i18n.locale]);
}

export function useCountryName(code: string | null | undefined) {
  const { i18n } = useLingui();
  return useMemo(() => {
    if (!code) {
      return null;
    }
    const displayNames = new Intl.DisplayNames([i18n.locale], { type: "region" });
    return displayNames.of(code) ?? code;
  }, [code, i18n.locale]);
}

interface CountrySelectProps {
  countries: CountryOption[];
  defaultValue?: string | undefined;
  value?: string | null;
  onValueChange: (value: string | null) => void;
  disabled?: boolean;
  className?: string;
}

export function CountrySelect({
  countries,
  defaultValue,
  value,
  onValueChange,
  disabled,
  className
}: Readonly<CountrySelectProps>) {
  const formErrors = useContext(FormValidationContext);
  const countryErrors = formErrors.country;
  const errors = countryErrors
    ? Array.isArray(countryErrors)
      ? countryErrors.map((err) => ({ message: err }))
      : [{ message: countryErrors }]
    : undefined;

  return (
    <Field className={className}>
      <FieldLabel>{t`Country`}</FieldLabel>
      <Select
        name="country"
        defaultValue={defaultValue}
        value={value}
        onValueChange={(value) => onValueChange(value)}
        disabled={disabled}
      >
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

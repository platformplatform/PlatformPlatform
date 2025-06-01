import { t } from "@lingui/core/macro";
import type { RefAttributes } from "react";
import { Collection, Header, Section } from "react-aria-components";
import { twMerge } from "tailwind-merge";
import { Select, SelectItem } from "./Select";
import { TextField } from "./TextField";

export interface AddressFormValue {
  street?: string;
  street2?: string;
  city?: string;
  state?: string;
  zip?: string;
  country?: string;
}

export interface AddressFormProps extends RefAttributes<HTMLDivElement> {
  defaultValue?: AddressFormValue;
  isRequired?: boolean;
  isDisabled?: boolean;
  className?: string;
}

interface Country {
  code: string;
  name: string;
}

interface Continent {
  name: string;
  countries: Country[];
}

const COUNTRIES_BY_CONTINENT: Continent[] = [
  {
    name: "Africa",
    countries: [
      { code: "DZ", name: "Algeria" },
      { code: "AO", name: "Angola" },
      { code: "BJ", name: "Benin" },
      { code: "BW", name: "Botswana" },
      { code: "BF", name: "Burkina Faso" },
      { code: "BI", name: "Burundi" },
      { code: "CV", name: "Cabo Verde" },
      { code: "CM", name: "Cameroon" },
      { code: "CF", name: "Central African Republic" },
      { code: "TD", name: "Chad" },
      { code: "KM", name: "Comoros" },
      { code: "CG", name: "Congo" },
      { code: "CD", name: "Congo (Democratic Republic)" },
      { code: "CI", name: "Côte d'Ivoire" },
      { code: "DJ", name: "Djibouti" },
      { code: "EG", name: "Egypt" },
      { code: "GQ", name: "Equatorial Guinea" },
      { code: "ER", name: "Eritrea" },
      { code: "SZ", name: "Eswatini" },
      { code: "ET", name: "Ethiopia" },
      { code: "GA", name: "Gabon" },
      { code: "GM", name: "Gambia" },
      { code: "GH", name: "Ghana" },
      { code: "GN", name: "Guinea" },
      { code: "GW", name: "Guinea-Bissau" },
      { code: "KE", name: "Kenya" },
      { code: "LS", name: "Lesotho" },
      { code: "LR", name: "Liberia" },
      { code: "LY", name: "Libya" },
      { code: "MG", name: "Madagascar" },
      { code: "MW", name: "Malawi" },
      { code: "ML", name: "Mali" },
      { code: "MR", name: "Mauritania" },
      { code: "MU", name: "Mauritius" },
      { code: "MA", name: "Morocco" },
      { code: "MZ", name: "Mozambique" },
      { code: "NA", name: "Namibia" },
      { code: "NE", name: "Niger" },
      { code: "NG", name: "Nigeria" },
      { code: "RW", name: "Rwanda" },
      { code: "ST", name: "São Tomé and Príncipe" },
      { code: "SN", name: "Senegal" },
      { code: "SC", name: "Seychelles" },
      { code: "SL", name: "Sierra Leone" },
      { code: "SO", name: "Somalia" },
      { code: "ZA", name: "South Africa" },
      { code: "SS", name: "South Sudan" },
      { code: "SD", name: "Sudan" },
      { code: "TZ", name: "Tanzania" },
      { code: "TG", name: "Togo" },
      { code: "TN", name: "Tunisia" },
      { code: "UG", name: "Uganda" },
      { code: "ZM", name: "Zambia" },
      { code: "ZW", name: "Zimbabwe" }
    ]
  },
  {
    name: "Asia",
    countries: [
      { code: "AF", name: "Afghanistan" },
      { code: "AM", name: "Armenia" },
      { code: "AZ", name: "Azerbaijan" },
      { code: "BH", name: "Bahrain" },
      { code: "BD", name: "Bangladesh" },
      { code: "BT", name: "Bhutan" },
      { code: "BN", name: "Brunei" },
      { code: "KH", name: "Cambodia" },
      { code: "CN", name: "China" },
      { code: "CY", name: "Cyprus" },
      { code: "GE", name: "Georgia" },
      { code: "IN", name: "India" },
      { code: "ID", name: "Indonesia" },
      { code: "IR", name: "Iran" },
      { code: "IQ", name: "Iraq" },
      { code: "IL", name: "Israel" },
      { code: "JP", name: "Japan" },
      { code: "JO", name: "Jordan" },
      { code: "KZ", name: "Kazakhstan" },
      { code: "KW", name: "Kuwait" },
      { code: "KG", name: "Kyrgyzstan" },
      { code: "LA", name: "Laos" },
      { code: "LB", name: "Lebanon" },
      { code: "MY", name: "Malaysia" },
      { code: "MV", name: "Maldives" },
      { code: "MN", name: "Mongolia" },
      { code: "MM", name: "Myanmar" },
      { code: "NP", name: "Nepal" },
      { code: "KP", name: "North Korea" },
      { code: "OM", name: "Oman" },
      { code: "PK", name: "Pakistan" },
      { code: "PS", name: "Palestine" },
      { code: "PH", name: "Philippines" },
      { code: "QA", name: "Qatar" },
      { code: "SA", name: "Saudi Arabia" },
      { code: "SG", name: "Singapore" },
      { code: "KR", name: "South Korea" },
      { code: "LK", name: "Sri Lanka" },
      { code: "SY", name: "Syria" },
      { code: "TW", name: "Taiwan" },
      { code: "TJ", name: "Tajikistan" },
      { code: "TH", name: "Thailand" },
      { code: "TL", name: "Timor-Leste" },
      { code: "TR", name: "Turkey" },
      { code: "TM", name: "Turkmenistan" },
      { code: "AE", name: "United Arab Emirates" },
      { code: "UZ", name: "Uzbekistan" },
      { code: "VN", name: "Vietnam" },
      { code: "YE", name: "Yemen" }
    ]
  },
  {
    name: "Europe",
    countries: [
      { code: "AL", name: "Albania" },
      { code: "AD", name: "Andorra" },
      { code: "AT", name: "Austria" },
      { code: "BY", name: "Belarus" },
      { code: "BE", name: "Belgium" },
      { code: "BA", name: "Bosnia and Herzegovina" },
      { code: "BG", name: "Bulgaria" },
      { code: "HR", name: "Croatia" },
      { code: "CZ", name: "Czech Republic" },
      { code: "DK", name: "Denmark" },
      { code: "EE", name: "Estonia" },
      { code: "FI", name: "Finland" },
      { code: "FR", name: "France" },
      { code: "DE", name: "Germany" },
      { code: "GR", name: "Greece" },
      { code: "HU", name: "Hungary" },
      { code: "IS", name: "Iceland" },
      { code: "IE", name: "Ireland" },
      { code: "IT", name: "Italy" },
      { code: "XK", name: "Kosovo" },
      { code: "LV", name: "Latvia" },
      { code: "LI", name: "Liechtenstein" },
      { code: "LT", name: "Lithuania" },
      { code: "LU", name: "Luxembourg" },
      { code: "MT", name: "Malta" },
      { code: "MD", name: "Moldova" },
      { code: "MC", name: "Monaco" },
      { code: "ME", name: "Montenegro" },
      { code: "NL", name: "Netherlands" },
      { code: "MK", name: "North Macedonia" },
      { code: "NO", name: "Norway" },
      { code: "PL", name: "Poland" },
      { code: "PT", name: "Portugal" },
      { code: "RO", name: "Romania" },
      { code: "RU", name: "Russia" },
      { code: "SM", name: "San Marino" },
      { code: "RS", name: "Serbia" },
      { code: "SK", name: "Slovakia" },
      { code: "SI", name: "Slovenia" },
      { code: "ES", name: "Spain" },
      { code: "SE", name: "Sweden" },
      { code: "CH", name: "Switzerland" },
      { code: "UA", name: "Ukraine" },
      { code: "GB", name: "United Kingdom" },
      { code: "VA", name: "Vatican City" }
    ]
  },
  {
    name: "North America",
    countries: [
      { code: "AG", name: "Antigua and Barbuda" },
      { code: "BS", name: "Bahamas" },
      { code: "BB", name: "Barbados" },
      { code: "BZ", name: "Belize" },
      { code: "CA", name: "Canada" },
      { code: "CR", name: "Costa Rica" },
      { code: "CU", name: "Cuba" },
      { code: "DM", name: "Dominica" },
      { code: "DO", name: "Dominican Republic" },
      { code: "SV", name: "El Salvador" },
      { code: "GD", name: "Grenada" },
      { code: "GT", name: "Guatemala" },
      { code: "HT", name: "Haiti" },
      { code: "HN", name: "Honduras" },
      { code: "JM", name: "Jamaica" },
      { code: "MX", name: "Mexico" },
      { code: "NI", name: "Nicaragua" },
      { code: "PA", name: "Panama" },
      { code: "KN", name: "Saint Kitts and Nevis" },
      { code: "LC", name: "Saint Lucia" },
      { code: "VC", name: "Saint Vincent and the Grenadines" },
      { code: "TT", name: "Trinidad and Tobago" },
      { code: "US", name: "United States" }
    ]
  },
  {
    name: "South America",
    countries: [
      { code: "AR", name: "Argentina" },
      { code: "BO", name: "Bolivia" },
      { code: "BR", name: "Brazil" },
      { code: "CL", name: "Chile" },
      { code: "CO", name: "Colombia" },
      { code: "EC", name: "Ecuador" },
      { code: "GY", name: "Guyana" },
      { code: "PY", name: "Paraguay" },
      { code: "PE", name: "Peru" },
      { code: "SR", name: "Suriname" },
      { code: "UY", name: "Uruguay" },
      { code: "VE", name: "Venezuela" }
    ]
  },
  {
    name: "Oceania",
    countries: [
      { code: "AU", name: "Australia" },
      { code: "FJ", name: "Fiji" },
      { code: "KI", name: "Kiribati" },
      { code: "MH", name: "Marshall Islands" },
      { code: "FM", name: "Micronesia" },
      { code: "NR", name: "Nauru" },
      { code: "NZ", name: "New Zealand" },
      { code: "PW", name: "Palau" },
      { code: "PG", name: "Papua New Guinea" },
      { code: "WS", name: "Samoa" },
      { code: "SB", name: "Solomon Islands" },
      { code: "TO", name: "Tonga" },
      { code: "TV", name: "Tuvalu" },
      { code: "VU", name: "Vanuatu" }
    ]
  },
  {
    name: "Antarctica",
    countries: [{ code: "AQ", name: "Antarctica" }]
  }
];

export function AddressForm({
  defaultValue,
  isRequired = false,
  isDisabled = false,
  className,
  ...props
}: Readonly<AddressFormProps>) {
  return (
    <div {...props} className={twMerge("flex flex-col gap-4", className)}>
      <TextField
        name="street"
        label={t`Street address`}
        defaultValue={defaultValue?.street}
        isRequired={isRequired}
        isDisabled={isDisabled}
        placeholder="123 Main Street"
      />

      <TextField
        name="street2"
        label={t`Street address 2`}
        defaultValue={defaultValue?.street2}
        isDisabled={isDisabled}
        placeholder="Apartment, suite, etc. (optional)"
      />

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
        <TextField
          name="city"
          label={t`City`}
          defaultValue={defaultValue?.city}
          isRequired={isRequired}
          isDisabled={isDisabled}
          placeholder="New York"
        />

        <TextField
          name="state"
          label={t`State / Province`}
          defaultValue={defaultValue?.state}
          isRequired={isRequired}
          isDisabled={isDisabled}
          placeholder="NY"
        />
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
        <TextField
          name="zip"
          label={t`ZIP / Postal code`}
          defaultValue={defaultValue?.zip}
          isRequired={isRequired}
          isDisabled={isDisabled}
          placeholder="10001"
        />

        <Select
          name="country"
          label={t`Country`}
          defaultSelectedKey={defaultValue?.country}
          isRequired={isRequired}
          isDisabled={isDisabled}
        >
          <Collection items={COUNTRIES_BY_CONTINENT}>
            {(continent) => (
              <Section key={continent.name}>
                <Header>{continent.name}</Header>
                <Collection items={continent.countries}>
                  {(country) => (
                    <SelectItem key={country.code} id={country.code}>
                      {country.name}
                    </SelectItem>
                  )}
                </Collection>
              </Section>
            )}
          </Collection>
        </Select>
      </div>
    </div>
  );
}

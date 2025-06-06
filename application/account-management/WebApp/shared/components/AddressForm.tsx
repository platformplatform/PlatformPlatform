import { t } from "@lingui/core/macro";
import { Select, SelectItem } from "@repo/ui/components/Select";
import { TextField } from "@repo/ui/components/TextField";
import { Header, Section } from "react-aria-components";
import { AddressAutocomplete } from "./AddressAutocomplete";

export interface AddressData {
  street: string;
  street2: string;
  city: string;
  state: string;
  zip: string;
  country: string;
}

export interface AddressFormProps {
  address: AddressData;
  onAddressChange: (address: AddressData) => void;
  onAddressSelect?: (address: AddressData) => void;
  isDisabled?: boolean;
}

interface Country {
  code: string;
  name: string;
}

interface Continent {
  name: string;
  countries: Country[];
}

export function AddressForm({ address, onAddressChange, onAddressSelect, isDisabled = false }: AddressFormProps) {
  const handleAddressChange = (field: keyof AddressData, value: string) => {
    onAddressChange({
      ...address,
      [field]: value
    });
  };

  const handleAddressSelect = (selectedAddress: AddressData) => {
    onAddressSelect?.(selectedAddress);
  };

  const continents = [
    {
      name: t`Europe`,
      countries: [
        { code: "AD", name: t`Andorra` },
        { code: "AL", name: t`Albania` },
        { code: "AT", name: t`Austria` },
        { code: "BA", name: t`Bosnia and Herzegovina` },
        { code: "BE", name: t`Belgium` },
        { code: "BG", name: t`Bulgaria` },
        { code: "BY", name: t`Belarus` },
        { code: "CH", name: t`Switzerland` },
        { code: "CY", name: t`Cyprus` },
        { code: "CZ", name: t`Czech Republic` },
        { code: "DE", name: t`Germany` },
        { code: "DK", name: t`Denmark` },
        { code: "EE", name: t`Estonia` },
        { code: "ES", name: t`Spain` },
        { code: "FI", name: t`Finland` },
        { code: "FR", name: t`France` },
        { code: "GB", name: t`United Kingdom` },
        { code: "GR", name: t`Greece` },
        { code: "HR", name: t`Croatia` },
        { code: "HU", name: t`Hungary` },
        { code: "IE", name: t`Ireland` },
        { code: "IS", name: t`Iceland` },
        { code: "IT", name: t`Italy` },
        { code: "LI", name: t`Liechtenstein` },
        { code: "LT", name: t`Lithuania` },
        { code: "LU", name: t`Luxembourg` },
        { code: "LV", name: t`Latvia` },
        { code: "MC", name: t`Monaco` },
        { code: "MD", name: t`Moldova` },
        { code: "ME", name: t`Montenegro` },
        { code: "MK", name: t`North Macedonia` },
        { code: "MT", name: t`Malta` },
        { code: "NL", name: t`Netherlands` },
        { code: "NO", name: t`Norway` },
        { code: "PL", name: t`Poland` },
        { code: "PT", name: t`Portugal` },
        { code: "RO", name: t`Romania` },
        { code: "RS", name: t`Serbia` },
        { code: "RU", name: t`Russia` },
        { code: "SE", name: t`Sweden` },
        { code: "SI", name: t`Slovenia` },
        { code: "SK", name: t`Slovakia` },
        { code: "SM", name: t`San Marino` },
        { code: "UA", name: t`Ukraine` },
        { code: "VA", name: t`Vatican City` }
      ]
    },
    {
      name: t`North America`,
      countries: [
        { code: "AG", name: t`Antigua and Barbuda` },
        { code: "BS", name: t`Bahamas` },
        { code: "BB", name: t`Barbados` },
        { code: "BZ", name: t`Belize` },
        { code: "CA", name: t`Canada` },
        { code: "CR", name: t`Costa Rica` },
        { code: "CU", name: t`Cuba` },
        { code: "DM", name: t`Dominica` },
        { code: "DO", name: t`Dominican Republic` },
        { code: "SV", name: t`El Salvador` },
        { code: "GD", name: t`Grenada` },
        { code: "GT", name: t`Guatemala` },
        { code: "HT", name: t`Haiti` },
        { code: "HN", name: t`Honduras` },
        { code: "JM", name: t`Jamaica` },
        { code: "MX", name: t`Mexico` },
        { code: "NI", name: t`Nicaragua` },
        { code: "PA", name: t`Panama` },
        { code: "KN", name: t`Saint Kitts and Nevis` },
        { code: "LC", name: t`Saint Lucia` },
        { code: "VC", name: t`Saint Vincent and the Grenadines` },
        { code: "TT", name: t`Trinidad and Tobago` },
        { code: "US", name: t`United States` }
      ]
    },
    {
      name: t`South America`,
      countries: [
        { code: "AR", name: t`Argentina` },
        { code: "BO", name: t`Bolivia` },
        { code: "BR", name: t`Brazil` },
        { code: "CL", name: t`Chile` },
        { code: "CO", name: t`Colombia` },
        { code: "EC", name: t`Ecuador` },
        { code: "FK", name: t`Falkland Islands` },
        { code: "GF", name: t`French Guiana` },
        { code: "GY", name: t`Guyana` },
        { code: "PY", name: t`Paraguay` },
        { code: "PE", name: t`Peru` },
        { code: "SR", name: t`Suriname` },
        { code: "UY", name: t`Uruguay` },
        { code: "VE", name: t`Venezuela` }
      ]
    },
    {
      name: t`Asia`,
      countries: [
        { code: "AF", name: t`Afghanistan` },
        { code: "AM", name: t`Armenia` },
        { code: "AZ", name: t`Azerbaijan` },
        { code: "BH", name: t`Bahrain` },
        { code: "BD", name: t`Bangladesh` },
        { code: "BT", name: t`Bhutan` },
        { code: "BN", name: t`Brunei` },
        { code: "KH", name: t`Cambodia` },
        { code: "CN", name: t`China` },
        { code: "GE", name: t`Georgia` },
        { code: "HK", name: t`Hong Kong` },
        { code: "IN", name: t`India` },
        { code: "ID", name: t`Indonesia` },
        { code: "IR", name: t`Iran` },
        { code: "IQ", name: t`Iraq` },
        { code: "IL", name: t`Israel` },
        { code: "JP", name: t`Japan` },
        { code: "JO", name: t`Jordan` },
        { code: "KZ", name: t`Kazakhstan` },
        { code: "KW", name: t`Kuwait` },
        { code: "KG", name: t`Kyrgyzstan` },
        { code: "LA", name: t`Laos` },
        { code: "LB", name: t`Lebanon` },
        { code: "MO", name: t`Macao` },
        { code: "MY", name: t`Malaysia` },
        { code: "MV", name: t`Maldives` },
        { code: "MN", name: t`Mongolia` },
        { code: "MM", name: t`Myanmar` },
        { code: "NP", name: t`Nepal` },
        { code: "KP", name: t`North Korea` },
        { code: "OM", name: t`Oman` },
        { code: "PK", name: t`Pakistan` },
        { code: "PS", name: t`Palestine` },
        { code: "PH", name: t`Philippines` },
        { code: "QA", name: t`Qatar` },
        { code: "SA", name: t`Saudi Arabia` },
        { code: "SG", name: t`Singapore` },
        { code: "KR", name: t`South Korea` },
        { code: "LK", name: t`Sri Lanka` },
        { code: "SY", name: t`Syria` },
        { code: "TW", name: t`Taiwan` },
        { code: "TJ", name: t`Tajikistan` },
        { code: "TH", name: t`Thailand` },
        { code: "TL", name: t`Timor-Leste` },
        { code: "TR", name: t`Turkey` },
        { code: "TM", name: t`Turkmenistan` },
        { code: "AE", name: t`United Arab Emirates` },
        { code: "UZ", name: t`Uzbekistan` },
        { code: "VN", name: t`Vietnam` },
        { code: "YE", name: t`Yemen` }
      ]
    },
    {
      name: t`Africa`,
      countries: [
        { code: "DZ", name: t`Algeria` },
        { code: "AO", name: t`Angola` },
        { code: "BJ", name: t`Benin` },
        { code: "BW", name: t`Botswana` },
        { code: "BF", name: t`Burkina Faso` },
        { code: "BI", name: t`Burundi` },
        { code: "CV", name: t`Cape Verde` },
        { code: "CM", name: t`Cameroon` },
        { code: "CF", name: t`Central African Republic` },
        { code: "TD", name: t`Chad` },
        { code: "KM", name: t`Comoros` },
        { code: "CG", name: t`Congo` },
        { code: "CD", name: t`Democratic Republic of the Congo` },
        { code: "DJ", name: t`Djibouti` },
        { code: "EG", name: t`Egypt` },
        { code: "GQ", name: t`Equatorial Guinea` },
        { code: "ER", name: t`Eritrea` },
        { code: "SZ", name: t`Eswatini` },
        { code: "ET", name: t`Ethiopia` },
        { code: "GA", name: t`Gabon` },
        { code: "GM", name: t`Gambia` },
        { code: "GH", name: t`Ghana` },
        { code: "GN", name: t`Guinea` },
        { code: "GW", name: t`Guinea-Bissau` },
        { code: "CI", name: t`Ivory Coast` },
        { code: "KE", name: t`Kenya` },
        { code: "LS", name: t`Lesotho` },
        { code: "LR", name: t`Liberia` },
        { code: "LY", name: t`Libya` },
        { code: "MG", name: t`Madagascar` },
        { code: "MW", name: t`Malawi` },
        { code: "ML", name: t`Mali` },
        { code: "MR", name: t`Mauritania` },
        { code: "MU", name: t`Mauritius` },
        { code: "MA", name: t`Morocco` },
        { code: "MZ", name: t`Mozambique` },
        { code: "NA", name: t`Namibia` },
        { code: "NE", name: t`Niger` },
        { code: "NG", name: t`Nigeria` },
        { code: "RW", name: t`Rwanda` },
        { code: "ST", name: t`São Tomé and Príncipe` },
        { code: "SN", name: t`Senegal` },
        { code: "SC", name: t`Seychelles` },
        { code: "SL", name: t`Sierra Leone` },
        { code: "SO", name: t`Somalia` },
        { code: "ZA", name: t`South Africa` },
        { code: "SS", name: t`South Sudan` },
        { code: "SD", name: t`Sudan` },
        { code: "TZ", name: t`Tanzania` },
        { code: "TG", name: t`Togo` },
        { code: "TN", name: t`Tunisia` },
        { code: "UG", name: t`Uganda` },
        { code: "ZM", name: t`Zambia` },
        { code: "ZW", name: t`Zimbabwe` }
      ]
    },
    {
      name: t`Oceania`,
      countries: [
        { code: "AU", name: t`Australia` },
        { code: "FJ", name: t`Fiji` },
        { code: "KI", name: t`Kiribati` },
        { code: "MH", name: t`Marshall Islands` },
        { code: "FM", name: t`Micronesia` },
        { code: "NR", name: t`Nauru` },
        { code: "NZ", name: t`New Zealand` },
        { code: "PW", name: t`Palau` },
        { code: "PG", name: t`Papua New Guinea` },
        { code: "WS", name: t`Samoa` },
        { code: "SB", name: t`Solomon Islands` },
        { code: "TO", name: t`Tonga` },
        { code: "TV", name: t`Tuvalu` },
        { code: "VU", name: t`Vanuatu` }
      ]
    }
  ];

  return (
    <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
      {/* Street address with autocomplete */}
      <div className="md:col-span-2">
        <AddressAutocomplete
          value={address.street}
          onChange={(value) => handleAddressChange("street", value)}
          onAddressSelect={handleAddressSelect}
          isDisabled={isDisabled}
          label={t`Street address`}
          name="street"
          countryCode={address.country}
        />
      </div>

      <div className="md:col-span-2">
        <TextField
          label={t`Street address 2 (optional)`}
          name="street2"
          value={address.street2}
          onChange={(value) => handleAddressChange("street2", value)}
          isDisabled={isDisabled}
          placeholder={t`Apartment, suite, etc.`}
        />
      </div>

      <TextField
        label={t`ZIP/Postal code`}
        name="zip"
        value={address.zip}
        onChange={(value) => handleAddressChange("zip", value)}
        isDisabled={isDisabled}
        placeholder={t`Enter ZIP or postal code`}
      />

      <TextField
        label={t`City`}
        name="city"
        value={address.city}
        onChange={(value) => handleAddressChange("city", value)}
        isDisabled={isDisabled}
        placeholder={t`Enter city`}
      />

      <TextField
        label={t`State/Province`}
        name="state"
        value={address.state}
        onChange={(value) => handleAddressChange("state", value)}
        isDisabled={isDisabled}
        placeholder={t`Enter state or province`}
      />

      <Select
        label={t`Country`}
        name="country"
        selectedKey={address.country}
        onSelectionChange={(value) => handleAddressChange("country", value as string)}
        isDisabled={isDisabled}
        placeholder={t`Select country`}
      >
        {continents.map((continent: Continent) => (
          <Section key={continent.name}>
            <Header className="sticky top-0 z-10 border-b bg-background px-2 py-1 font-semibold text-muted-foreground text-xs">
              {continent.name}
            </Header>
            {continent.countries.map((country: Country) => (
              <SelectItem key={country.code} id={country.code}>
                {country.name}
              </SelectItem>
            ))}
          </Section>
        ))}
      </Select>
    </div>
  );
}

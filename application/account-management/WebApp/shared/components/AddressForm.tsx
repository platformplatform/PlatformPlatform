import { t } from "@lingui/core/macro";
import { Select, SelectItem } from "@repo/ui/components/Select";
import { TextField } from "@repo/ui/components/TextField";
import { Collection, Header, Section } from "react-aria-components";

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
  isDisabled?: boolean;
}

const continents = [
  {
    name: t`Europe`,
    countries: [
      { code: "AD", name: t`Andorra` },
      { code: "AT", name: t`Austria` },
      { code: "BE", name: t`Belgium` },
      { code: "BG", name: t`Bulgaria` },
      { code: "HR", name: t`Croatia` },
      { code: "CY", name: t`Cyprus` },
      { code: "CZ", name: t`Czech Republic` },
      { code: "DK", name: t`Denmark` },
      { code: "EE", name: t`Estonia` },
      { code: "FI", name: t`Finland` },
      { code: "FR", name: t`France` },
      { code: "DE", name: t`Germany` },
      { code: "GR", name: t`Greece` },
      { code: "HU", name: t`Hungary` },
      { code: "IS", name: t`Iceland` },
      { code: "IE", name: t`Ireland` },
      { code: "IT", name: t`Italy` },
      { code: "LV", name: t`Latvia` },
      { code: "LI", name: t`Liechtenstein` },
      { code: "LT", name: t`Lithuania` },
      { code: "LU", name: t`Luxembourg` },
      { code: "MT", name: t`Malta` },
      { code: "MC", name: t`Monaco` },
      { code: "NL", name: t`Netherlands` },
      { code: "NO", name: t`Norway` },
      { code: "PL", name: t`Poland` },
      { code: "PT", name: t`Portugal` },
      { code: "RO", name: t`Romania` },
      { code: "SM", name: t`San Marino` },
      { code: "SK", name: t`Slovakia` },
      { code: "SI", name: t`Slovenia` },
      { code: "ES", name: t`Spain` },
      { code: "SE", name: t`Sweden` },
      { code: "CH", name: t`Switzerland` },
      { code: "GB", name: t`United Kingdom` },
      { code: "VA", name: t`Vatican City` }
    ]
  },
  {
    name: t`North America`,
    countries: [
      { code: "CA", name: t`Canada` },
      { code: "MX", name: t`Mexico` },
      { code: "US", name: t`United States` }
    ]
  },
  {
    name: t`Asia`,
    countries: [
      { code: "CN", name: t`China` },
      { code: "IN", name: t`India` },
      { code: "JP", name: t`Japan` },
      { code: "KR", name: t`South Korea` },
      { code: "SG", name: t`Singapore` },
      { code: "TH", name: t`Thailand` },
      { code: "VN", name: t`Vietnam` }
    ]
  },
  {
    name: t`Oceania`,
    countries: [
      { code: "AU", name: t`Australia` },
      { code: "NZ", name: t`New Zealand` }
    ]
  },
  {
    name: t`South America`,
    countries: [
      { code: "AR", name: t`Argentina` },
      { code: "BR", name: t`Brazil` },
      { code: "CL", name: t`Chile` },
      { code: "CO", name: t`Colombia` },
      { code: "PE", name: t`Peru` }
    ]
  },
  {
    name: t`Africa`,
    countries: [
      { code: "EG", name: t`Egypt` },
      { code: "KE", name: t`Kenya` },
      { code: "MA", name: t`Morocco` },
      { code: "NG", name: t`Nigeria` },
      { code: "ZA", name: t`South Africa` }
    ]
  }
];

export function AddressForm({ address, onAddressChange, isDisabled = false }: AddressFormProps) {
  const handleFieldChange = (field: keyof AddressData) => (value: string) => {
    onAddressChange({
      ...address,
      [field]: value
    });
  };

  const handleCountryChange = (value: string) => {
    onAddressChange({
      ...address,
      country: value
    });
  };

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
        <TextField
          name="street"
          label={t`Street address`}
          value={address.street}
          onChange={handleFieldChange("street")}
          isDisabled={isDisabled}
          className="md:col-span-2"
        />

        <TextField
          name="street2"
          label={t`Street address 2 (optional)`}
          value={address.street2}
          onChange={handleFieldChange("street2")}
          isDisabled={isDisabled}
          className="md:col-span-2"
        />

        <TextField
          name="city"
          label={t`City`}
          value={address.city}
          onChange={handleFieldChange("city")}
          isDisabled={isDisabled}
        />

        <TextField
          name="state"
          label={t`State/Province`}
          value={address.state}
          onChange={handleFieldChange("state")}
          isDisabled={isDisabled}
        />

        <TextField
          name="zip"
          label={t`ZIP/Postal code`}
          value={address.zip}
          onChange={handleFieldChange("zip")}
          isDisabled={isDisabled}
        />

        <Select
          name="country"
          label={t`Country`}
          selectedKey={address.country}
          onSelectionChange={(key) => handleCountryChange(key as string)}
          isDisabled={isDisabled}
        >
          <Collection items={continents}>
            {(continent) => (
              <Section key={continent.name}>
                <Header className="font-semibold px-2 py-1 text-muted-foreground text-xs tracking-wide uppercase">
                  {continent.name}
                </Header>
                <Collection items={continent.countries}>
                  {(country) => (
                    <SelectItem key={country.code} id={country.code} textValue={country.name}>
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

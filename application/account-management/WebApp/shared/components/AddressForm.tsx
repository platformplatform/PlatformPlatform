import { t } from "@lingui/core/macro";
import { TextField } from "@repo/ui/components/TextField";
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
  countryCode?: string;
  isDisabled?: boolean;
}

export function AddressForm({
  address,
  onAddressChange,
  onAddressSelect,
  countryCode,
  isDisabled = false
}: AddressFormProps) {
  // Check if other fields should be enabled
  const isStreetFilled = Boolean(address.street && address.street.trim().length > 0);

  const handleAddressChange = (field: keyof AddressData, value: string) => {
    onAddressChange({
      ...address,
      [field]: value
    });
  };

  const handleAddressSelect = (selectedAddress: AddressData) => {
    onAddressSelect?.(selectedAddress);
  };

  return (
    <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
      {/* Street address with autocomplete */}
      <div className="md:col-span-2">
        <AddressAutocomplete
          value={address.street}
          onChange={(value) => handleAddressChange("street", value)}
          onAddressSelect={handleAddressSelect}
          isDisabled={isDisabled}
          placeholder={countryCode ? t`Start typing an address...` : t`Select a country first to enable address search`}
          label={t`Street address`}
          name="street"
          countryCode={countryCode}
        />
      </div>

      <div className="md:col-span-2">
        <TextField
          label={t`Street address 2 (optional)`}
          name="street2"
          value={address.street2}
          onChange={(value) => handleAddressChange("street2", value)}
          isDisabled={isDisabled || !isStreetFilled}
          placeholder={t`Apartment, suite, etc.`}
        />
      </div>

      <TextField
        label={t`ZIP/Postal code`}
        name="zip"
        value={address.zip}
        onChange={(value) => handleAddressChange("zip", value)}
        isDisabled={isDisabled || !isStreetFilled}
        placeholder={t`Enter ZIP or postal code`}
      />

      <TextField
        label={t`City`}
        name="city"
        value={address.city}
        onChange={(value) => handleAddressChange("city", value)}
        isDisabled={isDisabled || !isStreetFilled}
        placeholder={t`Enter city`}
      />

      <TextField
        label={t`State/Province`}
        name="state"
        value={address.state}
        onChange={(value) => handleAddressChange("state", value)}
        isDisabled={isDisabled || !isStreetFilled}
        placeholder={t`Enter state or province`}
      />
    </div>
  );
}

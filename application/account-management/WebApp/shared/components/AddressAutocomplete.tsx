import { api } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";
import { ComboBox, ComboBoxItem } from "@repo/ui/components/ComboBox";
import { useDebounce } from "@repo/ui/hooks/useDebounce";
import { useState } from "react";
import type { AddressData } from "./AddressForm";

export interface AddressSuggestion {
  formattedAddress: string;
  street: string | null;
  city: string | null;
  state: string | null;
  zip: string | null;
  country: string | null;
}

interface SuggestionItem extends AddressSuggestion {
  id: string;
}

export interface AddressAutocompleteProps {
  onAddressSelect: (address: AddressData) => void;
  isDisabled?: boolean;
  placeholder?: string;
}

export function AddressAutocomplete({ onAddressSelect, isDisabled = false, placeholder }: AddressAutocompleteProps) {
  const [inputValue, setInputValue] = useState("");
  const debouncedQuery = useDebounce(inputValue, 300);

  const { data: response } = api.useQuery(
    "get",
    "/api/account-management/addresses/search",
    {
      params: {
        query: {
          Query: { query: debouncedQuery }
        }
      }
    },
    {
      enabled: Boolean(debouncedQuery && debouncedQuery.length >= 2)
    }
  );

  const suggestions = response?.suggestions || [];

  const handleSelectionChange = (key: string | number | null) => {
    if (key) {
      const selectedSuggestion = suggestions.find((_, index) => index.toString() === key);
      if (selectedSuggestion) {
        onAddressSelect({
          street: selectedSuggestion.street || "",
          street2: "",
          city: selectedSuggestion.city || "",
          state: selectedSuggestion.state || "",
          zip: selectedSuggestion.zip || "",
          country: selectedSuggestion.country || ""
        });
        setInputValue("");
      }
    }
  };

  return (
    <ComboBox
      label={t`Search for address`}
      placeholder={placeholder || t`Start typing an address...`}
      inputValue={inputValue}
      onInputChange={setInputValue}
      onSelectionChange={handleSelectionChange}
      isDisabled={isDisabled}
      items={suggestions.map((suggestion, index) => ({
        id: index.toString(),
        ...suggestion
      }))}
    >
      {(item: SuggestionItem) => (
        <ComboBoxItem key={item.id} textValue={item.formattedAddress}>
          <div className="flex flex-col">
            <div className="font-medium">{item.street || item.formattedAddress}</div>
            <div className="text-muted-foreground text-sm">
              {[item.city, item.zip, item.country].filter(Boolean).join(", ")}
            </div>
          </div>
        </ComboBoxItem>
      )}
    </ComboBox>
  );
}

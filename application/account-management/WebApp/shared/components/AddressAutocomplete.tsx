import { api } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";
import { ListBox, ListBoxItem } from "@repo/ui/components/ListBox";
import { Popover } from "@repo/ui/components/Popover";
import { TextField } from "@repo/ui/components/TextField";
import { Tooltip, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { useDebounce } from "@repo/ui/hooks/useDebounce";
import { AlertTriangle } from "lucide-react";
import { useRef, useState } from "react";
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
  value?: string;
  onChange?: (value: string) => void;
  label?: string;
  name?: string;
  countryCode?: string;
}

export function AddressAutocomplete({
  onAddressSelect,
  isDisabled = false,
  placeholder,
  value = "",
  onChange,
  label,
  name,
  countryCode
}: AddressAutocompleteProps) {
  const [inputValue, setInputValue] = useState(value);
  const [isSelecting, setIsSelecting] = useState(false);
  const [isPopoverOpen, setIsPopoverOpen] = useState(false);
  const [lastSelectedAddress, setLastSelectedAddress] = useState<string>("");
  const debouncedQuery = useDebounce(inputValue, 300);
  const triggerRef = useRef<HTMLInputElement>(null);

  const { data: response } = api.useQuery(
    "get",
    "/api/account-management/addresses/search",
    {
      params: {
        query: {
          // @ts-ignore - OpenAPI contract mismatch: expects { Query: { query: string } } but backend wants { Query: string }
          Query: debouncedQuery || undefined,
          // Include country code in search if available
          ...(countryCode && { CountryCode: countryCode })
        }
      }
    },
    {
      enabled: Boolean(
        debouncedQuery &&
          debouncedQuery.length >= 2 &&
          !isSelecting &&
          debouncedQuery !== lastSelectedAddress &&
          countryCode // Only enable search if country is selected
      )
    }
  );

  // Limit suggestions to 20 items for better UX while still providing good coverage
  const suggestions = (response?.suggestions || []).slice(0, 20);

  // Show popover when we have suggestions and the input is focused
  const shouldShowPopover = suggestions.length > 0 && !isSelecting && inputValue.length >= 2 && countryCode;

  const handleInputChange = (newValue: string) => {
    setInputValue(newValue);
    setIsSelecting(false);
    setIsPopoverOpen(true);
    onChange?.(newValue);
  };

  const handleFocus = () => {
    if (suggestions.length > 0 && inputValue.length >= 2 && countryCode) {
      setIsPopoverOpen(true);
    }
  };

  const handleBlur = (event: React.FocusEvent) => {
    // Only close if focus is moving outside the entire autocomplete component
    const relatedTarget = event.relatedTarget as HTMLElement;
    const popoverElement = document.querySelector("[data-popover]");

    if (!relatedTarget || !popoverElement?.contains(relatedTarget)) {
      // Delay to allow for selection to complete
      setTimeout(() => {
        setIsPopoverOpen(false);
      }, 150);
    }
  };

  const handleKeyDown = (event: React.KeyboardEvent) => {
    if (event.key === "ArrowDown" && suggestions.length > 0) {
      event.preventDefault();
      setIsPopoverOpen(true);
      // Only focus the ListBox when user explicitly presses ArrowDown
      setTimeout(() => {
        const listbox = document.querySelector('[role="listbox"]') as HTMLElement;
        if (listbox) {
          listbox.focus();
          // Focus first option
          const firstOption = listbox.querySelector('[role="option"]') as HTMLElement;
          if (firstOption) {
            firstOption.focus();
          }
        }
      }, 0);
    } else if (event.key === "Escape") {
      setIsPopoverOpen(false);
      triggerRef.current?.focus();
    }
  };

  const handleSelection = (key: string | number) => {
    setIsSelecting(true);
    setIsPopoverOpen(false);

    const selectedSuggestion = suggestions.find((_, index) => index.toString() === key);
    if (selectedSuggestion) {
      // Extract just the street name, not the full formatted address
      const streetName = selectedSuggestion.street || "";
      setInputValue(streetName);
      setLastSelectedAddress(streetName);
      onChange?.(streetName);

      // Map country name to country code
      const getCountryCode = (countryName: string | null): string => {
        if (!countryName) {
          return countryCode || "";
        }

        // Simple mapping for common countries - this could be expanded
        const countryMap: Record<string, string> = {
          Denmark: "DK",
          Germany: "DE",
          "United States": "US",
          "United Kingdom": "GB",
          Canada: "CA",
          Australia: "AU",
          France: "FR",
          Spain: "ES",
          Italy: "IT",
          Netherlands: "NL",
          Sweden: "SE",
          Norway: "NO",
          Finland: "FI"
        };

        return countryMap[countryName] || countryCode || countryName;
      };

      const addressData: AddressData = {
        street: streetName,
        street2: "",
        city: selectedSuggestion.city || "",
        state: selectedSuggestion.state || "",
        zip: selectedSuggestion.zip || "",
        country: getCountryCode(selectedSuggestion.country)
      };

      // Fill out the rest of the form
      onAddressSelect(addressData);

      // Focus the input and position cursor at the end for continued editing
      setTimeout(() => {
        if (triggerRef.current) {
          triggerRef.current.focus();
          triggerRef.current.setSelectionRange(streetName.length, streetName.length);
        }
      }, 100);
    }
  };

  return (
    <div className="relative">
      <TextField
        ref={triggerRef}
        label={label || t`Street address`}
        placeholder={placeholder || t`Enter street address`}
        value={inputValue}
        onChange={handleInputChange}
        onFocus={handleFocus}
        onBlur={handleBlur}
        onKeyDown={handleKeyDown}
        isDisabled={isDisabled || !countryCode}
        name={name || "addressLine1"}
        autoComplete="off"
        aria-expanded={shouldShowPopover && isPopoverOpen}
        aria-haspopup="listbox"
        aria-autocomplete="list"
      />

      {/* Show service warning icon with tooltip */}
      {response && "serviceStatus" in response && response.serviceStatus !== "Available" && (
        <TooltipTrigger delay={0}>
          <button
            type="button"
            className="-mt-[34] absolute right-2 z-50 rounded-sm p-1 hover:bg-accent/50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            aria-label="Service status warning"
          >
            <AlertTriangle className="h-5 w-5 text-amber-500" />
          </button>
          <Tooltip className="z-50 max-w-xs rounded-md border bg-popover px-3 py-2 text-popover-foreground text-sm shadow-md">
            {(response as { serviceMessage?: string }).serviceMessage || t`Address service is currently not working`}
          </Tooltip>
        </TooltipTrigger>
      )}

      {shouldShowPopover && isPopoverOpen && (
        <Popover
          isOpen={true}
          onOpenChange={setIsPopoverOpen}
          triggerRef={triggerRef}
          placement="bottom start"
          className="max-h-[400px] min-w-[300px] max-w-[500px] overflow-hidden"
          data-popover={true}
        >
          <ListBox
            aria-label={t`Address suggestions`}
            selectionMode="single"
            className="max-h-[360px] overflow-y-auto"
            onSelectionChange={(keys) => {
              const key = Array.from(keys)[0];
              if (key !== undefined) {
                handleSelection(key);
              }
            }}
            items={suggestions.map((suggestion, index) => ({
              id: index.toString(),
              ...suggestion
            }))}
            shouldFocusWrap={true}
            disallowEmptySelection={true}
          >
            {(item: SuggestionItem) => (
              <ListBoxItem
                key={item.id}
                textValue={item.formattedAddress}
                className="cursor-pointer px-3 py-2 hover:bg-accent focus:bg-accent focus:outline-none"
              >
                <div className="flex flex-col">
                  <div className="font-medium text-sm">{item.street || item.formattedAddress}</div>
                  <div className="text-muted-foreground text-xs">
                    {[item.city, item.zip, item.country].filter(Boolean).join(", ")}
                  </div>
                </div>
              </ListBoxItem>
            )}
          </ListBox>
        </Popover>
      )}
    </div>
  );
}

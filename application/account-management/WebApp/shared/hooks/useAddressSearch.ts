import type { AddressSuggestion } from "@repo/ui/components/AddressAutocomplete";
import { useCallback } from "react";

interface ApiAddressSuggestion {
  formattedAddress: string;
  street?: string;
  city?: string;
  state?: string;
  zip?: string;
  country?: string;
}

interface SearchAddressesApiResponse {
  suggestions: ApiAddressSuggestion[];
}

export function useAddressSearch() {
  const searchAddresses = useCallback(async (query: string): Promise<AddressSuggestion[]> => {
    if (!query.trim()) {
      return [];
    }

    try {
      // Use the openapi-fetch client directly for this case since we need to call it programmatically
      const response = await fetch(
        `/api/account-management/addresses/search?Query=${encodeURIComponent(query.trim())}`,
        {
          method: "GET",
          headers: {
            "Content-Type": "application/json"
          }
        }
      );

      if (!response.ok) {
        console.error("Failed to search addresses:", response.statusText);
        return [];
      }

      const data: SearchAddressesApiResponse = await response.json();

      if (data?.suggestions) {
        return data.suggestions.map((suggestion: ApiAddressSuggestion) => ({
          formattedAddress: suggestion.formattedAddress || "",
          street: suggestion.street,
          city: suggestion.city,
          state: suggestion.state,
          zip: suggestion.zip,
          country: suggestion.country
        }));
      }

      return [];
    } catch (error) {
      console.error("Failed to search addresses:", error);
      return [];
    }
  }, []);

  return { searchAddresses };
}

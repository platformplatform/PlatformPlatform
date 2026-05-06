import { t } from "@lingui/core/macro";
import { InputGroup, InputGroupAddon, InputGroupButton, InputGroupInput } from "@repo/ui/components/InputGroup";
import { ToggleGroup, ToggleGroupItem } from "@repo/ui/components/ToggleGroup";
import { useDebounce } from "@repo/ui/hooks/useDebounce";
import { useNavigate } from "@tanstack/react-router";
import { SearchIcon, XIcon } from "lucide-react";
import { useEffect, useState } from "react";

import type { SortableBillingEventProperties } from "@/shared/lib/api/client";

import { BillingEventType } from "@/shared/lib/api/client";
import { getBillingEventTypeLabel } from "@/shared/lib/api/labels";

interface BillingEventsToolbarProps {
  search: string | undefined;
  eventTypes: BillingEventType[];
}

// Curated subset of the 17 BillingEventType values surfaced as quick-filter chips. The remaining values
// (BillingInfo*, PaymentMethodUpdated, etc.) are still returned by the API and visible in the table; they
// just aren't first-class quick filters because operators rarely scope to those alone.
const QUICK_FILTER_TYPES = [
  BillingEventType.SubscriptionCreated,
  BillingEventType.SubscriptionUpgraded,
  BillingEventType.SubscriptionDowngraded,
  BillingEventType.SubscriptionCancelled,
  BillingEventType.PaymentFailed
];

export function BillingEventsToolbar({ search, eventTypes }: Readonly<BillingEventsToolbarProps>) {
  const navigate = useNavigate();
  const [searchInput, setSearchInput] = useState(search ?? "");
  const debouncedSearch = useDebounce(searchInput, 500);

  useEffect(() => {
    if ((debouncedSearch || undefined) === search) {
      return;
    }
    navigate({
      to: "/billing-events",
      search: (previous) => ({
        eventTypes: previous.eventTypes,
        orderBy: previous.orderBy as SortableBillingEventProperties | undefined,
        sortOrder: previous.sortOrder,
        search: debouncedSearch || undefined,
        pageOffset: undefined
      })
    });
  }, [debouncedSearch, navigate, search]);

  useEffect(() => {
    setSearchInput(search ?? "");
  }, [search]);

  const handleEventTypesChange = (values: string[]) => {
    const next = values as BillingEventType[];
    navigate({
      to: "/billing-events",
      search: (previous) => ({
        search: previous.search,
        orderBy: previous.orderBy as SortableBillingEventProperties | undefined,
        sortOrder: previous.sortOrder,
        eventTypes: next.length === 0 ? undefined : next,
        pageOffset: undefined
      })
    });
  };

  return (
    <div className="mb-4 flex flex-wrap items-center gap-3">
      <div className="max-w-[20rem] min-w-[14rem] flex-1">
        <InputGroup>
          <InputGroupAddon>
            <SearchIcon />
          </InputGroupAddon>
          <InputGroupInput
            type="text"
            role="searchbox"
            aria-label={t`Search`}
            placeholder={t`Search by account name`}
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
            onKeyDown={(event) => event.key === "Escape" && searchInput && setSearchInput("")}
          />
          {searchInput && (
            <InputGroupAddon align="inline-end">
              <InputGroupButton onClick={() => setSearchInput("")} size="icon-xs" aria-label={t`Clear search`}>
                <XIcon />
              </InputGroupButton>
            </InputGroupAddon>
          )}
        </InputGroup>
      </div>

      <ToggleGroup
        variant="outline"
        aria-label={t`Event type`}
        multiple={true}
        value={eventTypes}
        onValueChange={handleEventTypesChange}
      >
        {QUICK_FILTER_TYPES.map((value) => (
          <ToggleGroupItem key={value} value={value} className="min-w-[5rem] justify-center">
            {getBillingEventTypeLabel(value)}
          </ToggleGroupItem>
        ))}
      </ToggleGroup>
    </div>
  );
}

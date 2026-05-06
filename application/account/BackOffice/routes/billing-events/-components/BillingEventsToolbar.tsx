import { t } from "@lingui/core/macro";
import { InputGroup, InputGroupAddon, InputGroupButton, InputGroupInput } from "@repo/ui/components/InputGroup";
import { MultiSelect } from "@repo/ui/components/MultiSelect";
import { useDebounce } from "@repo/ui/hooks/useDebounce";
import { useNavigate } from "@tanstack/react-router";
import { SearchIcon, XIcon } from "lucide-react";
import { useEffect, useMemo, useState } from "react";

import type { SortableBillingEventProperties } from "@/shared/lib/api/client";

import { BillingEventType } from "@/shared/lib/api/client";
import { getBillingEventTypeLabel } from "@/shared/lib/api/labels";

interface BillingEventsToolbarProps {
  search: string | undefined;
  eventTypes: BillingEventType[];
}

// Order matches the BillingEventType enum so the dropdown reads in the same lifecycle order operators
// see in our domain log (creation → renewal → upgrade → downgrade → cancellation → payment).
const ALL_EVENT_TYPES: BillingEventType[] = [
  BillingEventType.SubscriptionCreated,
  BillingEventType.SubscriptionRenewed,
  BillingEventType.SubscriptionUpgraded,
  BillingEventType.SubscriptionDowngradeScheduled,
  BillingEventType.SubscriptionDowngradeCancelled,
  BillingEventType.SubscriptionDowngraded,
  BillingEventType.SubscriptionCancelled,
  BillingEventType.SubscriptionReactivated,
  BillingEventType.SubscriptionExpired,
  BillingEventType.SubscriptionImmediatelyCancelled,
  BillingEventType.SubscriptionSuspended,
  BillingEventType.PaymentFailed,
  BillingEventType.PaymentRecovered,
  BillingEventType.PaymentRefunded,
  BillingEventType.BillingInfoAdded,
  BillingEventType.BillingInfoUpdated,
  BillingEventType.PaymentMethodUpdated
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

  const eventTypeItems = useMemo(
    () => ALL_EVENT_TYPES.map((value) => ({ id: value, label: getBillingEventTypeLabel(value) })),
    []
  );

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

      <div className="min-w-[12rem]">
        <MultiSelect
          name="event-types"
          placeholder={t`All event types`}
          items={eventTypeItems}
          value={eventTypes}
          onChange={handleEventTypesChange}
        />
      </div>
    </div>
  );
}

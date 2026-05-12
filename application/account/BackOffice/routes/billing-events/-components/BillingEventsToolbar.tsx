import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { InputGroup, InputGroupAddon, InputGroupButton, InputGroupInput } from "@repo/ui/components/InputGroup";
import { ToggleGroup, ToggleGroupItem } from "@repo/ui/components/ToggleGroup";
import { useDebounce } from "@repo/ui/hooks/useDebounce";
import { useNavigate } from "@tanstack/react-router";
import { SearchIcon, XIcon } from "lucide-react";
import { useEffect, useState } from "react";

import type { SortableBillingEventProperties } from "@/shared/lib/api/client";

// The TanStack Router cross-route inference widens `previous.view` to the union of every route's
// view literals (the /invoices toggle's "all" | "invoices" | "refunds" leaks in here). Cast at
// every write boundary so each handler keeps its own narrowed view set.
export type BillingEventsView = "all" | "mrr" | "state" | "other";

interface BillingEventsToolbarProps {
  search: string | undefined;
  view: BillingEventsView;
}

export function BillingEventsToolbar({ search, view }: Readonly<BillingEventsToolbarProps>) {
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
        view: previous.view as BillingEventsView | undefined,
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

  const handleViewChange = (values: string[]) => {
    // ToggleGroup multi-select returns an array; the page treats this as single-select (one pill at a
    // time) so we collapse to the first value, falling back to the default "all" view if the user
    // somehow deselected everything.
    const next = (values[0] as BillingEventsView | undefined) ?? "all";
    if (next === view) {
      return;
    }
    navigate({
      to: "/billing-events",
      search: (previous) => ({
        search: previous.search,
        orderBy: previous.orderBy as SortableBillingEventProperties | undefined,
        sortOrder: previous.sortOrder,
        // "all" is the default — keep it out of the URL so the most common path stays clean.
        view: next === "all" ? undefined : next,
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

      <ToggleGroup variant="outline" aria-label={t`Event view`} value={[view]} onValueChange={handleViewChange}>
        <ToggleGroupItem value="all" className="min-w-[5rem] justify-center">
          <Trans>All</Trans>
        </ToggleGroupItem>
        <ToggleGroupItem value="mrr" className="min-w-[7rem] justify-center">
          <Trans>MRR impact</Trans>
        </ToggleGroupItem>
        <ToggleGroupItem value="state" className="min-w-[8rem] justify-center">
          <Trans>Subscription state</Trans>
        </ToggleGroupItem>
        <ToggleGroupItem value="other" className="min-w-[5rem] justify-center">
          <Trans>Other</Trans>
        </ToggleGroupItem>
      </ToggleGroup>
    </div>
  );
}

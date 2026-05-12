import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { InputGroup, InputGroupAddon, InputGroupButton, InputGroupInput } from "@repo/ui/components/InputGroup";
import { ToggleGroup, ToggleGroupItem } from "@repo/ui/components/ToggleGroup";
import { useDebounce } from "@repo/ui/hooks/useDebounce";
import { useNavigate } from "@tanstack/react-router";
import { SearchIcon, XIcon } from "lucide-react";
import { useEffect, useState } from "react";

import type { SortableBackOfficeInvoiceProperties } from "@/shared/lib/api/client";

export type InvoicesView = "all" | "invoices" | "refunds";

interface InvoicesToolbarProps {
  search: string | undefined;
  view: InvoicesView;
}

export function InvoicesToolbar({ search, view }: Readonly<InvoicesToolbarProps>) {
  const navigate = useNavigate();
  const [searchInput, setSearchInput] = useState(search ?? "");
  const debouncedSearch = useDebounce(searchInput, 500);

  useEffect(() => {
    if ((debouncedSearch || undefined) === search) {
      return;
    }
    navigate({
      to: "/invoices",
      search: (previous) => ({
        view: previous.view as InvoicesView | undefined,
        orderBy: previous.orderBy as SortableBackOfficeInvoiceProperties | undefined,
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
    const next = (values[0] as InvoicesView | undefined) ?? "all";
    if (next === view) {
      return;
    }
    navigate({
      to: "/invoices",
      search: (previous) => ({
        search: previous.search,
        orderBy: previous.orderBy as SortableBackOfficeInvoiceProperties | undefined,
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

      <ToggleGroup variant="outline" aria-label={t`Invoice view`} value={[view]} onValueChange={handleViewChange}>
        <ToggleGroupItem value="all" className="min-w-[5rem] justify-center">
          <Trans>All</Trans>
        </ToggleGroupItem>
        <ToggleGroupItem value="invoices" className="min-w-[7rem] justify-center">
          <Trans>Invoices</Trans>
        </ToggleGroupItem>
        <ToggleGroupItem value="refunds" className="min-w-[9rem] justify-center">
          <Trans>Refunds and credit notes</Trans>
        </ToggleGroupItem>
      </ToggleGroup>
    </div>
  );
}

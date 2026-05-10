import { t } from "@lingui/core/macro";
import { InputGroup, InputGroupAddon, InputGroupButton, InputGroupInput } from "@repo/ui/components/InputGroup";
import { MultiSelect } from "@repo/ui/components/MultiSelect";
import { useDebounce } from "@repo/ui/hooks/useDebounce";
import { useNavigate } from "@tanstack/react-router";
import { SearchIcon, XIcon } from "lucide-react";
import { useEffect, useMemo, useState } from "react";

import type { SortableBackOfficeInvoiceProperties } from "@/shared/lib/api/client";

import { BackOfficeInvoiceStatusFilter } from "@/shared/lib/api/client";

interface InvoicesToolbarProps {
  search: string | undefined;
  invoiceStatuses: BackOfficeInvoiceStatusFilter[];
}

// Order matches the typical lifecycle the operator scans for: successful payments first, then the
// exceptions worth investigating, then the credit-note flag (which is a property, not a status).
const ALL_STATUS_FILTERS: BackOfficeInvoiceStatusFilter[] = [
  BackOfficeInvoiceStatusFilter.Paid,
  BackOfficeInvoiceStatusFilter.Refunded,
  BackOfficeInvoiceStatusFilter.Failed,
  BackOfficeInvoiceStatusFilter.Pending,
  BackOfficeInvoiceStatusFilter.HasCreditNote
];

function getStatusFilterLabel(filter: BackOfficeInvoiceStatusFilter): string {
  switch (filter) {
    case BackOfficeInvoiceStatusFilter.Paid:
      return t`Paid`;
    case BackOfficeInvoiceStatusFilter.Refunded:
      return t`Refunded`;
    case BackOfficeInvoiceStatusFilter.Failed:
      return t`Failed`;
    case BackOfficeInvoiceStatusFilter.Pending:
      return t`Pending`;
    case BackOfficeInvoiceStatusFilter.HasCreditNote:
      return t`Credit note`;
    default:
      return String(filter);
  }
}

export function InvoicesToolbar({ search, invoiceStatuses }: Readonly<InvoicesToolbarProps>) {
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
        invoiceStatuses: previous.invoiceStatuses,
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

  const statusItems = useMemo(
    () => ALL_STATUS_FILTERS.map((value) => ({ id: value, label: getStatusFilterLabel(value) })),
    []
  );

  const handleStatusesChange = (values: string[]) => {
    const next = values as BackOfficeInvoiceStatusFilter[];
    navigate({
      to: "/invoices",
      search: (previous) => ({
        search: previous.search,
        orderBy: previous.orderBy as SortableBackOfficeInvoiceProperties | undefined,
        sortOrder: previous.sortOrder,
        invoiceStatuses: next.length === 0 ? undefined : next,
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
          name="invoice-invoiceStatuses"
          placeholder={t`All statuses`}
          items={statusItems}
          value={invoiceStatuses}
          onChange={handleStatusesChange}
        />
      </div>
    </div>
  );
}

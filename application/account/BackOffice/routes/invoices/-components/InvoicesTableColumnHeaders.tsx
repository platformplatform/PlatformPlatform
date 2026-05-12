import { Trans } from "@lingui/react/macro";
import { TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { ChevronDownIcon, ChevronUpIcon } from "lucide-react";

import { SortableBackOfficeInvoiceProperties, SortOrder } from "@/shared/lib/api/client";

interface InvoicesTableColumnHeadersProps {
  orderBy: SortableBackOfficeInvoiceProperties | undefined;
  sortOrder: SortOrder | undefined;
  onSort: (column: SortableBackOfficeInvoiceProperties) => void;
}

function SortableHead({
  column,
  orderBy,
  sortOrder,
  onSort,
  className,
  children
}: Readonly<{
  column: SortableBackOfficeInvoiceProperties;
  orderBy: SortableBackOfficeInvoiceProperties | undefined;
  sortOrder: SortOrder | undefined;
  onSort: (column: SortableBackOfficeInvoiceProperties) => void;
  className?: string;
  children: React.ReactNode;
}>) {
  const isActive = orderBy === column;
  // Backend default is Descending and the URL stores Descending as undefined; treat undefined as
  // Descending here so the chevron renders correctly when the active column is in its default state.
  const isDescending = isActive && (sortOrder ?? SortOrder.Descending) === SortOrder.Descending;
  const ariaSort = isActive ? (isDescending ? "descending" : "ascending") : "none";

  return (
    <TableHead className={className} aria-sort={ariaSort} onClick={() => onSort(column)}>
      {children}
      {isActive &&
        (isDescending ? (
          <ChevronDownIcon className="size-3 shrink-0" aria-hidden={true} />
        ) : (
          <ChevronUpIcon className="size-3 shrink-0" aria-hidden={true} />
        ))}
    </TableHead>
  );
}

export function InvoicesTableColumnHeaders({ orderBy, sortOrder, onSort }: Readonly<InvoicesTableColumnHeadersProps>) {
  return (
    <TableHeader className="z-10 bg-inherit sm:sticky sm:top-0">
      <TableRow>
        <SortableHead
          column={SortableBackOfficeInvoiceProperties.TenantName}
          orderBy={orderBy}
          sortOrder={sortOrder}
          onSort={onSort}
          className="w-[14rem] min-w-[12rem]"
        >
          <Trans>Account</Trans>
        </SortableHead>
        <SortableHead
          column={SortableBackOfficeInvoiceProperties.Date}
          orderBy={orderBy}
          sortOrder={sortOrder}
          onSort={onSort}
          className="w-[8rem]"
        >
          <Trans>Date</Trans>
        </SortableHead>
        <TableHead className="hidden w-[6rem] md:table-cell">
          <Trans>Plan</Trans>
        </TableHead>
        <TableHead className="hidden w-[6rem] text-right tabular-nums md:table-cell">
          <Trans>Amount</Trans>
        </TableHead>
        <TableHead className="hidden w-[5rem] text-right tabular-nums xl:table-cell">
          <Trans>VAT</Trans>
        </TableHead>
        <SortableHead
          column={SortableBackOfficeInvoiceProperties.Total}
          orderBy={orderBy}
          sortOrder={sortOrder}
          onSort={onSort}
          className="w-[6rem] text-right tabular-nums"
        >
          <Trans>Total</Trans>
        </SortableHead>
        <SortableHead
          column={SortableBackOfficeInvoiceProperties.Status}
          orderBy={orderBy}
          sortOrder={sortOrder}
          onSort={onSort}
          className="w-[7rem]"
        >
          <Trans>Status</Trans>
        </SortableHead>
      </TableRow>
    </TableHeader>
  );
}

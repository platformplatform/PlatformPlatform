import { Trans } from "@lingui/react/macro";
import { TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { ChevronDownIcon, ChevronUpIcon } from "lucide-react";

import { SortableBillingEventProperties, SortOrder } from "@/shared/lib/api/client";

interface BillingEventsTableColumnHeadersProps {
  orderBy: SortableBillingEventProperties | undefined;
  sortOrder: SortOrder | undefined;
  onSort: (column: SortableBillingEventProperties) => void;
}

// Local SortableTableHead so the column reuses the same sort-affordance as Accounts without
// coupling /billing-events to /accounts internal components.
function SortableHead({
  column,
  orderBy,
  sortOrder,
  onSort,
  className,
  children
}: Readonly<{
  column: SortableBillingEventProperties;
  orderBy: SortableBillingEventProperties | undefined;
  sortOrder: SortOrder | undefined;
  onSort: (column: SortableBillingEventProperties) => void;
  className?: string;
  children: React.ReactNode;
}>) {
  const isActive = orderBy === column;
  // Backend default is Descending, stored as undefined in the URL — treat undefined as Descending here so the
  // chevron renders correctly when the active column is in its default descending state.
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

export function BillingEventsTableColumnHeaders({
  orderBy,
  sortOrder,
  onSort
}: Readonly<BillingEventsTableColumnHeadersProps>) {
  return (
    <TableHeader className="z-10 bg-inherit sm:sticky sm:top-0">
      <TableRow>
        <SortableHead
          column={SortableBillingEventProperties.TenantName}
          orderBy={orderBy}
          sortOrder={sortOrder}
          onSort={onSort}
          className="w-[14rem] min-w-[12rem]"
        >
          <Trans>Account</Trans>
        </SortableHead>
        <SortableHead
          column={SortableBillingEventProperties.EventType}
          orderBy={orderBy}
          sortOrder={sortOrder}
          onSort={onSort}
          className="w-[10rem]"
        >
          <Trans>Event</Trans>
        </SortableHead>
        <TableHead className="hidden w-[10rem] md:table-cell">
          <Trans>Plan transition</Trans>
        </TableHead>
        <TableHead className="hidden w-[6rem] tabular-nums md:table-cell">
          <Trans>Amount</Trans>
        </TableHead>
        <TableHead className="hidden w-[5rem] xl:table-cell">
          <Trans>Country</Trans>
        </TableHead>
        <SortableHead
          column={SortableBillingEventProperties.OccurredAt}
          orderBy={orderBy}
          sortOrder={sortOrder}
          onSort={onSort}
          className="w-[8rem]"
        >
          <Trans>Occurred</Trans>
        </SortableHead>
      </TableRow>
    </TableHeader>
  );
}

import { TableHead } from "@repo/ui/components/Table";
import { ChevronDownIcon, ChevronUpIcon } from "lucide-react";

import type { SortableTicketProperties } from "@/shared/lib/api/client";

import { SortOrder } from "@/shared/lib/api/client";

export function SortableInboxTableHead({
  column,
  orderBy,
  sortOrder,
  onSort,
  className,
  children
}: Readonly<{
  column: SortableTicketProperties;
  orderBy: SortableTicketProperties;
  sortOrder: SortOrder;
  onSort: (column: SortableTicketProperties) => void;
  className?: string;
  children: React.ReactNode;
}>) {
  const isActive = orderBy === column;
  const isDescending = isActive && sortOrder === SortOrder.Descending;
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

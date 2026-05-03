import { TableHead } from "@repo/ui/components/Table";
import { ChevronDownIcon, ChevronUpIcon } from "lucide-react";

import type { SortableTenantProperties } from "@/shared/lib/api/client";

import { SortOrder } from "@/shared/lib/api/client";

export function SortableTableHead({
  column,
  orderBy,
  sortOrder,
  onSort,
  className,
  children
}: Readonly<{
  column: SortableTenantProperties;
  orderBy: SortableTenantProperties | undefined;
  sortOrder: SortOrder | undefined;
  onSort: (column: SortableTenantProperties) => void;
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

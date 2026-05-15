import { TableHead } from "@repo/ui/components/Table";
import { ChevronDownIcon, ChevronUpIcon } from "lucide-react";

import { SortOrder } from "@/shared/lib/api/client";

interface SortableHeadProps<TColumn extends string> {
  column: TColumn;
  effectiveOrderBy: TColumn;
  effectiveSortOrder: SortOrder;
  onSort: (column: TColumn) => void;
  className?: string;
  children: React.ReactNode;
}

export function SortableHead<TColumn extends string>({
  column,
  effectiveOrderBy,
  effectiveSortOrder,
  onSort,
  className,
  children
}: Readonly<SortableHeadProps<TColumn>>) {
  const isActive = effectiveOrderBy === column;
  const isDescending = isActive && effectiveSortOrder === SortOrder.Descending;
  const ariaSort = isActive ? (isDescending ? "descending" : "ascending") : "none";

  return (
    <TableHead className={className} aria-sort={ariaSort} onClick={() => onSort(column)}>
      <span className="inline-flex cursor-pointer items-center gap-1 select-none">
        {children}
        {isActive &&
          (isDescending ? (
            <ChevronDownIcon className="size-3 shrink-0" aria-hidden={true} />
          ) : (
            <ChevronUpIcon className="size-3 shrink-0" aria-hidden={true} />
          ))}
      </span>
    </TableHead>
  );
}

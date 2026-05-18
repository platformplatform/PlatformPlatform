import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Checkbox } from "@repo/ui/components/Checkbox";
import { TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { ArrowDownIcon, ArrowUpIcon } from "lucide-react";

type SortDirection = "ascending" | "descending";

interface TablePreviewHeaderProps {
  sortColumn: string;
  sortDirection: SortDirection;
  onSort: (column: string) => void;
  fixedColumns: boolean;
  showCheckboxes: boolean;
  multiSelect: boolean;
  allChecked: boolean;
  indeterminate: boolean;
  onToggleAll: () => void;
}

function SortIndicator({
  column,
  sortColumn,
  direction
}: {
  column: string;
  sortColumn: string;
  direction: SortDirection;
}) {
  if (column !== sortColumn) {
    return null;
  }
  return direction === "ascending" ? <ArrowUpIcon className="size-3.5" /> : <ArrowDownIcon className="size-3.5" />;
}

export function TablePreviewHeader({
  sortColumn,
  sortDirection,
  onSort,
  fixedColumns,
  showCheckboxes,
  multiSelect,
  allChecked,
  indeterminate,
  onToggleAll
}: Readonly<TablePreviewHeaderProps>) {
  return (
    <TableHeader>
      <TableRow>
        {showCheckboxes && (
          <TableHead className="w-10">
            {multiSelect && (
              <Checkbox
                checked={allChecked}
                indeterminate={indeterminate}
                onCheckedChange={onToggleAll}
                aria-label={t`Select all rows on this page`}
              />
            )}
          </TableHead>
        )}
        <TableHead data-column="name" onClick={() => onSort("name")}>
          <Trans>Recipe</Trans>
          <SortIndicator column="name" sortColumn={sortColumn} direction={sortDirection} />
        </TableHead>
        <TableHead data-column="cuisine" onClick={() => onSort("cuisine")}>
          <Trans>Cuisine</Trans>
          <SortIndicator column="cuisine" sortColumn={sortColumn} direction={sortDirection} />
        </TableHead>
        <TableHead
          data-column="cookTime"
          className={fixedColumns ? "w-28" : undefined}
          onClick={() => onSort("cookTime")}
        >
          <Trans>Cook time</Trans>
          <SortIndicator column="cookTime" sortColumn={sortColumn} direction={sortDirection} />
        </TableHead>
        <TableHead
          data-column="addedAt"
          className={fixedColumns ? "w-36" : undefined}
          onClick={() => onSort("addedAt")}
        >
          <Trans>Added</Trans>
          <SortIndicator column="addedAt" sortColumn={sortColumn} direction={sortDirection} />
        </TableHead>
        <TableHead
          data-column="difficulty"
          className={fixedColumns ? "w-36" : undefined}
          onClick={() => onSort("difficulty")}
        >
          <Trans>Difficulty</Trans>
          <SortIndicator column="difficulty" sortColumn={sortColumn} direction={sortDirection} />
        </TableHead>
        <TableHead className="w-12" />
      </TableRow>
    </TableHeader>
  );
}

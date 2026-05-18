import type { RowKey, TableRowSize } from "@repo/ui/components/Table";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Table, TableBody } from "@repo/ui/components/Table";
import { TablePagination } from "@repo/ui/components/TablePagination";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { useEffect, useMemo, useState } from "react";

import type { SampleDish } from "./sampleDishData";

import { DishRow } from "./DishRow";
import { pageSize, sampleDishes } from "./sampleDishData";
import { TablePreviewHeader } from "./TablePreviewHeader";
import { TablePreviewToolbar } from "./TablePreviewToolbar";

type SortDirection = "ascending" | "descending";

interface TablePreviewProps {
  selectedDish?: SampleDish | null;
  onDishSelect?: (dish: SampleDish | null) => void;
  onSelectedDishesChange?: (dishes: SampleDish[]) => void;
  onSummaryPaneChange?: (enabled: boolean) => void;
}

export function TablePreview({
  selectedDish,
  onDishSelect,
  onSelectedDishesChange,
  onSummaryPaneChange
}: TablePreviewProps) {
  const [currentPage, setCurrentPage] = useState(1);
  const [sortColumn, setSortColumn] = useState("name");
  const [sortDirection, setSortDirection] = useState<SortDirection>("ascending");
  const [rowSize, setRowSize] = useState<TableRowSize>("spacious");
  const [fixedColumns, setFixedColumns] = useState(true);
  const [showCheckboxes, setShowCheckboxes] = useState(true);
  const [multiSelect, setMultiSelect] = useState(true);
  const [summaryPane, setSummaryPane] = useState(true);
  const [selectedKeys, setSelectedKeys] = useState<ReadonlySet<RowKey>>(() => new Set());
  const formatDate = useFormatDate();

  useEffect(() => {
    const selected = sampleDishes.filter((dish) => selectedKeys.has(dish.id));
    onSelectedDishesChange?.(selected);
  }, [selectedKeys, onSelectedDishesChange]);

  // Dependent toggles (Show checkboxes, Summary pane) stay disabled rather than unchecked when
  // Multi-select flips off, so turning Multi-select back on restores the previous preview. The
  // effective booleans below mirror that pattern.
  const effectiveShowCheckboxes = multiSelect && showCheckboxes;
  const effectiveSummaryPane = multiSelect && summaryPane;

  useEffect(() => {
    onSummaryPaneChange?.(effectiveSummaryPane);
  }, [effectiveSummaryPane, onSummaryPaneChange]);

  const sortedDishes = useMemo(
    () =>
      [...sampleDishes].sort((a, b) => {
        const aValue = a[sortColumn as keyof SampleDish];
        const bValue = b[sortColumn as keyof SampleDish];
        const comparison =
          typeof aValue === "number" && typeof bValue === "number"
            ? aValue - bValue
            : String(aValue).localeCompare(String(bValue));
        return sortDirection === "ascending" ? comparison : -comparison;
      }),
    [sortColumn, sortDirection]
  );

  const totalPages = Math.ceil(sortedDishes.length / pageSize);
  const paginatedDishes = sortedDishes.slice((currentPage - 1) * pageSize, currentPage * pageSize);

  const handleSort = (column: string) => {
    if (sortColumn === column) {
      setSortDirection(sortDirection === "ascending" ? "descending" : "ascending");
    } else {
      setSortColumn(column);
      setSortDirection("ascending");
    }
  };

  const handleActivate = (key: RowKey) => {
    if (selectedDish?.id === Number(key)) {
      onDishSelect?.(null);
      return;
    }
    const dish = sampleDishes.find((d) => d.id === Number(key)) ?? null;
    onDishSelect?.(dish);
  };

  const handleMultiSelectChange = (checked: boolean) => {
    setMultiSelect(checked);
    if (!checked) {
      setSelectedKeys((prev) => {
        const first = prev.values().next().value;
        return first != null ? new Set<RowKey>([first]) : new Set<RowKey>();
      });
    }
  };

  // Select-all spans the whole dataset, not just the current page: checked means every row is
  // selected, indeterminate means some but not all are selected. Toggling clears everything when
  // fully selected, otherwise selects every row across all pages.
  const allChecked = sampleDishes.length > 0 && selectedKeys.size === sampleDishes.length;
  const someChecked = selectedKeys.size > 0;
  const headerIndeterminate = someChecked && !allChecked;
  const toggleAll = () => {
    if (allChecked) {
      setSelectedKeys(new Set<RowKey>());
      return;
    }
    setSelectedKeys(new Set<RowKey>(sampleDishes.map((d) => d.id)));
  };

  return (
    <div className="flex flex-1 flex-col gap-2">
      <p className="text-sm text-muted-foreground">
        <Trans>
          Click a row to open the side pane. Use Shift or Cmd/Ctrl with click or arrow keys to multi-select, Enter to
          open, and Space to toggle the selection.
        </Trans>
      </p>
      <TablePreviewToolbar
        fixedColumns={fixedColumns}
        setFixedColumns={setFixedColumns}
        rowSize={rowSize}
        setRowSize={setRowSize}
        showCheckboxes={showCheckboxes}
        onShowCheckboxesChange={setShowCheckboxes}
        multiSelect={multiSelect}
        onMultiSelectChange={handleMultiSelectChange}
        summaryPane={summaryPane}
        onSummaryPaneChange={setSummaryPane}
      />
      <Table
        rowSize={rowSize}
        aria-label={t`Recipes`}
        selectionMode={multiSelect ? "multiple" : "single"}
        selectedKeys={selectedKeys}
        onSelectionChange={setSelectedKeys}
        onActivate={handleActivate}
        activateOnNavigate={selectedDish != null}
        scrollToKey={selectedDish?.id}
      >
        <TablePreviewHeader
          sortColumn={sortColumn}
          sortDirection={sortDirection}
          onSort={handleSort}
          fixedColumns={fixedColumns}
          showCheckboxes={effectiveShowCheckboxes}
          multiSelect={multiSelect}
          allChecked={allChecked}
          indeterminate={headerIndeterminate}
          onToggleAll={toggleAll}
        />
        <TableBody>
          {paginatedDishes.map((dish) => (
            <DishRow
              key={dish.id}
              dish={dish}
              rowSize={rowSize}
              formatDate={formatDate}
              showCheckbox={effectiveShowCheckboxes}
              isChecked={selectedKeys.has(dish.id)}
            />
          ))}
        </TableBody>
      </Table>
      <div className="mt-auto shrink-0 pt-2">
        <TablePagination
          currentPage={currentPage}
          totalPages={totalPages}
          onPageChange={setCurrentPage}
          previousLabel={t`Previous`}
          nextLabel={t`Next`}
          trackingTitle="Component preview recipes"
          className="w-full"
        />
      </div>
    </div>
  );
}

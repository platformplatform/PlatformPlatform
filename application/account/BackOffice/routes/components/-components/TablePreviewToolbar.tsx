import type { TableRowSize } from "@repo/ui/components/Table";

import { t } from "@lingui/core/macro";
import { SwitchField } from "@repo/ui/components/SwitchField";

interface TablePreviewToolbarProps {
  fixedColumns: boolean;
  setFixedColumns: (value: boolean) => void;
  rowSize: TableRowSize;
  setRowSize: (value: TableRowSize) => void;
  showCheckboxes: boolean;
  onShowCheckboxesChange: (value: boolean) => void;
  multiSelect: boolean;
  onMultiSelectChange: (value: boolean) => void;
  summaryPane: boolean;
  onSummaryPaneChange: (value: boolean) => void;
}

export function TablePreviewToolbar({
  fixedColumns,
  setFixedColumns,
  rowSize,
  setRowSize,
  showCheckboxes,
  onShowCheckboxesChange,
  multiSelect,
  onMultiSelectChange,
  summaryPane,
  onSummaryPaneChange
}: Readonly<TablePreviewToolbarProps>) {
  return (
    <div className="flex items-center justify-between">
      <div className="flex flex-wrap items-center gap-4">
        <SwitchField
          label={t`Fixed columns`}
          name="fixed-columns"
          checked={fixedColumns}
          onCheckedChange={setFixedColumns}
        />
        <SwitchField
          label={t`Spacious rows`}
          name="spacious-rows"
          checked={rowSize === "spacious"}
          onCheckedChange={(checked) => setRowSize(checked ? "spacious" : "compact")}
        />
        <SwitchField
          label={t`Multi-select`}
          name="multi-select"
          checked={multiSelect}
          onCheckedChange={onMultiSelectChange}
        />
        <SwitchField
          label={t`Show checkboxes`}
          name="show-checkboxes"
          checked={showCheckboxes}
          disabled={!multiSelect}
          onCheckedChange={onShowCheckboxesChange}
        />
        <SwitchField
          label={t`Multi-select summary side pane`}
          name="summary-pane"
          checked={summaryPane}
          disabled={!multiSelect}
          onCheckedChange={onSummaryPaneChange}
        />
      </div>
    </div>
  );
}

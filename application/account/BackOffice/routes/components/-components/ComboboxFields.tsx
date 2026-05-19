import type { ReactNode } from "react";

import { t } from "@lingui/core/macro";
import { ComboboxField } from "@repo/ui/components/ComboboxField";
import { TrendingUpIcon } from "lucide-react";
import { useState } from "react";

import type { ControlRowDerivedProps } from "./controlRowTypes";

import { tooltips } from "./controlTooltips";

type ChartItem = { id: string; label: string; icon?: ReactNode };

export interface ComboboxFieldsProps extends ControlRowDerivedProps {
  chartItems: ChartItem[];
}

export function ComboboxFields({
  label,
  tooltip,
  disabled,
  readOnly,
  showIcon,
  hasValues,
  placeholders,
  errorMessage,
  chartItems
}: ComboboxFieldsProps) {
  const items = showIcon ? chartItems : chartItems.map(({ icon: _, ...rest }) => rest);
  const [selectValue, setSelectValue] = useState<string | null>(hasValues ? "pie" : null);
  const [freeTextValue, setFreeTextValue] = useState<string | null>(hasValues ? "pie" : null);
  const [creatableValue, setCreatableValue] = useState<string | null>(hasValues ? "pie" : null);
  const [creatableItems, setCreatableItems] = useState<ChartItem[]>([]);
  const allCreatableItems = [...items, ...creatableItems];

  return (
    <>
      <ComboboxField
        label={label ? t`Combobox` : undefined}
        tooltip={tooltip ? tooltips.combobox : undefined}
        placeholder={placeholders ? t`Search charts...` : undefined}
        emptyMessage={t`No results found`}
        items={items}
        value={selectValue}
        onValueChange={setSelectValue}
        disabled={disabled}
        readOnly={readOnly}
        errorMessage={errorMessage}
        startIcon={showIcon && (selectValue || placeholders) ? <TrendingUpIcon /> : undefined}
      />
      <ComboboxField
        label={label ? t`Combobox (free text)` : undefined}
        tooltip={tooltip ? tooltips.comboboxFreeText : undefined}
        placeholder={placeholders ? t`Type or search...` : undefined}
        items={items}
        value={freeTextValue}
        onValueChange={setFreeTextValue}
        disabled={disabled}
        readOnly={readOnly}
        errorMessage={errorMessage}
        startIcon={showIcon && (freeTextValue || placeholders) ? <TrendingUpIcon /> : undefined}
        allowCustomValue
      />
      <ComboboxField
        label={label ? t`Combobox (creatable)` : undefined}
        tooltip={tooltip ? tooltips.comboboxCreatable : undefined}
        placeholder={placeholders ? t`Type or search...` : undefined}
        items={allCreatableItems}
        value={creatableValue}
        onValueChange={setCreatableValue}
        disabled={disabled}
        readOnly={readOnly}
        errorMessage={errorMessage}
        startIcon={showIcon && (creatableValue || placeholders) ? <TrendingUpIcon /> : undefined}
        allowCreate
        onCreateItem={(itemLabel) => {
          const newId = itemLabel.toLowerCase().replace(/\s+/g, "-");
          if (!allCreatableItems.some((item) => item.id === newId)) {
            setCreatableItems((previous) => [...previous, { id: newId, label: itemLabel }]);
          }
        }}
      />
    </>
  );
}

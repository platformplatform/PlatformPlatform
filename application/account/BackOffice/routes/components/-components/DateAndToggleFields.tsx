import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { CheckboxField } from "@repo/ui/components/CheckboxField";
import { DateField } from "@repo/ui/components/DateField";
import { DateInput } from "@repo/ui/components/DateInput";
import { DatePicker } from "@repo/ui/components/DatePicker";
import { DateRangePicker } from "@repo/ui/components/DateRangePicker";
import { InlineFieldGroup } from "@repo/ui/components/InlineFieldGroup";
import { RadioGroupItem } from "@repo/ui/components/RadioGroup";
import { RadioGroupField } from "@repo/ui/components/RadioGroupField";
import { SwitchField } from "@repo/ui/components/SwitchField";
import { TimeField } from "@repo/ui/components/TimeField";
import { useState } from "react";

import type { ControlRowDerivedProps } from "./controlRowTypes";

import { tooltips } from "./controlTooltips";
import { TextAreaFields } from "./TextAreaFields";
import { ToggleGroupField } from "./ToggleGroupField";
import { WorkdayPicker } from "./WorkdayPicker";

export function DateAndToggleFields({
  suffix,
  label,
  tooltip,
  disabled,
  readOnly,
  showIcon,
  hasValues,
  placeholders,
  errorMessage
}: ControlRowDerivedProps) {
  const [switchChecked, setSwitchChecked] = useState(hasValues);
  const [checkboxChecked, setCheckboxChecked] = useState(hasValues);
  const [indeterminateCheckboxChecked, setIndeterminateCheckboxChecked] = useState(false);
  const [isIndeterminate, setIsIndeterminate] = useState(true);
  const [datePickerValue, setDatePickerValue] = useState<string | undefined>(hasValues ? "2025-06-15" : undefined);
  const [dateInputValue, setDateInputValue] = useState<string | undefined>(hasValues ? "2025-06-15" : undefined);
  const [dateRangeValue, setDateRangeValue] = useState<{ start: Date; end: Date } | null>(
    hasValues ? { start: new Date(2025, 5, 1), end: new Date(2025, 5, 15) } : null
  );

  return (
    <>
      <DatePicker
        label={label ? t`Date picker` : undefined}
        tooltip={tooltip ? tooltips.datePicker : undefined}
        name={`datepicker-${suffix}`}
        placeholder={placeholders ? t`Pick a date` : undefined}
        startIcon={showIcon ? undefined : null}
        value={datePickerValue}
        onChange={setDatePickerValue}
        displayFormat="long"
        disabled={disabled}
        readOnly={readOnly}
        errorMessage={errorMessage}
      />
      <WorkdayPicker
        suffix={suffix}
        label={label}
        tooltip={tooltip}
        hasValues={hasValues}
        showIcon={showIcon}
        placeholders={placeholders}
        disabled={disabled}
        readOnly={readOnly}
        errorMessage={errorMessage}
      />
      <DateInput
        label={label ? t`Date input` : undefined}
        tooltip={tooltip ? tooltips.dateInput : undefined}
        name={`dateinput-${suffix}`}
        placeholder={placeholders ? t`Type a date` : undefined}
        startIcon={showIcon ? undefined : null}
        value={dateInputValue}
        onChange={setDateInputValue}
        disabled={disabled}
        readOnly={readOnly}
        errorMessage={errorMessage}
      />
      <DateRangePicker
        label={label ? t`Date range` : undefined}
        tooltip={tooltip ? tooltips.dateRange : undefined}
        name={`daterange-${suffix}`}
        placeholder={placeholders ? undefined : ""}
        startIcon={showIcon ? undefined : null}
        value={dateRangeValue}
        onChange={setDateRangeValue}
        displayFormat="short"
        disabled={disabled}
        readOnly={readOnly}
        errorMessage={errorMessage}
      />
      <DateField
        label={label ? t`Native date` : undefined}
        tooltip={tooltip ? tooltips.dateField : undefined}
        name={`datefield-${suffix}`}
        defaultValue={hasValues ? "2025-06-15" : undefined}
        disabled={disabled}
        readOnly={readOnly}
        errorMessage={errorMessage}
      />
      <TimeField
        label={label ? t`Native time` : undefined}
        tooltip={tooltip ? tooltips.timeField : undefined}
        name={`time-${suffix}`}
        defaultValue={hasValues ? "14:30" : undefined}
        disabled={disabled}
        readOnly={readOnly}
        errorMessage={errorMessage}
      />
      <TextAreaFields {...{ suffix, label, tooltip, disabled, readOnly, hasValues, placeholders, errorMessage }} />
      <InlineFieldGroup alignWithLabel={label}>
        <SwitchField
          label={t`Switch`}
          tooltip={tooltip ? tooltips.switchField : undefined}
          name={`switch-${suffix}`}
          checked={switchChecked}
          onCheckedChange={setSwitchChecked}
          disabled={disabled}
          readOnly={readOnly}
          errorMessage={errorMessage}
        />
      </InlineFieldGroup>
      <InlineFieldGroup alignWithLabel={label}>
        <CheckboxField
          label={t`Checkbox`}
          tooltip={tooltip ? tooltips.checkboxField : undefined}
          name={`checkbox-${suffix}`}
          checked={checkboxChecked}
          onCheckedChange={setCheckboxChecked}
          disabled={disabled}
          readOnly={readOnly}
          errorMessage={errorMessage}
        />
        <CheckboxField
          label={t`Indeterminate`}
          tooltip={tooltip ? tooltips.checkboxIndeterminate : undefined}
          name={`checkbox-indeterminate-${suffix}`}
          checked={indeterminateCheckboxChecked}
          indeterminate={isIndeterminate}
          onCheckedChange={(checked) => {
            setIsIndeterminate(false);
            setIndeterminateCheckboxChecked(checked);
          }}
          disabled={disabled}
          readOnly={readOnly}
          errorMessage={errorMessage}
        />
      </InlineFieldGroup>
      <RadioGroupField
        label={label ? t`Radio group` : undefined}
        tooltip={tooltip ? tooltips.radioGroup : undefined}
        name={`radio-${suffix}`}
        defaultValue={hasValues ? "option-a" : undefined}
        disabled={disabled}
        readOnly={readOnly}
        errorMessage={errorMessage}
      >
        <label htmlFor={`radio-${suffix}-a`} className="flex items-center gap-2">
          <RadioGroupItem id={`radio-${suffix}-a`} value="option-a" />
          <Trans>Option A</Trans>
        </label>
        <label htmlFor={`radio-${suffix}-b`} className="flex items-center gap-2">
          <RadioGroupItem id={`radio-${suffix}-b`} value="option-b" />
          <Trans>Option B</Trans>
        </label>
      </RadioGroupField>
      <ToggleGroupField label={label} tooltip={tooltip} hasValues={hasValues} disabled={disabled} readOnly={readOnly} />
    </>
  );
}

import { t } from "@lingui/core/macro";
import { DatePicker } from "@repo/ui/components/DatePicker";
import { type ReactNode, useState } from "react";

import { tooltips } from "./controlTooltips";

interface WorkdayPickerProps {
  suffix: string;
  label?: boolean;
  tooltip?: boolean;
  hasValues?: boolean;
  showIcon?: boolean;
  placeholders?: boolean;
  disabled?: boolean;
  readOnly?: boolean;
  errorMessage?: string;
}

function toIsoDate(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function isWeekend(date: Date): boolean {
  return date.getDay() === 0 || date.getDay() === 6;
}

// Demonstrates DatePicker constraints: bounded range (today + 4 through today + 30) plus an
// arbitrary per-date predicate (weekends are unselectable). The predicate is just a function, so
// any rule works -- e.g. excluding holidays, or whitelisting a fetched set of available dates.
export function WorkdayPicker({
  suffix,
  label,
  tooltip,
  hasValues,
  showIcon,
  placeholders,
  disabled,
  readOnly,
  errorMessage
}: Readonly<WorkdayPickerProps>): ReactNode {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  // Range includes yesterday/today/tomorrow so the relative display ("Today", "Yesterday", ...)
  // can be shown off in the demo.
  const minimumDate = new Date(today);
  minimumDate.setDate(minimumDate.getDate() - 7);
  const maximumDate = new Date(today);
  maximumDate.setDate(maximumDate.getDate() + 30);

  // Today, or the next workday if today is a weekend; used as the demo default when "Values" is on.
  const defaultWorkday = (() => {
    const candidate = new Date(today);
    while (isWeekend(candidate) && candidate <= maximumDate) {
      candidate.setDate(candidate.getDate() + 1);
    }
    return candidate;
  })();

  const [value, setValue] = useState<string | undefined>(hasValues ? toIsoDate(defaultWorkday) : undefined);

  return (
    <DatePicker
      label={label ? t`Workday picker` : undefined}
      tooltip={tooltip ? tooltips.workdayPicker : undefined}
      name={`workday-${suffix}`}
      placeholder={placeholders ? t`Pick a workday` : undefined}
      startIcon={showIcon ? undefined : null}
      value={value}
      onChange={setValue}
      min={toIsoDate(minimumDate)}
      max={toIsoDate(maximumDate)}
      disabledDate={isWeekend}
      displayFormat="relative"
      disabled={disabled}
      readOnly={readOnly}
      errorMessage={errorMessage}
    />
  );
}

import type { DateRange } from "react-day-picker";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Calendar } from "@repo/ui/components/Calendar";
import { FieldError } from "@repo/ui/components/Field";
import { LabelWithTooltip } from "@repo/ui/components/LabelWithTooltip";
import { useFieldError } from "@repo/ui/hooks/useFieldError";
import { useState } from "react";

import { Prop, PropList, PropNote } from "./PropTooltip";

interface InlineCalendarPreviewProps {
  label?: boolean;
  tooltip?: boolean;
  disabled?: boolean;
  readOnly?: boolean;
  error?: boolean;
}

const singleTooltip = (
  <PropList title="Calendar (single)" description="Pick exactly one date">
    <Prop name="mode">"single"</Prop>
    <Prop name="selected / onSelect">Date | undefined</Prop>
    <PropNote>Use for birthdays, due dates, appointment slots - any single-date choice.</PropNote>
  </PropList>
);

const multipleTooltip = (
  <PropList title="Calendar (multiple)" description="Pick several non-contiguous dates">
    <Prop name="mode">"multiple"</Prop>
    <Prop name="selected / onSelect">Date[] | undefined</Prop>
    <PropNote>Use for PTO, shift availability, or any flagging of individual dates.</PropNote>
  </PropList>
);

const rangeTooltip = (
  <PropList title="Calendar (range)" description="Pick a contiguous start/end range">
    <Prop name="mode">"range"</Prop>
    <Prop name="selected / onSelect">{"{ from, to } | undefined"}</Prop>
    <PropNote>Use for bookings, reports, anything defined by a span.</PropNote>
  </PropList>
);

export function InlineCalendarPreview({
  label,
  tooltip,
  disabled,
  readOnly,
  error
}: Readonly<InlineCalendarPreviewProps>) {
  const [singleDate, setSingleDate] = useState<Date | undefined>(() => new Date(2026, 3, 10));
  const [multipleDates, setMultipleDates] = useState<Date[] | undefined>(() => [
    new Date(2026, 3, 3),
    new Date(2026, 3, 14),
    new Date(2026, 3, 22)
  ]);
  const [range, setRange] = useState<DateRange | undefined>(() => ({
    from: new Date(2026, 3, 7),
    to: new Date(2026, 3, 18)
  }));

  const errorMessage = error ? t`This field is required` : undefined;
  const single = useFieldError({ errorMessage });
  const multiple = useFieldError({ errorMessage });
  const rangeError = useFieldError({ errorMessage });

  const handleSingleSelect = (date: Date | undefined) => {
    single.clearNow();
    setSingleDate(date);
  };
  const handleMultipleSelect = (dates: Date[] | undefined) => {
    multiple.clearNow();
    setMultipleDates(dates);
  };
  const handleRangeSelect = (next: DateRange | undefined) => {
    rangeError.clearNow();
    setRange(next);
  };

  return (
    <div className="flex flex-col gap-2">
      <h4>
        <Trans>Inline calendar</Trans>
      </h4>
      <div className="flex flex-wrap gap-6">
        <div className="flex flex-col gap-3">
          {label && (
            <div className="flex items-center gap-1 text-sm leading-snug font-medium">
              <LabelWithTooltip tooltip={tooltip ? singleTooltip : undefined}>
                <Trans>Calendar (single)</Trans>
              </LabelWithTooltip>
            </div>
          )}
          <Calendar
            mode="single"
            selected={singleDate}
            onSelect={handleSingleSelect}
            numberOfMonths={1}
            showWeekNumber
            disabled={disabled}
            readOnly={readOnly}
            aria-invalid={single.isInvalid || undefined}
          />
          <FieldError errors={single.errors} />
        </div>
        <div className="flex flex-col gap-3">
          {label && (
            <div className="flex items-center gap-1 text-sm leading-snug font-medium">
              <LabelWithTooltip tooltip={tooltip ? multipleTooltip : undefined}>
                <Trans>Calendar (multiple)</Trans>
              </LabelWithTooltip>
            </div>
          )}
          <Calendar
            mode="multiple"
            selected={multipleDates}
            onSelect={handleMultipleSelect}
            numberOfMonths={1}
            disabled={disabled}
            readOnly={readOnly}
            aria-invalid={multiple.isInvalid || undefined}
          />
          <FieldError errors={multiple.errors} />
        </div>
        <div className="flex flex-col gap-3">
          {label && (
            <div className="flex items-center gap-1 text-sm leading-snug font-medium">
              <LabelWithTooltip tooltip={tooltip ? rangeTooltip : undefined}>
                <Trans>Calendar (range)</Trans>
              </LabelWithTooltip>
            </div>
          )}
          <Calendar
            mode="range"
            selected={range}
            onSelect={handleRangeSelect}
            numberOfMonths={1}
            disabled={disabled}
            readOnly={readOnly}
            aria-invalid={rangeError.isInvalid || undefined}
          />
          <FieldError errors={rangeError.errors} />
        </div>
      </div>
    </div>
  );
}

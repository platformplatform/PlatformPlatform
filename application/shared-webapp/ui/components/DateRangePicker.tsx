import { useLingui } from "@lingui/react";
import { format, type Locale } from "date-fns";
import { da, enUS } from "date-fns/locale";
import { CalendarIcon, XIcon } from "lucide-react";
import { useState } from "react";
import type { DateRange } from "react-day-picker";
import { cn } from "../utils";
import { Button } from "./Button";
import { Calendar } from "./Calendar";
import { Field, FieldLabel } from "./Field";
import { Popover, PopoverContent, PopoverTrigger } from "./Popover";

/**
 * Maps app locale codes to date-fns locale objects.
 * Add new locales here when extending language support.
 */
const dateFnsLocaleMap: Record<string, Locale> = {
  "en-US": enUS,
  "da-DK": da
};

export interface DateRangeValue {
  start: Date;
  end: Date;
}

export interface DateRangePickerProps {
  label?: string;
  value?: DateRangeValue | null;
  onChange?: (value: DateRangeValue | null) => void;
  placeholder?: string;
  className?: string;
  disabled?: boolean;
}

export function DateRangePicker({
  label,
  value,
  onChange,
  placeholder = "Select dates",
  className,
  disabled
}: Readonly<DateRangePickerProps>) {
  const { i18n } = useLingui();
  const dateLocale = dateFnsLocaleMap[i18n.locale] ?? enUS;
  const [open, setOpen] = useState(false);
  const [selectionsCount, setSelectionsCount] = useState(0);
  // Track the first clicked date separately since react-day-picker's onSelect
  // doesn't reliably tell us which date was clicked
  const [firstClickDate, setFirstClickDate] = useState<Date | null>(null);

  const dateRange: DateRange | undefined = value ? { from: value.start, to: value.end } : undefined;

  const handleOpenChange = (isOpen: boolean) => {
    setOpen(isOpen);
    if (isOpen) {
      setSelectionsCount(0);
      setFirstClickDate(null);
    }
  };

  const handleDayClick = (day: Date) => {
    const newCount = selectionsCount + 1;
    setSelectionsCount(newCount);

    if (newCount === 1) {
      // First click: use existing start as pivot (matches react-day-picker behavior)
      const existingStart = value?.start;
      const existingEnd = value?.end;
      setFirstClickDate(day);

      if (existingStart && existingEnd) {
        if (day.getTime() < existingStart.getTime()) {
          // Clicked before existing start: clicked becomes start, keep end
          onChange?.({ start: day, end: existingEnd });
        } else {
          // Clicked on or after existing start: keep start, clicked becomes end
          onChange?.({ start: existingStart, end: day });
        }
      } else {
        // No existing range: clicked becomes both start and end
        onChange?.({ start: day, end: day });
      }
    } else {
      // Second click: combine with the first click to form the range
      const firstDate = firstClickDate ?? day;

      // Earlier date becomes start, later becomes end
      let newStart = firstDate;
      let newEnd = day;
      if (newStart.getTime() > newEnd.getTime()) {
        [newStart, newEnd] = [newEnd, newStart];
      }

      onChange?.({ start: newStart, end: newEnd });

      // Close if we have a valid range with different dates
      if (newStart.getTime() !== newEnd.getTime()) {
        setTimeout(() => setOpen(false), 100);
      }
    }
  };

  const handleClear = (event: React.MouseEvent) => {
    event.preventDefault();
    event.stopPropagation();
    onChange?.(null);
  };

  const formatDateRange = () => {
    if (!value?.start || !value?.end) {
      return placeholder;
    }
    return `${format(value.start, "PP", { locale: dateLocale })} - ${format(value.end, "PP", { locale: dateLocale })}`;
  };

  const hasValue = value !== null && value !== undefined;

  return (
    <Field className={cn("flex flex-col", className)}>
      {label && <FieldLabel>{label}</FieldLabel>}
      <div className="relative">
        <Popover open={open} onOpenChange={handleOpenChange}>
          <PopoverTrigger
            render={
              <Button
                variant="outline"
                // NOTE: This diverges from stock ShadCN to prevent hover background change on the trigger button.
                className={cn(
                  "w-full min-w-40 justify-between border border-input font-normal hover:bg-white dark:hover:bg-input/30",
                  hasValue && "pr-9"
                )}
                disabled={disabled}
              >
                <div className={cn("flex items-center gap-2", !hasValue && "text-muted-foreground")}>
                  <CalendarIcon />
                  <span className="flex-1 text-right">{formatDateRange()}</span>
                </div>
              </Button>
            }
          />
          <PopoverContent className="w-auto overflow-hidden p-0" align="start">
            <Calendar
              mode="range"
              selected={dateRange}
              onDayClick={handleDayClick}
              numberOfMonths={1}
              defaultMonth={value?.start}
            />
          </PopoverContent>
        </Popover>
        {hasValue && (
          <Button
            variant="ghost"
            size="icon-xs"
            className="absolute top-1/2 right-1 -translate-y-1/2"
            onClick={handleClear}
            disabled={disabled}
            aria-label="Clear dates"
          >
            <XIcon className="size-5" />
          </Button>
        )}
      </div>
    </Field>
  );
}

export function parseDateString(dateString: string): Date {
  return new Date(dateString);
}

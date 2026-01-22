import { format } from "date-fns";
import { CalendarIcon, XIcon } from "lucide-react";
import { useState } from "react";
import type { DateRange } from "react-day-picker";
import { cn } from "../utils";
import { Button } from "./Button";
import { Calendar } from "./Calendar";
import { Field, FieldLabel } from "./Field";
import { Popover, PopoverContent, PopoverTrigger } from "./Popover";

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
  const [open, setOpen] = useState(false);

  const dateRange: DateRange | undefined = value ? { from: value.start, to: value.end } : undefined;

  const handleSelect = (range: DateRange | undefined) => {
    if (range?.from && range?.to) {
      onChange?.({ start: range.from, end: range.to });
      // Only close if start and end are different dates
      if (range.from.getTime() !== range.to.getTime()) {
        setTimeout(() => setOpen(false), 100);
      }
    } else if (!range?.from && !range?.to) {
      onChange?.(null);
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
    return `${format(value.start, "MMM dd, yyyy")} - ${format(value.end, "MMM dd, yyyy")}`;
  };

  const hasValue = value !== null && value !== undefined;

  return (
    <Field className={cn("flex flex-col", className)}>
      {label && <FieldLabel>{label}</FieldLabel>}
      <div className="relative">
        <Popover open={open} onOpenChange={setOpen}>
          <PopoverTrigger
            render={
              <Button
                variant="outline"
                className={cn(
                  "w-full min-w-40 justify-between border border-input font-normal hover:bg-white dark:hover:bg-input/30",
                  hasValue && "pr-8"
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
            <Calendar mode="range" selected={dateRange} onSelect={handleSelect} numberOfMonths={1} />
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
            <XIcon className="size-4" />
          </Button>
        )}
      </div>
    </Field>
  );
}

export function parseDateString(dateString: string): Date {
  return new Date(dateString);
}

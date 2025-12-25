import { format, parseISO } from "date-fns";
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
    } else if (!range?.from && !range?.to) {
      onChange?.(null);
    }
  };

  const handleClear = (event: React.MouseEvent) => {
    event.stopPropagation();
    onChange?.(null);
  };

  const formatDateRange = () => {
    if (!value) {
      return "";
    }
    return `${format(value.start, "yyyy-MM-dd")} - ${format(value.end, "yyyy-MM-dd")}`;
  };

  const hasValue = value !== null && value !== undefined;

  return (
    <Field className={cn("flex flex-col gap-1", className)}>
      {label && <FieldLabel>{label}</FieldLabel>}
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger
          render={
            <Button
              variant="outline"
              className={cn(
                "flex h-10 items-center justify-between gap-2 font-normal",
                hasValue ? "min-w-[240px]" : "min-w-[180px]"
              )}
              disabled={disabled}
            >
              <div className="flex items-center gap-2 truncate">
                <CalendarIcon className="size-5 shrink-0" />
                <span className="truncate text-sm">{hasValue ? formatDateRange() : placeholder}</span>
              </div>
              {hasValue && <XIcon className="size-5 shrink-0 cursor-pointer" onClick={handleClear} />}
            </Button>
          }
        />
        <PopoverContent className="w-auto overflow-hidden p-0" align="start">
          <Calendar mode="range" selected={dateRange} onSelect={handleSelect} numberOfMonths={2} />
        </PopoverContent>
      </Popover>
    </Field>
  );
}

export function parseDateString(dateString: string): Date {
  return parseISO(dateString);
}

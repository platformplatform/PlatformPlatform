/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/index.html?path=/docs/daterangepicker--docs
 */
import { CalendarIcon, XIcon } from "lucide-react";
import { useEffect, useState } from "react";

import {
  DateRangePicker as AriaDateRangePicker,
  type DateRangePickerProps as AriaDateRangePickerProps,
  type DateValue,
  I18nProvider,
  type ValidationResult
} from "react-aria-components";
import { Button } from "./Button";
import { DateInput } from "./DateField";
import { Description } from "./Description";
import { Dialog } from "./Dialog";
import { FieldGroup } from "./Field";
import { FieldError } from "./FieldError";
import { Label } from "./Label";
import { Popover } from "./Popover";
import { RangeCalendar } from "./RangeCalendar";
import { composeTailwindRenderProps } from "./utils";

export interface DateRangePickerProps<T extends DateValue> extends AriaDateRangePickerProps<T> {
  label?: string;
  description?: string;
  errorMessage?: string | ((validation: ValidationResult) => string);
  placeholder?: string;
}

export function DateRangePicker<T extends DateValue>({
  label,
  description,
  errorMessage,
  value,
  onChange,
  placeholder = "Select dates",
  ...props
}: Readonly<DateRangePickerProps<T>>) {
  const [isExpanded, setIsExpanded] = useState(false);
  const hasValue = value !== null && value !== undefined;

  // Automatically expand when there's a value
  useEffect(() => {
    if (hasValue) {
      setIsExpanded(true);
    }
  }, [hasValue]);

  // Format date range for compact display
  const formatDateRange = () => {
    if (!value) {
      return "";
    }

    const formatDate = (date: DateValue) => {
      return date.toString().split("T")[0];
    };

    return `${formatDate(value.start)} - ${formatDate(value.end)}`;
  };

  // Clear the date range
  const clearDateRange = (e?: React.MouseEvent) => {
    if (e) {
      e.stopPropagation();
    }
    onChange?.(null);
    setIsExpanded(false);
  };

  return (
    // Using Canadian locale to force YYYY-MM-DD format which is unambiguous internationally
    // This avoids confusion between MM/DD/YYYY (US) and DD/MM/YYYY (EU) formats
    <I18nProvider locale="en-CA">
      <AriaDateRangePicker
        {...props}
        value={value}
        onChange={(newValue) => {
          onChange?.(newValue);
          // Keep expanded if a value is selected
          if (!newValue) {
            setIsExpanded(false);
          }
        }}
        className={composeTailwindRenderProps(props.className, "group flex flex-col gap-1")}
      >
        {label && <Label>{label}</Label>}

        {isExpanded ? (
          // Expanded view - standard date range picker
          <FieldGroup className="w-auto min-w-[265px]">
            <DateInput slot="start" className="px-2 py-1.5 text-sm" />
            <span
              aria-hidden="true"
              className="text-foreground group-disabled:text-muted forced-colors:text-[ButtonText] group-disabled:forced-colors:text-[GrayText]"
            >
              –
            </span>
            <DateInput slot="end" className="flex-1 px-2 py-1.5 text-sm" />
            {value && (
              <Button variant="icon" className="mr-1 w-6 group-empty:invisible" onPress={() => onChange?.(null)}>
                <XIcon aria-hidden={true} className="h-4 w-4" />
              </Button>
            )}
            <Button variant="icon" className="h-6 w-6 rounded-sm outline-offset-0">
              <CalendarIcon aria-hidden={true} className="h-4 w-4" />
            </Button>
          </FieldGroup>
        ) : (
          // Compact view - just a button with calendar icon
          <Button
            variant="outline"
            className={`flex h-10 items-center justify-between gap-2 border border-input bg-input-background px-3 py-2 text-foreground hover:bg-accent hover:text-accent-foreground ${
              hasValue ? "w-full min-w-[240px]" : "w-full min-w-[180px]"
            }`}
            onPress={() => setIsExpanded(true)}
          >
            <div className="flex items-center gap-2 truncate">
              <CalendarIcon className="h-5 w-5 flex-shrink-0" />
              <span className="truncate text-sm">{hasValue ? formatDateRange() : placeholder}</span>
            </div>
            {hasValue && <XIcon className="h-5 w-5 flex-shrink-0 cursor-pointer" onClick={clearDateRange} />}
          </Button>
        )}

        {description && <Description>{description}</Description>}
        <FieldError>{errorMessage}</FieldError>
        <Popover>
          <Dialog>
            <RangeCalendar />
          </Dialog>
        </Popover>
      </AriaDateRangePicker>
    </I18nProvider>
  );
}

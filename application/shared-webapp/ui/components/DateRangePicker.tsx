/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/index.html?path=/docs/daterangepicker--docs
 */
import { CalendarIcon, XIcon } from "lucide-react";

import {
  DateRangePicker as AriaDateRangePicker,
  type DateRangePickerProps as AriaDateRangePickerProps,
  type DateValue,
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
}

export function DateRangePicker<T extends DateValue>({
  label,
  description,
  errorMessage,
  value,
  onChange,
  ...props
}: Readonly<DateRangePickerProps<T>>) {
  return (
    <AriaDateRangePicker
      {...props}
      value={value}
      onChange={onChange}
      className={composeTailwindRenderProps(props.className, "group flex flex-col gap-1")}
    >
      {label && <Label>{label}</Label>}
      <FieldGroup className="w-auto min-w-[208px]">
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
            <XIcon aria-hidden className="h-4 w-4" />
          </Button>
        )}
        <Button variant="icon" className="h-6 w-6 rounded-sm outline-offset-0">
          <CalendarIcon aria-hidden className="h-4 w-4" />
        </Button>
      </FieldGroup>
      {description && <Description>{description}</Description>}
      <FieldError>{errorMessage}</FieldError>
      <Popover>
        <Dialog>
          <RangeCalendar />
        </Dialog>
      </Popover>
    </AriaDateRangePicker>
  );
}

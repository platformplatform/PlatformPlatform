/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/index.html?path=/docs/datepicker--docs
 * ref: https://ui.shadcn.com/docs/components/date-picker
 */
import { CalendarIcon } from "lucide-react";
import {
  DatePicker as AriaDatePicker,
  type DatePickerProps as AriaDatePickerProps,
  type DateValue,
  I18nProvider,
  type ValidationResult
} from "react-aria-components";
import { Button } from "./Button";
import { Calendar } from "./Calendar";
import { DateInput } from "./DateField";
import { Description } from "./Description";
import { Dialog } from "./Dialog";
import { FieldGroup } from "./Field";
import { FieldError } from "./FieldError";
import { LabelWithTooltip } from "./LabelWithTooltip";
import { Popover } from "./Popover";
import { composeTailwindRenderProps } from "./utils";

export interface DatePickerProps<T extends DateValue> extends AriaDatePickerProps<T> {
  label?: string;
  description?: string;
  errorMessage?: string | ((validation: ValidationResult) => string);
  tooltip?: string;
}

export function DatePicker<T extends DateValue>({
  label,
  description,
  errorMessage,
  tooltip,
  ...props
}: Readonly<DatePickerProps<T>>) {
  return (
    // Using Canadian locale to force YYYY-MM-DD format which is unambiguous internationally
    // This avoids confusion between MM/DD/YYYY (US) and DD/MM/YYYY (EU) formats
    <I18nProvider locale="en-CA">
      <AriaDatePicker {...props} className={composeTailwindRenderProps(props.className, "group flex flex-col gap-3")}>
        {label && <LabelWithTooltip tooltip={tooltip}>{label}</LabelWithTooltip>}
        <FieldGroup className="w-auto min-w-[208px] bg-input-background">
          <DateInput className="min-w-[150px] flex-1 px-2 py-1.5 text-sm" />
          <Button variant="ghost" size="icon" className="mr-1 h-6 w-6 rounded-sm outline-offset-0">
            <CalendarIcon aria-hidden={true} className="h-4 w-4" />
          </Button>
        </FieldGroup>
        {description && <Description>{description}</Description>}
        <FieldError>{errorMessage}</FieldError>
        <Popover className="bg-input-background">
          <Dialog>
            <Calendar />
          </Dialog>
        </Popover>
      </AriaDatePicker>
    </I18nProvider>
  );
}

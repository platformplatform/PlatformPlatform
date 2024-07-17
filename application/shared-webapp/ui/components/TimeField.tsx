/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/timefield--docs
 */
import {
  TimeField as AriaTimeField,
  type TimeFieldProps as AriaTimeFieldProps,
  type TimeValue,
  type ValidationResult
} from "react-aria-components";
import { DateInput } from "./DateField";
import { Description } from "./Description";
import { FieldError } from "./FieldError";
import { Label } from "./Label";

export interface TimeFieldProps<T extends TimeValue> extends AriaTimeFieldProps<T> {
  label?: string;
  description?: string;
  errorMessage?: string | ((validation: ValidationResult) => string);
}

export function TimeField<T extends TimeValue>({
  label,
  description,
  errorMessage,
  ...props
}: Readonly<TimeFieldProps<T>>) {
  return (
    <AriaTimeField {...props}>
      <Label>{label}</Label>
      <DateInput />
      {description && <Description>{description}</Description>}
      <FieldError>{errorMessage}</FieldError>
    </AriaTimeField>
  );
}

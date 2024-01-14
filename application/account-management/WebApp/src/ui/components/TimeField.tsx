import type {
  TimeFieldProps as AriaTimeFieldProps,
  TimeValue,
  ValidationResult,
} from "react-aria-components";
import {
  TimeField as AriaTimeField,
} from "react-aria-components";
import { DateInput } from "./DateField";
import { Description, FieldError, Label } from "./Field";

export interface TimeFieldProps<T extends TimeValue>
  extends AriaTimeFieldProps<T> {
  label?: string;
  description?: string;
  errorMessage?: string | ((validation: ValidationResult) => string);
}

export function TimeField<T extends TimeValue>(
  { label, description, errorMessage, ...props }: Readonly<TimeFieldProps<T>>
) {
  return (
    <AriaTimeField {...props}>
      <Label>{label}</Label>
      <DateInput />
      {description && <Description>{description}</Description>}
      <FieldError>{errorMessage}</FieldError>
    </AriaTimeField>
  );
}

/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/index.html?path=/docs/datefield--docs
 */
import {
  DateField as AriaDateField,
  type DateFieldProps as AriaDateFieldProps,
  DateInput as AriaDateInput,
  type DateInputProps,
  DateSegment,
  type DateValue,
  type ValidationResult
} from "react-aria-components";
import { tv } from "tailwind-variants";
import { Description } from "./Description";
import { fieldGroupStyles } from "./Field";
import { FieldError } from "./FieldError";
import { Label } from "./Label";
import { composeTailwindRenderProps } from "./utils";

export interface DateFieldProps<T extends DateValue> extends AriaDateFieldProps<T> {
  label?: string;
  description?: string;
  errorMessage?: string | ((validation: ValidationResult) => string);
}

export function DateField<T extends DateValue>({
  label,
  description,
  errorMessage,
  ...props
}: Readonly<DateFieldProps<T>>) {
  return (
    <AriaDateField {...props} className={composeTailwindRenderProps(props.className, "flex flex-col gap-1")}>
      {label && <Label>{label}</Label>}
      <DateInput />
      {description && <Description>{description}</Description>}
      <FieldError>{errorMessage}</FieldError>
    </AriaDateField>
  );
}

const segmentStyles = tv({
  base: "inline p-0.5 type-literal:px-0 rounded-md outline outline-0 forced-color-adjust-none caret-transparent text-foreground text-sm forced-colors:text-[ButtonText]",
  variants: {
    isPlaceholder: {
      true: "text-muted-foreground italic"
    },
    isDisabled: {
      true: "opacity-50 cursor-not-allowed forced-colors:text-[GrayText]"
    },
    isFocused: {
      true: "bg-accent text-accent-foreground forced-colors:bg-[Highlight] forced-colors:text-[HighlightText]"
    }
  }
});

export function DateInput(props: Readonly<Omit<DateInputProps, "children">>) {
  return (
    <AriaDateInput
      className={(renderProps) =>
        fieldGroupStyles({ ...renderProps, class: "block min-w-[150px] px-2 py-1.5 text-sm" })
      }
      {...props}
    >
      {(segment) => <DateSegment segment={segment} className={segmentStyles} />}
    </AriaDateInput>
  );
}

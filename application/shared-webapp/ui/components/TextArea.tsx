import type { RefAttributes } from "react";
/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/textfield--docs
 */
import {
  TextArea as AriaTextArea,
  TextField as AriaTextField,
  type TextFieldProps as AriaTextFieldProps,
  type ValidationResult
} from "react-aria-components";
import { tv } from "tailwind-variants";
import { Description } from "./Description";
import { fieldBorderStyles } from "./Field";
import { FieldError } from "./FieldError";
import { Label } from "./Label";
import { focusRing } from "./focusRing";
import { composeTailwindRenderProps } from "./utils";

const textAreaStyles = tv({
  extend: focusRing,
  base: "h-auto resize-y rounded-md border bg-background px-2 py-1.5 text-foreground text-sm placeholder:text-muted-foreground",
  variants: {
    isFocused: fieldBorderStyles.variants.isFocusWithin,
    isInvalid: {
      true: "border-destructive",
      false: "border-input"
    },
    isDisabled: {
      true: "cursor-not-allowed opacity-50"
    }
  }
});

export interface TextAreaProps
  extends AriaTextFieldProps,
    Partial<Pick<HTMLInputElement, "autocomplete" | "placeholder">>,
    RefAttributes<HTMLInputElement> {
  label?: string;
  description?: string;
  errorMessage?: string | ((validation: ValidationResult) => string);
  rows?: number;
}

export function TextArea({ label, description, errorMessage, className, rows, ...props }: Readonly<TextAreaProps>) {
  if (props.children) {
    return <AriaTextField {...props} className={composeTailwindRenderProps(className, "flex flex-col gap-1")} />;
  }
  return (
    <AriaTextField {...props} className={composeTailwindRenderProps(className, "flex flex-col gap-1")}>
      {label && <Label>{label}</Label>}
      <AriaTextArea name={props.name} className={textAreaStyles} rows={rows} />
      {description && <Description>{description}</Description>}
      <FieldError>{errorMessage}</FieldError>
    </AriaTextField>
  );
}
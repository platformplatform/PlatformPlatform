import type { Attributes, HtmlHTMLAttributes } from "react";
/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/textfield--docs
 */
import {
  TextField as AriaTextField,
  type TextFieldProps as AriaTextFieldProps,
  type ValidationResult
} from "react-aria-components";
import { tv } from "tailwind-variants";
import { Description } from "./Description";
import { fieldBorderStyles } from "./Field";
import { FieldError } from "./FieldError";
import { Input } from "./Input";
import { Label } from "./Label";
import { focusRing } from "./focusRing";
import { composeTailwindRenderProps } from "./utils";

const inputStyles = tv({
  extend: focusRing,
  base: "border rounded-md",
  variants: {
    isFocused: fieldBorderStyles.variants.isFocusWithin,
    ...fieldBorderStyles.variants
  }
});

export interface TextFieldProps
  extends AriaTextFieldProps,
    Partial<Pick<HTMLInputElement, "autocomplete" | "placeholder">> {
  label?: string;
  description?: string;
  errorMessage?: string | ((validation: ValidationResult) => string);
}

export function TextField({
  label,
  description,
  errorMessage,
  placeholder,
  autocomplete,
  children,
  className,
  ...props
}: Readonly<TextFieldProps>) {
  if (children) {
    return (
      <AriaTextField {...props} className={composeTailwindRenderProps(className, "flex flex-col gap-1")}>
        {children}
      </AriaTextField>
    );
  }
  return (
    <AriaTextField {...props} className={composeTailwindRenderProps(className, "flex flex-col gap-1")}>
      {label && <Label>{label}</Label>}
      <Input className={inputStyles} placeholder={placeholder} autoComplete={autocomplete} />
      {description && <Description>{description}</Description>}
      <FieldError>{errorMessage}</FieldError>
    </AriaTextField>
  );
}

import type { RefAttributes } from "react";
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
  variants: {
    isFocused: fieldBorderStyles.variants.isFocusWithin,
    ...fieldBorderStyles.variants
  }
});

export interface TextFieldProps
  extends AriaTextFieldProps,
    Partial<Pick<HTMLInputElement, "autocomplete" | "placeholder">>,
    RefAttributes<HTMLInputElement> {
  label?: string;
  description?: string;
  errorMessage?: string | ((validation: ValidationResult) => string);
  isDisabled?: boolean;
  isReadOnly?: boolean;
  inputClassName?: string;
  startIcon?: React.ReactNode;
}

export function TextField({
  label,
  description,
  errorMessage,
  className,
  isDisabled,
  isReadOnly,
  inputClassName,
  startIcon,
  ...props
}: Readonly<TextFieldProps>) {
  if (props.children) {
    return <AriaTextField {...props} className={composeTailwindRenderProps(className, "flex flex-col gap-1")} />;
  }

  return (
    <AriaTextField {...props} className={composeTailwindRenderProps(className, "flex flex-col gap-1")}>
      {label && <Label>{label}</Label>}
      <Input
        name={props.name}
        className={`${inputStyles} ${inputClassName || ""}`}
        isDisabled={isDisabled}
        isReadOnly={isReadOnly}
        startIcon={startIcon}
      />
      {description && <Description>{description}</Description>}
      <FieldError>{errorMessage}</FieldError>
    </AriaTextField>
  );
}

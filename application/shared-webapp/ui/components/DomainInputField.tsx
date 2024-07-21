import {
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
import { useContext } from "react";
import { Group } from "./Group";
import { Input } from "./Input";
import { FormValidationContext } from "react-aria-components";
import { useFocusRing } from "react-aria";

const inputStyles = tv({
  extend: focusRing,
  base: "h-10 border overflow-hidden",
  variants: {
    isFocused: fieldBorderStyles.variants.isFocusWithin,
    ...fieldBorderStyles.variants
  }
});

export interface DomainInputFieldProps
  extends AriaTextFieldProps,
    Partial<Pick<HTMLInputElement, "autocomplete" | "placeholder">> {
  domain: string;
  label?: string;
  description?: string;
  errorMessage?: string | ((validation: ValidationResult) => string);
}

export function DomainInputField({
  name,
  domain,
  label,
  description,
  errorMessage,
  placeholder,
  autocomplete,
  children,
  className,
  ...props
}: Readonly<DomainInputFieldProps>) {
  const errors = useContext(FormValidationContext);
  const isInvalid = Boolean(name != null && name in errors ? errors?.[name] : undefined);
  const { focusProps, isFocusVisible } = useFocusRing();
  return (
    <AriaTextField {...props} name={name} className={composeTailwindRenderProps(className, "flex flex-col gap-1")}>
      {label && <Label>{label}</Label>}
      <Group className={inputStyles({ isInvalid, isFocusVisible })}>
        <Input {...focusProps} isEmbedded placeholder={placeholder} autoComplete={autocomplete} />
        <div className="min-w-fit max-w-fit shrink-0 text-xs flex items-center p-2 text-muted-foreground">{domain}</div>
      </Group>
      {description && <Description>{description}</Description>}
      <FieldError>{errorMessage}</FieldError>
    </AriaTextField>
  );
}

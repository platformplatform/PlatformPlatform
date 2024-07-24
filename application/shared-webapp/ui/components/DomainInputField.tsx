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
import { CheckIcon, TriangleAlertIcon } from "lucide-react";

const inputStyles = tv({
  extend: focusRing,
  base: "flex h-10 border",
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
  isSubdomainFree?: boolean | null;
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
  isSubdomainFree,
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
        <div className="flex grow overflow-hidden">
          <Input
            {...focusProps}
            isEmbedded
            placeholder={placeholder}
            autoComplete={autocomplete}
            autoCorrect="off"
            className="h-full w-full"
          />
        </div>
        <div className="w-fit gap-1 text-xs flex items-center p-1 text-muted-foreground">
          {domain}
          <AvailabilityIcon isAvailable={isSubdomainFree} />
        </div>
      </Group>
      {description && <Description>{description}</Description>}
      <FieldError>{errorMessage}</FieldError>
    </AriaTextField>
  );
}

type AvailabilityIconProps = {
  isAvailable?: boolean | null;
};

function AvailabilityIcon({ isAvailable }: Readonly<AvailabilityIconProps>) {
  if (isAvailable === false) return <TriangleAlertIcon className="h-4 w-4 stroke-danger" />;
  if (isAvailable === true) return <CheckIcon className="h-4 w-4 stroke-success" />;

  return null;
}

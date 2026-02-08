import { useContext } from "react";
import { cn } from "../utils";
import { Field, FieldDescription, FieldError, FieldLabel } from "./Field";
import { FormValidationContext } from "./Form";
import { Input } from "./Input";
import { InputGroup, InputGroupAddon, InputGroupInput } from "./InputGroup";
import { LabelWithTooltip } from "./LabelWithTooltip";

export interface TextFieldProps extends Omit<React.ComponentProps<"input">, "className" | "onChange"> {
  label?: string;
  description?: string;
  errorMessage?: string;
  tooltip?: string;
  className?: string;
  inputClassName?: string;
  startIcon?: React.ReactNode;
  onChange?: (value: string) => void;
  isRequired?: boolean;
  isDisabled?: boolean;
  isReadOnly?: boolean;
}

export function TextField({
  label,
  description,
  errorMessage,
  tooltip,
  className,
  inputClassName,
  startIcon,
  name,
  type,
  value,
  onChange,
  autoFocus,
  isRequired,
  isDisabled,
  isReadOnly,
  ...props
}: Readonly<TextFieldProps>) {
  const formErrors = useContext(FormValidationContext);
  const fieldValidationErrors = name && formErrors && name in formErrors ? formErrors[name] : undefined;
  const fieldErrorMessages = fieldValidationErrors
    ? Array.isArray(fieldValidationErrors)
      ? fieldValidationErrors
      : [fieldValidationErrors]
    : [];
  const errors = errorMessage
    ? [{ message: errorMessage }]
    : fieldErrorMessages.length > 0
      ? fieldErrorMessages.map((err) => ({ message: err }))
      : undefined;
  const isInvalid = errors && errors.length > 0;

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onChange?.(e.target.value);
  };

  const inputProps = {
    id: name,
    name,
    type,
    value,
    onChange: handleChange,
    autoFocus,
    required: isRequired,
    disabled: isDisabled,
    readOnly: isReadOnly,
    "aria-invalid": isInvalid || undefined,
    ...props
  };

  return (
    <Field className={cn("flex flex-col", className)}>
      {label && (
        <FieldLabel htmlFor={name}>
          {tooltip ? <LabelWithTooltip tooltip={tooltip}>{label}</LabelWithTooltip> : label}
        </FieldLabel>
      )}
      {startIcon ? (
        <InputGroup>
          <InputGroupAddon>{startIcon}</InputGroupAddon>
          <InputGroupInput className={inputClassName} {...inputProps} />
        </InputGroup>
      ) : (
        <Input className={inputClassName} {...inputProps} />
      )}
      {description && <FieldDescription>{description}</FieldDescription>}
      <FieldError errors={errors} />
    </Field>
  );
}

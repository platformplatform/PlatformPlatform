import { useContext } from "react";
import { FormValidationContext } from "react-aria-components";
import { cn } from "../utils";
import { Field, FieldDescription, FieldError, FieldLabel } from "./Field";
import { Input } from "./Input";
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

  return (
    <Field className={cn("flex flex-col gap-1", className)}>
      {label && (
        <FieldLabel htmlFor={name}>
          {tooltip ? <LabelWithTooltip tooltip={tooltip}>{label}</LabelWithTooltip> : label}
        </FieldLabel>
      )}
      {startIcon ? (
        <div className="relative">
          <div className="pointer-events-none absolute top-1/2 left-3 flex -translate-y-1/2 items-center">
            {startIcon}
          </div>
          <Input
            id={name}
            name={name}
            type={type}
            value={value}
            onChange={handleChange}
            autoFocus={autoFocus}
            required={isRequired}
            disabled={isDisabled}
            readOnly={isReadOnly}
            aria-invalid={isInvalid || undefined}
            className={cn(startIcon && "pl-9", inputClassName)}
            {...props}
          />
        </div>
      ) : (
        <Input
          id={name}
          name={name}
          type={type}
          value={value}
          onChange={handleChange}
          autoFocus={autoFocus}
          required={isRequired}
          disabled={isDisabled}
          readOnly={isReadOnly}
          aria-invalid={isInvalid || undefined}
          className={inputClassName}
          {...props}
        />
      )}
      {description && <FieldDescription>{description}</FieldDescription>}
      <FieldError errors={errors} />
    </Field>
  );
}

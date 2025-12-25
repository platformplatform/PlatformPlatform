import { ChevronDown, ChevronUp } from "lucide-react";
import { useContext, useRef } from "react";
import { cn } from "../utils";
import { Field, FieldDescription, FieldError, FieldLabel } from "./Field";
import { FormValidationContext } from "./Form";
import { InputGroup, InputGroupButton, InputGroupInput } from "./InputGroup";
import { LabelWithTooltip } from "./LabelWithTooltip";

export interface NumberFieldProps {
  label?: string;
  description?: string;
  errorMessage?: string;
  tooltip?: string;
  className?: string;
  name?: string;
  value?: number;
  defaultValue?: number;
  onChange?: (value: number) => void;
  minValue?: number;
  maxValue?: number;
  step?: number;
  autoFocus?: boolean;
  isDisabled?: boolean;
  isRequired?: boolean;
  isReadOnly?: boolean;
}

export function NumberField({
  label,
  description,
  errorMessage,
  tooltip,
  className,
  name,
  value,
  defaultValue,
  onChange,
  minValue,
  maxValue,
  step = 1,
  autoFocus,
  isDisabled,
  isRequired,
  isReadOnly
}: Readonly<NumberFieldProps>) {
  const inputRef = useRef<HTMLInputElement>(null);
  const formErrors = useContext(FormValidationContext);
  const contextErrors = name != null && name in formErrors ? formErrors[name] : undefined;
  const errorMessages = errorMessage ? [errorMessage] : contextErrors;
  const errors = errorMessages
    ? (Array.isArray(errorMessages) ? errorMessages : [errorMessages]).map((msg) => ({ message: msg }))
    : undefined;

  const getCurrentValue = (): number => {
    if (inputRef.current) {
      const parsed = Number.parseFloat(inputRef.current.value);
      return Number.isNaN(parsed) ? (defaultValue ?? minValue ?? 0) : parsed;
    }
    return value ?? defaultValue ?? minValue ?? 0;
  };

  const clampValue = (val: number): number => {
    let clamped = val;
    if (minValue !== undefined && clamped < minValue) {
      clamped = minValue;
    }
    if (maxValue !== undefined && clamped > maxValue) {
      clamped = maxValue;
    }
    return clamped;
  };

  const handleIncrement = () => {
    if (isDisabled || isReadOnly) {
      return;
    }
    const newValue = clampValue(getCurrentValue() + step);
    if (inputRef.current) {
      inputRef.current.value = String(newValue);
      inputRef.current.dispatchEvent(new Event("input", { bubbles: true }));
    }
    onChange?.(newValue);
  };

  const handleDecrement = () => {
    if (isDisabled || isReadOnly) {
      return;
    }
    const newValue = clampValue(getCurrentValue() - step);
    if (inputRef.current) {
      inputRef.current.value = String(newValue);
      inputRef.current.dispatchEvent(new Event("input", { bubbles: true }));
    }
    onChange?.(newValue);
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const parsed = Number.parseFloat(e.target.value);
    if (!Number.isNaN(parsed)) {
      onChange?.(clampValue(parsed));
    }
  };

  const handleBlur = (e: React.FocusEvent<HTMLInputElement>) => {
    const parsed = Number.parseFloat(e.target.value);
    if (!Number.isNaN(parsed)) {
      const clamped = clampValue(parsed);
      if (clamped !== parsed) {
        e.target.value = String(clamped);
        onChange?.(clamped);
      }
    }
  };

  const isAtMin = minValue !== undefined && getCurrentValue() <= minValue;
  const isAtMax = maxValue !== undefined && getCurrentValue() >= maxValue;

  return (
    <Field className={cn("flex flex-col", className)} data-disabled={isDisabled || undefined}>
      {label && (
        <FieldLabel htmlFor={name}>
          {tooltip ? <LabelWithTooltip tooltip={tooltip}>{label}</LabelWithTooltip> : label}
        </FieldLabel>
      )}
      <InputGroup data-disabled={isDisabled || undefined}>
        <InputGroupInput
          ref={inputRef}
          id={name}
          name={name}
          type="number"
          inputMode="numeric"
          defaultValue={defaultValue}
          value={value}
          onChange={handleChange}
          onBlur={handleBlur}
          autoFocus={autoFocus}
          disabled={isDisabled}
          required={isRequired}
          readOnly={isReadOnly}
          min={minValue}
          max={maxValue}
          step={step}
          className="[appearance:textfield] [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
        />
        <div className="flex flex-col border-input border-l">
          <InputGroupButton
            variant="ghost"
            size="icon-xs"
            className="h-[calc(50%-0.5px)] rounded-none rounded-tr-md border-input border-b px-1.5"
            onClick={handleIncrement}
            disabled={isDisabled || isReadOnly || isAtMax}
            aria-label="Increment"
          >
            <ChevronUp aria-hidden={true} className="h-3 w-3" />
          </InputGroupButton>
          <InputGroupButton
            variant="ghost"
            size="icon-xs"
            className="h-[calc(50%-0.5px)] rounded-none rounded-br-md px-1.5"
            onClick={handleDecrement}
            disabled={isDisabled || isReadOnly || isAtMin}
            aria-label="Decrement"
          >
            <ChevronDown aria-hidden={true} className="h-3 w-3" />
          </InputGroupButton>
        </div>
      </InputGroup>
      {description && <FieldDescription>{description}</FieldDescription>}
      <FieldError errors={errors} />
    </Field>
  );
}

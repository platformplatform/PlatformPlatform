import { SearchIcon, XIcon } from "lucide-react";
import { useRef } from "react";
import { cn } from "../utils";
import { Field, FieldDescription, FieldError, FieldLabel } from "./Field";
import { InputGroup, InputGroupAddon, InputGroupButton, InputGroupInput } from "./InputGroup";
import { LabelWithTooltip } from "./LabelWithTooltip";

export interface SearchFieldProps {
  label?: string;
  description?: string;
  errorMessage?: string;
  tooltip?: string;
  placeholder?: string;
  className?: string;
  name?: string;
  value?: string;
  onChange?: (value: string) => void;
  autoFocus?: boolean;
  disabled?: boolean;
}

export function SearchField({
  label,
  description,
  errorMessage,
  tooltip,
  placeholder,
  className,
  name,
  value,
  onChange,
  autoFocus,
  disabled
}: Readonly<SearchFieldProps>) {
  const inputRef = useRef<HTMLInputElement>(null);
  const errors = errorMessage ? [{ message: errorMessage }] : undefined;
  const hasValue = value != null && value.length > 0;

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onChange?.(e.target.value);
  };

  const handleClear = (event?: React.MouseEvent) => {
    event?.preventDefault();
    event?.stopPropagation();
    onChange?.("");
    setTimeout(() => {
      inputRef.current?.focus();
    }, 0);
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Escape" && hasValue) {
      onChange?.(""); // Let native Escape behavior clear field, then sync to state
    }
  };

  return (
    <Field className={cn("flex min-w-[40px] flex-col", className)}>
      {label && (
        <FieldLabel htmlFor={name}>
          {tooltip ? <LabelWithTooltip tooltip={tooltip}>{label}</LabelWithTooltip> : label}
        </FieldLabel>
      )}
      <InputGroup>
        <InputGroupAddon>
          <SearchIcon aria-hidden={true} />
        </InputGroupAddon>
        <InputGroupInput
          ref={inputRef}
          id={name}
          name={name}
          type="search"
          value={value ?? ""}
          onChange={handleChange}
          onKeyDown={handleKeyDown}
          autoFocus={autoFocus}
          disabled={disabled}
          placeholder={placeholder}
          className="[&::-webkit-search-cancel-button]:hidden [&::-webkit-search-decoration]:hidden"
        />
        <InputGroupAddon align="inline-end" className={hasValue ? "" : "invisible"}>
          <InputGroupButton
            onClick={handleClear}
            disabled={disabled || !hasValue}
            aria-label="Clear search"
            size="icon-xs"
          >
            <XIcon className="size-5" aria-hidden={true} />
          </InputGroupButton>
        </InputGroupAddon>
      </InputGroup>
      {description && <FieldDescription>{description}</FieldDescription>}
      <FieldError errors={errors} />
    </Field>
  );
}

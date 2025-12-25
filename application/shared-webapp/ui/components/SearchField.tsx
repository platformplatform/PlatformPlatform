import { SearchIcon, XIcon } from "lucide-react";
import { cn } from "../utils";
import { Button } from "./Button";
import { Field, FieldDescription, FieldError, FieldLabel } from "./Field";
import { Input } from "./Input";
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
  const errors = errorMessage ? [{ message: errorMessage }] : undefined;
  const hasValue = value != null && value.length > 0;

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onChange?.(e.target.value);
  };

  const handleClear = () => {
    onChange?.("");
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Escape" && hasValue) {
      e.preventDefault();
      handleClear();
    }
  };

  return (
    <Field className={cn("flex min-w-[40px] flex-col gap-1", className)}>
      {label && (
        <FieldLabel htmlFor={name} className="sr-only">
          {tooltip ? <LabelWithTooltip tooltip={tooltip}>{label}</LabelWithTooltip> : label}
        </FieldLabel>
      )}
      <div className="relative">
        <div className="pointer-events-none absolute top-1/2 left-2.5 flex -translate-y-1/2 items-center">
          <SearchIcon aria-hidden={true} className="h-4 w-4 text-muted-foreground" />
        </div>
        <Input
          id={name}
          name={name}
          type="search"
          value={value}
          onChange={handleChange}
          onKeyDown={handleKeyDown}
          autoFocus={autoFocus}
          disabled={disabled}
          placeholder={placeholder}
          className="pr-8 pl-9 [&::-webkit-search-cancel-button]:hidden [&::-webkit-search-decoration]:hidden"
        />
        {hasValue && (
          <Button
            type="button"
            variant="ghost"
            size="icon-xs"
            className="absolute top-1/2 right-1 -translate-y-1/2"
            onClick={handleClear}
            disabled={disabled}
            aria-label="Clear search"
          >
            <XIcon aria-hidden={true} className="h-4 w-4" />
          </Button>
        )}
      </div>
      {description && <FieldDescription>{description}</FieldDescription>}
      <FieldError errors={errors} />
    </Field>
  );
}

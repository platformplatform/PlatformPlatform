import { CheckIcon, ChevronDownIcon } from "lucide-react";
import { type ReactNode, useCallback, useEffect, useRef, useState } from "react";

import { useFieldError } from "../hooks/useFieldError";
import { cn } from "../utils";
import { Field, FieldDescription, FieldError } from "./Field";
import { Label } from "./Label";
import { LabelWithTooltip } from "./LabelWithTooltip";
import { Popover, PopoverContent, PopoverTrigger } from "./Popover";

export interface MultiSelectItem {
  id: string;
  label: string;
  icon?: ReactNode;
}

export interface MultiSelectProps {
  name?: string;
  label?: string;
  description?: string;
  errorMessage?: string;
  tooltip?: React.ReactNode;
  placeholder?: string;
  emptyMessage?: ReactNode;
  startIcon?: ReactNode;
  items: MultiSelectItem[];
  value: string[];
  onChange: (value: string[]) => void;
  className?: string;
  disabled?: boolean;
  readOnly?: boolean;
}

export function MultiSelect({
  name,
  label,
  description,
  errorMessage,
  tooltip,
  placeholder,
  emptyMessage,
  startIcon,
  items,
  value,
  onChange,
  className,
  disabled,
  readOnly
}: MultiSelectProps) {
  const { errors, isInvalid, clearNow } = useFieldError({ name, errorMessage });
  const [open, setOpen] = useState(false);
  const listRef = useRef<HTMLDivElement>(null);
  const displayLabel = value.length > 0 ? `${value.length} selected` : placeholder;

  const handleToggle = useCallback(
    (itemId: string) => {
      if (readOnly) return;
      clearNow();
      if (value.includes(itemId)) {
        onChange(value.filter((v) => v !== itemId));
      } else {
        onChange([...value, itemId]);
      }
    },
    [value, onChange, readOnly, clearNow]
  );

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent, itemId: string) => {
      if (e.key === "Tab") {
        e.preventDefault();
        setOpen(false);
        document.getElementById(name ?? "")?.focus();
        return;
      }
      if (e.key === " " || e.key === "Enter") {
        e.preventDefault();
        handleToggle(itemId);
      } else if (e.key === "ArrowDown") {
        e.preventDefault();
        const next = (e.currentTarget as HTMLElement).nextElementSibling as HTMLElement | null;
        next?.focus();
      } else if (e.key === "ArrowUp") {
        e.preventDefault();
        const previous = (e.currentTarget as HTMLElement).previousElementSibling as HTMLElement | null;
        previous?.focus();
      } else if (e.key === "Escape") {
        e.preventDefault();
        setOpen(false);
        document.getElementById(name ?? "")?.focus();
      }
    },
    [handleToggle, name, setOpen]
  );

  const handleOpenChange = (isOpen: boolean) => {
    setOpen(isOpen);
  };

  useEffect(() => {
    if (!open) return;
    const timer = setTimeout(() => {
      const firstOption = listRef.current?.querySelector("[role=option]") as HTMLElement | null;
      firstOption?.focus();
    }, 50);
    return () => clearTimeout(timer);
  }, [open]);

  return (
    <Field className={cn("flex flex-col", className)}>
      {label && (
        <Label htmlFor={name} data-slot="field-label" className="cursor-default leading-snug">
          {tooltip ? <LabelWithTooltip tooltip={tooltip}>{label}</LabelWithTooltip> : label}
        </Label>
      )}
      {items.length === 0 ? (
        emptyMessage && <p className="text-sm text-muted-foreground">{emptyMessage}</p>
      ) : (
        <Popover open={readOnly ? false : open} onOpenChange={readOnly ? undefined : handleOpenChange}>
          <PopoverTrigger
            render={
              <button
                id={name}
                type="button"
                role="combobox"
                aria-label={label ?? placeholder}
                aria-controls={`${name}-listbox`}
                aria-expanded={open}
                aria-invalid={isInvalid || undefined}
                disabled={disabled}
                onKeyDown={(e: React.KeyboardEvent) => {
                  if (e.key === "ArrowDown" && !open) {
                    e.preventDefault();
                    handleOpenChange(true);
                  }
                }}
                className={cn(
                  "flex h-[var(--control-height)] w-full cursor-pointer items-center justify-between gap-1.5 rounded-md border border-input bg-white px-2.5 text-sm whitespace-nowrap shadow-xs outline-ring transition-[color,box-shadow] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 disabled:pointer-events-none disabled:opacity-50 aria-invalid:outline aria-invalid:outline-2 aria-invalid:outline-offset-2 aria-invalid:outline-destructive aria-invalid:focus-visible:shadow-error-halo dark:bg-input/30",
                  readOnly &&
                    "focus:outline focus:outline-2 focus:outline-offset-2 aria-invalid:focus:shadow-error-halo"
                )}
              />
            }
          >
            {startIcon && (
              <span
                className={cn(
                  "shrink-0 [&_svg:not([class*='size-'])]:size-4",
                  value.length === 0 && "text-muted-foreground"
                )}
              >
                {startIcon}
              </span>
            )}
            <span className={cn("flex-1 truncate text-left", value.length === 0 && "text-muted-foreground")}>
              {displayLabel}
            </span>
            <ChevronDownIcon className="size-4 shrink-0 opacity-50" />
          </PopoverTrigger>
          <PopoverContent className="min-w-(--anchor-width) p-1" align="start">
            <div
              id={`${name}-listbox`}
              ref={listRef}
              role="listbox"
              aria-multiselectable="true"
              className="flex flex-col"
            >
              {items.map((item) => {
                const checked = value.includes(item.id);
                return (
                  <div
                    key={item.id}
                    role="option"
                    aria-selected={checked}
                    tabIndex={-1}
                    onClick={() => handleToggle(item.id)}
                    onKeyDown={(e) => handleKeyDown(e, item.id)}
                    className={cn(
                      "relative flex cursor-pointer items-center gap-2 rounded-sm py-3 pr-8 pl-2 text-sm outline-hidden select-none hover:bg-accent focus:bg-accent active:bg-accent",
                      checked && "bg-accent"
                    )}
                  >
                    {item.icon && <span className="shrink-0 [&_svg:not([class*='size-'])]:size-4">{item.icon}</span>}
                    <span className="whitespace-nowrap">{item.label}</span>
                    {checked && (
                      <span className="pointer-events-none absolute right-2 flex size-4 items-center justify-center">
                        <CheckIcon className="size-4" />
                      </span>
                    )}
                  </div>
                );
              })}
            </div>
          </PopoverContent>
        </Popover>
      )}
      {description && <FieldDescription>{description}</FieldDescription>}
      <FieldError errors={errors} />
    </Field>
  );
}

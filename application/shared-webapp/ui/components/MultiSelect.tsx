/**
 * MultiSelect component for multiple selection dropdowns
 * Based on the Select component but optimized for multiple selection
 */
import { ChevronDown, XIcon } from "lucide-react";
import type React from "react";
import { useContext, useEffect, useRef, useState } from "react";
import {
  Button,
  DialogTrigger,
  FormValidationContext,
  type Key,
  ListBox,
  type ListBoxItemProps,
  type ListBoxProps,
  type Selection,
  type ValidationResult
} from "react-aria-components";
import { tv } from "tailwind-variants";
import { Description } from "./Description";
import { DropdownItem, DropdownSection, type DropdownSectionProps } from "./Dropdown";
import { FieldError } from "./FieldError";
import { focusRing } from "./focusRing";
import { Label } from "./Label";
import { Popover } from "./Popover";

const buttonStyles = tv({
  extend: focusRing,
  base: "flex h-10 w-full min-w-[150px] cursor-default items-center gap-4 rounded-md border border-input bg-input-background py-2 pr-2 pl-3 text-start text-foreground transition",
  variants: {
    isInvalid: {
      true: "border-destructive group-invalid:border-destructive forced-colors:group-invalid:border-[Mark]"
    },
    isDisabled: {
      false: "pressed:bg-active-background pressed:text-accent-foreground hover:bg-hover-background",
      true: "opacity-50 forced-colors:border-[GrayText] forced-colors:text-[GrayText]"
    }
  }
});

export type MultiSelectItemShape = {
  id: Key;
  label: string;
};

export interface MultiSelectProps<T extends MultiSelectItemShape> extends Omit<ListBoxProps<T>, "children"> {
  label?: string;
  description?: string;
  errorMessage?: string | ((validation: ValidationResult) => string);
  tooltip?: string;
  items: Iterable<T>;
  selectedKeys: Selection;
  onSelectionChange: (keys: Selection) => void;
  children: React.ReactNode | ((item: T) => React.ReactNode);
  className?: string;
  placeholder?: string;
  isReadOnly?: boolean;
  name?: string; // for validation context
}

export function MultiSelect<T extends MultiSelectItemShape>({
  items,
  selectedKeys,
  onSelectionChange,
  children,
  label,
  description,
  errorMessage,
  tooltip,
  className,
  placeholder = "Select options...",
  isReadOnly = false,
  name,
  ...listBoxProps
}: Readonly<MultiSelectProps<T>>) {
  const errors = useContext(FormValidationContext);
  const isInvalid = Boolean(name != null && name in errors ? errors?.[name] : undefined);

  const buttonRef = useRef<HTMLButtonElement>(null);
  const [popoverWidth, setPopoverWidth] = useState<number>();

  useEffect(() => {
    if (!buttonRef.current) {
      return;
    }
    const observer = new globalThis.ResizeObserver(() => {
      if (buttonRef.current) {
        setPopoverWidth(buttonRef.current.offsetWidth);
      }
    });
    observer.observe(buttonRef.current);
    setPopoverWidth(buttonRef.current.offsetWidth);
    return () => observer.disconnect();
  }, []);

  const itemsArr = items ? Array.from(items) : [];
  const hasItems = itemsArr.length > 0;

  return (
    <div className={`group flex flex-col gap-1 ${className || ""}`}>
      <DialogTrigger>
        {label && <Label tooltip={tooltip}>{label}</Label>}
        <Button
          ref={buttonRef}
          className={(renderProps) =>
            `${buttonStyles({ ...renderProps, isInvalid })} justify-between hover:bg-transparent`
          }
          isDisabled={isReadOnly || !hasItems}
        >
          <div className="flex min-h-[1.5rem] min-w-0 flex-1 items-center gap-1">
            <MultiSelectValueDisplay
              items={itemsArr}
              selectedKeys={selectedKeys}
              onRemove={(key) => {
                if (selectedKeys instanceof Set) {
                  const newSet = new Set(selectedKeys);
                  newSet.delete(key);
                  onSelectionChange(newSet);
                }
              }}
              placeholder={hasItems ? placeholder : "No options available"}
            />
          </div>
          {hasItems && (
            <ChevronDown
              aria-hidden={true}
              className="h-4 w-4 text-muted-foreground group-disabled:text-muted forced-colors:text-[ButtonText] forced-colors:group-disabled:text-[GrayText]"
            />
          )}
        </Button>
        {description && <Description>{description}</Description>}
        <FieldError>{errorMessage}</FieldError>
        {hasItems && (
          <Popover style={{ width: popoverWidth }}>
            <ListBox
              aria-label="listbox"
              items={items}
              selectionMode="multiple"
              selectedKeys={selectedKeys}
              onSelectionChange={onSelectionChange}
              className="max-h-[inherit] overflow-auto p-1 outline-none [clip-path:inset(0_0_0_0_round_.75rem)]"
              {...listBoxProps}
            >
              {children}
            </ListBox>
          </Popover>
        )}
      </DialogTrigger>
    </div>
  );
}

export function MultiSelectItem(props: Readonly<ListBoxItemProps>) {
  // Only wrap string children in the span for truncation
  return (
    <DropdownItem {...props}>
      {typeof props.children === "string" ? (
        <span className="block w-full overflow-hidden truncate whitespace-nowrap">{props.children}</span>
      ) : (
        props.children
      )}
    </DropdownItem>
  );
}

export function MultiSelectSection<T extends object>(props: Readonly<DropdownSectionProps<T>>) {
  return <DropdownSection {...props} />;
}

interface MultiSelectValueDisplayProps<T extends MultiSelectItemShape> {
  items: Iterable<T>;
  selectedKeys: Selection;
  onRemove: (key: Key) => void;
  placeholder?: string;
}

export function MultiSelectValueDisplay<T extends MultiSelectItemShape>({
  items,
  selectedKeys,
  onRemove,
  placeholder
}: Readonly<MultiSelectValueDisplayProps<T>>) {
  const itemsArr = Array.isArray(items) ? items : Array.from(items);
  const selectedItems = itemsArr.filter((item) => (selectedKeys instanceof Set ? selectedKeys.has(item.id) : false));

  if (selectedItems.length === 0) {
    return <span className="text-muted-foreground text-sm">{placeholder}</span>;
  }

  return (
    <div className="flex min-w-0 items-center gap-1">
      <span className="flex min-w-0 flex-shrink items-center gap-1 rounded bg-accent px-2 py-1 text-accent-foreground text-xs">
        <span className="block w-full overflow-hidden truncate whitespace-nowrap">{selectedItems[0].label}</span>
        <XIcon
          className="h-4 w-4 flex-shrink-0 rounded-full p-0.5 text-muted-foreground hover:bg-accent-foreground/10"
          onPointerDown={(e) => {
            e.stopPropagation();
            onRemove(selectedItems[0].id);
          }}
          aria-label="Remove"
        />
      </span>
      {selectedItems.length > 1 && (
        <span className="flex-shrink-0 text-muted-foreground text-xs">+{selectedItems.length - 1}</span>
      )}
    </div>
  );
}

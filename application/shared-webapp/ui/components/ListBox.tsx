import { Check } from "lucide-react";
import type * as React from "react";
import { cn } from "../utils";

export interface ListBoxProps<T> extends Omit<React.ComponentPropsWithRef<"div">, "children"> {
  items?: Iterable<T>;
  selectionMode?: "none" | "single" | "multiple";
  selectedKeys?: Set<React.Key> | "all";
  onSelectionChange?: (keys: Set<React.Key>) => void;
  children: React.ReactNode | ((item: T) => React.ReactNode);
}

export function ListBox<T extends object>({
  className,
  children,
  items,
  selectionMode = "none",
  selectedKeys,
  onSelectionChange,
  ...props
}: ListBoxProps<T>) {
  const itemsArray = items ? Array.from(items) : [];

  const renderChildren = () => {
    if (typeof children === "function") {
      return itemsArray.map((item) => children(item));
    }
    return children;
  };

  return (
    <div
      role="listbox"
      aria-multiselectable={selectionMode === "multiple" ? true : undefined}
      className={cn("rounded-lg border border-border p-1 outline-0", className)}
      {...props}
    >
      {renderChildren()}
    </div>
  );
}

export interface ListBoxItemProps extends Omit<React.ComponentPropsWithRef<"div">, "id"> {
  id?: React.Key;
  textValue?: string;
  isDisabled?: boolean;
  isSelected?: boolean;
  onSelect?: () => void;
}

export function ListBoxItem({
  className,
  children,
  id,
  textValue,
  isDisabled = false,
  isSelected = false,
  onSelect,
  ...props
}: ListBoxItemProps) {
  const handleClick = () => {
    if (!isDisabled && onSelect) {
      onSelect();
    }
  };

  const handleKeyDown = (event: React.KeyboardEvent<HTMLDivElement>) => {
    if (isDisabled) {
      return;
    }
    if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      onSelect?.();
    }
  };

  return (
    <div
      role="option"
      aria-selected={isSelected}
      aria-disabled={isDisabled || undefined}
      data-selected={isSelected || undefined}
      data-disabled={isDisabled || undefined}
      tabIndex={isDisabled ? undefined : 0}
      className={cn(
        "group relative flex cursor-default select-none items-center gap-4 rounded-md px-2.5 py-1.5 text-sm outline-none will-change-transform",
        "bg-background hover:bg-hover-background",
        "focus-visible:outline-2 focus-visible:outline-ring focus-visible:outline-offset-2",
        isSelected &&
          "bg-active-background hover:bg-selected-hover-background forced-colors:bg-[Highlight] forced-colors:text-[HighlightText] forced-colors:outline-[HighlightText]",
        isDisabled && "text-muted-foreground/50 forced-colors:text-[GrayText]",
        className
      )}
      onClick={handleClick}
      onKeyDown={handleKeyDown}
      {...props}
    >
      {children}
      <div className="absolute right-4 bottom-0 left-4 hidden h-px bg-border forced-colors:bg-[HighlightText] [.group[data-selected]:has(+[data-selected])_&]:block" />
    </div>
  );
}

export interface ListBoxSectionProps<T> extends Omit<React.ComponentPropsWithRef<"div">, "title" | "children"> {
  title?: string;
  items?: Iterable<T>;
  children: React.ReactNode | ((item: T) => React.ReactNode);
}

export function ListBoxSection<T extends object>({
  className,
  title,
  items,
  children,
  ...props
}: ListBoxSectionProps<T>) {
  const itemsArray = items ? Array.from(items) : [];

  const renderChildren = () => {
    if (typeof children === "function") {
      return itemsArray.map((item) => children(item));
    }
    return children;
  };

  return (
    <div className={cn("after:block after:h-[5px] after:content-[''] first:-mt-[5px]", className)} {...props}>
      {title && (
        <div className="sticky -top-[5px] z-10 -mx-1 -mt-px truncate border-y bg-transparent px-4 py-1 font-semibold text-accent-foreground text-sm backdrop-blur-md supports-[-moz-appearance:none]:bg-accent [&+*]:mt-1">
          {title}
        </div>
      )}
      {renderChildren()}
    </div>
  );
}

export { Check as ListBoxItemIndicator };

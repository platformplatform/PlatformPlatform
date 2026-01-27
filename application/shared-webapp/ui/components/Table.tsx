import type * as React from "react";
import { createContext, use, useEffect, useRef, useState } from "react";

import { cn } from "../utils";

interface TableKeyboardNavigationContext {
  focusedRowIndex: number;
  setFocusedRowIndex: (index: number) => void;
}

const TableKeyboardNavigationContext = createContext<TableKeyboardNavigationContext | null>(null);

interface TableProps extends React.ComponentProps<"table"> {
  selectedIndex?: number;
  onNavigate?: (index: number) => void;
  onActivate?: (index: number) => void;
}

// NOTE: This diverges from stock ShadCN to add optional keyboard navigation with roving tabindex.
// Pass selectedIndex, onNavigate, and onActivate to enable arrow key navigation between body rows.
// TableRow accepts an optional index prop to participate in keyboard navigation via context.
function Table({ className, selectedIndex, onNavigate, onActivate, ...props }: TableProps) {
  const hasKeyboardNavigation = onNavigate != null;
  const containerRef = useRef<HTMLDivElement>(null);
  const [focusedRowIndex, setFocusedRowIndex] = useState<number>(
    selectedIndex != null && selectedIndex >= 0 ? selectedIndex : 0
  );

  useEffect(() => {
    if (selectedIndex != null && selectedIndex >= 0) {
      setFocusedRowIndex(selectedIndex);
    }
  }, [selectedIndex]);

  useEffect(() => {
    if (!hasKeyboardNavigation) {
      return;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      const container = containerRef.current;
      if (!container?.contains(document.activeElement)) {
        return;
      }

      const target = event.target as HTMLElement;
      if (target.tagName === "BUTTON" || target.closest("button")) {
        return;
      }

      const rows = container.querySelectorAll("tbody tr");
      const rowCount = rows.length;
      if (rowCount === 0) {
        return;
      }

      const currentIndex = selectedIndex != null && selectedIndex >= 0 ? selectedIndex : -1;

      if ((event.key === "Enter" || event.key === " ") && currentIndex >= 0) {
        event.preventDefault();
        event.stopPropagation();
        onActivate?.(currentIndex);
        return;
      }

      if (event.key === "ArrowDown" || event.key === "ArrowUp") {
        event.preventDefault();
        let nextIndex: number;
        if (event.key === "ArrowDown") {
          nextIndex = currentIndex < rowCount - 1 ? currentIndex + 1 : 0;
        } else {
          nextIndex = currentIndex > 0 ? currentIndex - 1 : rowCount - 1;
        }
        setFocusedRowIndex(nextIndex);
        onNavigate(nextIndex);

        const row = rows[nextIndex] as HTMLElement | undefined;
        row?.scrollIntoView({ block: "nearest" });
        row?.focus();
      }
    };

    document.addEventListener("keydown", handleKeyDown, true);
    return () => document.removeEventListener("keydown", handleKeyDown, true);
  }, [hasKeyboardNavigation, selectedIndex, onNavigate, onActivate]);

  const table = (
    <div ref={containerRef} data-slot="table-container" className="relative w-full overflow-x-auto">
      <table data-slot="table" className={cn("w-full caption-bottom text-sm", className)} {...props} />
    </div>
  );

  if (!hasKeyboardNavigation) {
    return table;
  }

  return (
    <TableKeyboardNavigationContext value={{ focusedRowIndex, setFocusedRowIndex }}>
      {table}
    </TableKeyboardNavigationContext>
  );
}

function TableHeader({ className, ...props }: React.ComponentProps<"thead">) {
  return <thead data-slot="table-header" className={cn("[&_tr]:border-b", className)} {...props} />;
}

function TableBody({ className, ...props }: React.ComponentProps<"tbody">) {
  return <tbody data-slot="table-body" className={cn("[&_tr:last-child]:border-0", className)} {...props} />;
}

function TableFooter({ className, ...props }: React.ComponentProps<"tfoot">) {
  return (
    <tfoot
      data-slot="table-footer"
      className={cn("border-t bg-muted/50 font-medium [&>tr]:last:border-b-0", className)}
      {...props}
    />
  );
}

interface TableRowProps extends React.ComponentProps<"tr"> {
  index?: number;
}

function TableRow({ className, index, ...props }: TableRowProps) {
  const keyboardNavigation = use(TableKeyboardNavigationContext);
  const hasNavigation = keyboardNavigation != null && index != null;

  return (
    <tr
      data-slot="table-row"
      // NOTE: This diverges from stock ShadCN to add focus ring styles using outline instead of ring utilities
      // and to support keyboard navigation via roving tabindex when an index prop and navigation context are present.
      className={cn(
        "rounded-md border-b outline-ring transition-colors hover:bg-muted/50 focus-visible:outline focus-visible:outline-2 focus-visible:-outline-offset-2 data-[state=selected]:bg-muted",
        className
      )}
      tabIndex={hasNavigation ? (index === keyboardNavigation.focusedRowIndex ? 0 : -1) : undefined}
      onFocus={hasNavigation ? () => keyboardNavigation.setFocusedRowIndex(index) : undefined}
      {...props}
    />
  );
}

function TableHead({ className, ...props }: React.ComponentProps<"th">) {
  return (
    <th
      data-slot="table-head"
      className={cn(
        "h-10 whitespace-nowrap px-2 text-left align-middle font-medium text-foreground [&:has([role=checkbox])]:pr-0",
        className
      )}
      {...props}
    />
  );
}

function TableCell({ className, ...props }: React.ComponentProps<"td">) {
  return (
    <td
      data-slot="table-cell"
      className={cn("whitespace-nowrap p-2 align-middle [&:has([role=checkbox])]:pr-0", className)}
      {...props}
    />
  );
}

function TableCaption({ className, ...props }: React.ComponentProps<"caption">) {
  return (
    <caption data-slot="table-caption" className={cn("mt-4 text-muted-foreground text-sm", className)} {...props} />
  );
}

export { Table, TableHeader, TableBody, TableFooter, TableHead, TableRow, TableCell, TableCaption };

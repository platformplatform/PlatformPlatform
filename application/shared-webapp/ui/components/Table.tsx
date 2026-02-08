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
  const [focusedRowIndex, setFocusedRowIndex] = useState<number>(0);

  // NOTE: This diverges from stock ShadCN to scroll the selected row into view (e.g., deep links).
  // Uses manual scrollTop instead of scrollIntoView to avoid changing the browser's sequential
  // focus navigation starting point, which would make Tab skip the "Skip to main content" link.
  useEffect(() => {
    if (selectedIndex == null || selectedIndex < 0) {
      return;
    }
    const container = containerRef.current;
    const row = container?.querySelectorAll("tbody tr")[selectedIndex] as HTMLElement | undefined;
    if (!row) {
      return;
    }

    let scrollable: HTMLElement | null = row.parentElement;
    while (scrollable && scrollable.scrollHeight <= scrollable.clientHeight) {
      scrollable = scrollable.parentElement;
    }
    if (!scrollable) {
      return;
    }

    const rowRect = row.getBoundingClientRect();
    const scrollableRect = scrollable.getBoundingClientRect();
    if (rowRect.top < scrollableRect.top) {
      scrollable.scrollTop -= scrollableRect.top - rowRect.top;
    } else if (rowRect.bottom > scrollableRect.bottom) {
      scrollable.scrollTop += rowRect.bottom - scrollableRect.bottom;
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

      // Only handle keyboard navigation when focus is inside the table body.
      // Column headers, buttons, and other elements handle their own keyboard events.
      if (!target.closest("tbody")) {
        return;
      }
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

      // NOTE: This diverges from stock ShadCN to clamp at first/last row instead of wrapping around.
      if (event.key === "ArrowDown" || event.key === "ArrowUp") {
        event.preventDefault();
        let nextIndex: number;
        if (event.key === "ArrowDown") {
          if (currentIndex >= rowCount - 1) {
            return;
          }
          nextIndex = currentIndex + 1;
        } else {
          if (currentIndex <= 0) {
            return;
          }
          nextIndex = currentIndex - 1;
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
      // NOTE: This diverges from stock ShadCN to add focus ring styles using outline instead of ring utilities,
      // active:bg-muted for press feedback, and keyboard navigation via roving tabindex when an index prop and navigation context are present.
      className={cn(
        "rounded-md border-b outline-ring transition-colors hover:bg-muted/50 focus-visible:outline focus-visible:outline-2 focus-visible:-outline-offset-2 active:bg-muted data-[state=selected]:bg-muted",
        className
      )}
      tabIndex={hasNavigation ? (index === keyboardNavigation.focusedRowIndex ? 0 : -1) : undefined}
      onFocus={hasNavigation ? () => keyboardNavigation.setFocusedRowIndex(index) : undefined}
      {...props}
    />
  );
}

// NOTE: This diverges from stock ShadCN to make clickable column headers keyboard-accessible.
// When onClick is present, the header gets tabIndex={0} and responds to Enter/Space.
function TableHead({ className, onClick, onKeyDown, ...props }: React.ComponentProps<"th">) {
  const isInteractive = onClick != null;

  const handleKeyDown = isInteractive
    ? (event: React.KeyboardEvent<HTMLTableCellElement>) => {
        onKeyDown?.(event);
        if (event.key === "Enter" || event.key === " ") {
          event.preventDefault();
          event.stopPropagation();
          onClick(event as unknown as React.MouseEvent<HTMLTableCellElement>);
        }
      }
    : onKeyDown;

  return (
    <th
      data-slot="table-head"
      className={cn(
        "h-10 whitespace-nowrap px-2 text-left align-middle font-medium text-foreground outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:-outline-offset-2 [&:has([role=checkbox])]:pr-0",
        className
      )}
      onClick={onClick}
      onKeyDown={handleKeyDown}
      tabIndex={isInteractive ? 0 : undefined}
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

import type * as React from "react";

import { createContext, use, useEffect, useLayoutEffect, useRef, useState } from "react";

import { cn } from "../utils";

type TableRowSize = "compact" | "spacious";
type RowKey = string | number;
type SelectionMode = "none" | "single" | "multiple";

interface TableSelectionContextValue {
  focusedKey: RowKey | null;
  selectedKeys: ReadonlySet<RowKey>;
  hasSelection: boolean;
  isKeyboardNavigating: boolean;
}

const TableSelectionContext = createContext<TableSelectionContextValue | null>(null);
const TableRowSizeContext = createContext<TableRowSize>("compact");
const TableStickyHeaderContext = createContext<boolean>(false);

const EMPTY_KEYS: ReadonlySet<RowKey> = new Set();

// Descendants that manage their own keyboard and mouse behaviour. Clicks on these should not
// be intercepted by the table. Checkboxes are explicitly excluded; the Table owns checkbox clicks
// so clicking a row and clicking its checkbox behave identically.
const INTERACTIVE_SELECTOR =
  'button:not([role="checkbox"]), [role="menuitem"], a[href], input:not([type="checkbox"]), select, textarea';

function parseRowKey(value: string): RowKey {
  const numeric = Number(value);
  return value !== "" && Number.isSafeInteger(numeric) && String(numeric) === value ? numeric : value;
}

function rowSelector(key: RowKey): string {
  return `tbody tr[data-row-key="${CSS.escape(String(key))}"]`;
}

function toggle(keys: ReadonlySet<RowKey>, key: RowKey): Set<RowKey> {
  const next = new Set<RowKey>(keys);
  if (next.has(key)) {
    next.delete(key);
  } else {
    next.add(key);
  }
  return next;
}

function range(anchor: RowKey, target: RowKey, orderedKeys: RowKey[]): Set<RowKey> {
  const anchorIdx = orderedKeys.indexOf(anchor);
  const targetIdx = orderedKeys.indexOf(target);
  if (anchorIdx < 0 || targetIdx < 0) {
    return new Set<RowKey>([target]);
  }
  const [start, end] = anchorIdx <= targetIdx ? [anchorIdx, targetIdx] : [targetIdx, anchorIdx];
  return new Set<RowKey>(orderedKeys.slice(start, end + 1));
}

// Each interaction produces an Outcome. A single apply step writes the outcome back to the
// component (selection callback, focus state, anchor ref, activate callback). Keeping the
// per-event logic as pure functions means each rule has one place to live.
//
// `transient` marks a selection that came from a plain click (or from a preceding transient arrow
// replace). Arrow navigation replaces transient selections with the focused row, but preserves
// explicit batches built via Cmd/Shift-click or Space toggle. An unselect-down-to-one via Space on
// an explicit batch still counts as explicit, so the remaining row survives arrow navigation.
interface Outcome {
  selection?: Set<RowKey>;
  focusKey?: RowKey;
  anchorKey?: RowKey | null;
  activate?: RowKey;
  transient?: boolean;
}

// Clicking a row body vs. clicking its checkbox must feel different:
//  - Plain row click = "make this the current row" (replace selection + activate). In multi mode
//    a plain click clears any prior batch; Cmd/Ctrl and Shift build it. On the next keyboard nav
//    the single-row selection gives way to keyboard semantics (handled in outcomeForArrow).
//  - Checkbox click = "add/remove from the batch" (toggle, never activates).
function outcomeForClick(
  key: RowKey,
  mode: SelectionMode,
  modKey: boolean,
  shift: boolean,
  isCheckboxClick: boolean,
  selectedKeys: ReadonlySet<RowKey>,
  anchorKey: RowKey | null,
  orderedKeys: RowKey[]
): Outcome {
  if (mode === "none") {
    return {};
  }
  if (mode === "single") {
    return { selection: new Set<RowKey>([key]), focusKey: key, anchorKey: key, activate: key, transient: true };
  }
  // multiple
  if (isCheckboxClick) {
    if (shift && anchorKey != null) {
      return { selection: range(anchorKey, key, orderedKeys), focusKey: key, transient: false };
    }
    return { selection: toggle(selectedKeys, key), focusKey: key, anchorKey: key, transient: false };
  }
  // row body click
  if (shift && anchorKey != null) {
    return { selection: range(anchorKey, key, orderedKeys), focusKey: key, transient: false };
  }
  if (modKey) {
    return { selection: toggle(selectedKeys, key), focusKey: key, anchorKey: key, transient: false };
  }
  return { selection: new Set<RowKey>([key]), focusKey: key, anchorKey: key, activate: key, transient: true };
}

function outcomeForArrow(
  direction: "up" | "down",
  shift: boolean,
  mode: SelectionMode,
  orderedKeys: RowKey[],
  focusedKey: RowKey | null,
  anchorKey: RowKey | null,
  isTransientSelection: boolean,
  activateOnNavigate: boolean
): Outcome {
  if (mode === "none" || orderedKeys.length === 0) {
    return {};
  }
  const currentIdx = focusedKey != null ? orderedKeys.indexOf(focusedKey) : -1;
  let nextIdx: number;
  if (direction === "down") {
    if (currentIdx < 0) {
      nextIdx = 0;
    } else if (currentIdx >= orderedKeys.length - 1) {
      return {};
    } else {
      nextIdx = currentIdx + 1;
    }
  } else {
    if (currentIdx <= 0) {
      return {};
    }
    nextIdx = currentIdx - 1;
  }
  const nextKey = orderedKeys[nextIdx];

  if (shift && mode === "multiple") {
    const anchor = anchorKey ?? focusedKey ?? nextKey;
    return {
      selection: range(anchor, nextKey, orderedKeys),
      focusKey: nextKey,
      anchorKey: anchor,
      transient: false
    };
  }

  const outcome: Outcome = { focusKey: nextKey, anchorKey: nextKey };
  // Selection follows keyboard focus when it represents a single row from a plain click or prior
  // arrow replace (transient). In single mode this is always the case. Explicit multi-select
  // batches built via Cmd/Shift-click or Space toggle are preserved even when the batch has been
  // whittled down to one row, so the user doesn't lose their explicit selection.
  if (mode === "single" || isTransientSelection) {
    outcome.selection = new Set<RowKey>([nextKey]);
    outcome.transient = true;
  }
  if (activateOnNavigate) {
    outcome.activate = nextKey;
  }
  return outcome;
}

function outcomeForEnter(focusedKey: RowKey | null): Outcome {
  return focusedKey != null ? { activate: focusedKey } : {};
}

function outcomeForSpace(mode: SelectionMode, focusedKey: RowKey | null, selectedKeys: ReadonlySet<RowKey>): Outcome {
  if (focusedKey == null || mode === "none") {
    return {};
  }
  if (mode === "single") {
    return { activate: focusedKey };
  }
  return { selection: toggle(selectedKeys, focusedKey), anchorKey: focusedKey, transient: false };
}

interface TableProps extends React.ComponentProps<"table"> {
  rowSize: TableRowSize;
  selectionMode?: SelectionMode;
  selectedKeys?: ReadonlySet<RowKey>;
  onSelectionChange?: (keys: Set<RowKey>) => void;
  onActivate?: (key: RowKey) => void;
  activateOnNavigate?: boolean;
  scrollToKey?: RowKey;
  stickyHeader?: boolean;
  containerClassName?: string;
}

function Table({
  className,
  rowSize,
  selectionMode = "none",
  selectedKeys,
  onSelectionChange,
  onActivate,
  activateOnNavigate = false,
  scrollToKey,
  stickyHeader = false,
  containerClassName,
  ...props
}: TableProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const hasSelection = selectionMode !== "none";
  const effectiveSelectedKeys = selectedKeys ?? EMPTY_KEYS;
  const [focusedKey, setFocusedKey] = useState<RowKey | null>(null);
  const [isKeyboardNavigating, setIsKeyboardNavigating] = useState(false);
  const anchorKeyRef = useRef<RowKey | null>(null);
  const transientSelectionRef = useRef(false);

  const stateRef = useRef({
    selectionMode,
    selectedKeys: effectiveSelectedKeys,
    activateOnNavigate,
    onSelectionChange,
    onActivate,
    focusedKey
  });
  stateRef.current = {
    selectionMode,
    selectedKeys: effectiveSelectedKeys,
    activateOnNavigate,
    onSelectionChange,
    onActivate,
    focusedKey
  };

  // Scroll a deep-linked row into view without hijacking the sequential focus navigation starting
  // point (scrollIntoView would make Tab skip past the "Skip to main content" link).
  useEffect(() => {
    if (scrollToKey == null) {
      return;
    }
    const container = containerRef.current;
    const row = container?.querySelector<HTMLElement>(rowSelector(scrollToKey));
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
  }, [scrollToKey]);

  // Keep focusedKey valid as rows/selection change (e.g. pagination, filtering). Prefer the current
  // focused row so rapid keyboard actions aren't reset by selection updates; only fall back to
  // scrollToKey / first selected / first row when the current focus is stale. A MutationObserver
  // on tbody catches pagination/filtering row swaps that don't change any prop (without this the
  // roving tabindex stays on a page-1 row after paginating to page 2, so Tab skips the table body).
  useEffect(() => {
    if (!hasSelection) {
      return;
    }
    const container = containerRef.current;
    if (!container) {
      return;
    }
    const rowExists = (key: RowKey) => container.querySelector(rowSelector(key)) != null;
    const validate = () => {
      setFocusedKey((current) => {
        if (current != null && rowExists(current)) {
          return current;
        }
        if (scrollToKey != null && rowExists(scrollToKey)) {
          return scrollToKey;
        }
        for (const key of effectiveSelectedKeys) {
          if (rowExists(key)) {
            return key;
          }
        }
        const first = container.querySelector<HTMLElement>("tbody tr[data-row-key]");
        return first?.dataset.rowKey != null ? parseRowKey(first.dataset.rowKey) : null;
      });
    };

    validate();

    const tbody = container.querySelector("tbody");
    if (tbody == null) {
      return;
    }
    const observer = new MutationObserver(validate);
    observer.observe(tbody, { childList: true });
    return () => observer.disconnect();
  }, [hasSelection, scrollToKey, effectiveSelectedKeys]);

  useEffect(() => {
    if (!hasSelection) {
      return;
    }
    const container = containerRef.current;
    const tableElement = container?.querySelector("table");
    if (!container || !tableElement) {
      return;
    }

    const readOrderedKeys = (): RowKey[] =>
      Array.from(container.querySelectorAll<HTMLElement>("tbody tr[data-row-key]"))
        .map((row) => row.dataset.rowKey)
        .filter((value): value is string => value != null)
        .map(parseRowKey);

    const applyOutcome = (outcome: Outcome) => {
      const state = stateRef.current;
      // Keep stateRef in sync synchronously so the next keypress (before React re-renders) sees
      // the updated selection/focus. Without this, rapid Shift+Arrow presses read stale focus and
      // the range stops growing after one extension.
      if (outcome.selection != null) {
        state.onSelectionChange?.(outcome.selection);
        stateRef.current = { ...stateRef.current, selectedKeys: outcome.selection };
      }
      if (outcome.anchorKey !== undefined) {
        anchorKeyRef.current = outcome.anchorKey;
      }
      if (outcome.focusKey !== undefined) {
        stateRef.current = { ...stateRef.current, focusedKey: outcome.focusKey };
        setFocusedKey(outcome.focusKey);
        const row = container.querySelector<HTMLElement>(rowSelector(outcome.focusKey));
        row?.scrollIntoView({ block: "nearest" });
        row?.focus();
      }
      if (outcome.activate !== undefined) {
        state.onActivate?.(outcome.activate);
      }
      if (outcome.transient !== undefined) {
        transientSelectionRef.current = outcome.transient;
      }
    };

    const handleClick = (event: MouseEvent) => {
      // BaseUI's Checkbox forwards a real click on its button to a hidden <input> via a synthetic
      // click(); that second click bubbles through the table too. Skip untrusted events so the
      // handler runs exactly once per user interaction.
      if (!event.isTrusted) {
        return;
      }
      const target = event.target as HTMLElement | null;
      if (!target) {
        return;
      }
      const row = target.closest<HTMLElement>("tbody tr[data-row-key]");
      if (!row || !container.contains(row)) {
        return;
      }
      if (target.closest(INTERACTIVE_SELECTOR) != null) {
        return;
      }
      const state = stateRef.current;
      // Treat any click inside a cell that contains a checkbox as a checkbox click, even if the
      // pointer missed the checkbox itself -- this makes the whole checkbox column act as a big
      // hit target without affecting clicks on other columns.
      const cell = target.closest<HTMLElement>("td");
      const cellHasCheckbox = cell?.querySelector('[role="checkbox"]') != null;
      const isCheckboxClick = cellHasCheckbox || target.closest('[role="checkbox"]') != null;
      const outcome = outcomeForClick(
        parseRowKey(row.dataset.rowKey ?? ""),
        state.selectionMode,
        event.metaKey || event.ctrlKey,
        event.shiftKey,
        isCheckboxClick,
        state.selectedKeys,
        anchorKeyRef.current,
        readOrderedKeys()
      );
      // Prevent the default checkbox toggle when the click landed on the actual checkbox; the Table
      // owns selection, so letting BaseUI also flip its controlled state would double-fire. For
      // clicks in the checkbox cell but off the checkbox, there's no default to prevent.
      if (target.closest('[role="checkbox"]') != null) {
        event.preventDefault();
      }
      applyOutcome(outcome);
    };

    const handleKeyDown = (event: KeyboardEvent) => {
      if (!container.contains(document.activeElement)) {
        return;
      }
      const target = event.target as HTMLElement | null;
      if (!target?.closest("tbody")) {
        return;
      }
      if (target.closest(INTERACTIVE_SELECTOR) != null) {
        return;
      }
      const state = stateRef.current;
      let outcome: Outcome | null = null;
      if (event.key === "Enter") {
        outcome = outcomeForEnter(state.focusedKey);
      } else if (event.key === " ") {
        outcome = outcomeForSpace(state.selectionMode, state.focusedKey, state.selectedKeys);
      } else if (event.key === "ArrowDown" || event.key === "ArrowUp") {
        outcome = outcomeForArrow(
          event.key === "ArrowDown" ? "down" : "up",
          event.shiftKey,
          state.selectionMode,
          readOrderedKeys(),
          state.focusedKey,
          anchorKeyRef.current,
          transientSelectionRef.current,
          state.activateOnNavigate
        );
      }
      if (outcome == null) {
        return;
      }
      if (outcome.focusKey !== undefined || outcome.selection != null || outcome.activate !== undefined) {
        event.preventDefault();
        event.stopPropagation();
      }
      setIsKeyboardNavigating(true);
      applyOutcome(outcome);
    };

    const handlePointerMove = () => {
      setIsKeyboardNavigating((current) => (current ? false : current));
    };

    tableElement.addEventListener("click", handleClick, true);
    document.addEventListener("keydown", handleKeyDown, true);
    tableElement.addEventListener("pointermove", handlePointerMove);
    return () => {
      tableElement.removeEventListener("click", handleClick, true);
      document.removeEventListener("keydown", handleKeyDown, true);
      tableElement.removeEventListener("pointermove", handlePointerMove);
    };
  }, [hasSelection]);

  const table = (
    <div
      ref={containerRef}
      data-slot="table-container"
      className={cn("relative w-full overflow-x-auto rounded-md border bg-card", containerClassName)}
    >
      <table data-slot="table" className={cn("w-full caption-bottom text-sm", className)} {...props} />
    </div>
  );
  const withRowSize = <TableRowSizeContext value={rowSize}>{table}</TableRowSizeContext>;
  const withSticky = <TableStickyHeaderContext value={stickyHeader}>{withRowSize}</TableStickyHeaderContext>;

  if (!hasSelection) {
    return withSticky;
  }

  return (
    <TableSelectionContext
      value={{ focusedKey, selectedKeys: effectiveSelectedKeys, hasSelection, isKeyboardNavigating }}
    >
      {withSticky}
    </TableSelectionContext>
  );
}

function TableHeader({ className, ...props }: React.ComponentProps<"thead">) {
  const sticky = use(TableStickyHeaderContext);
  return (
    <TableRowSizeContext value="compact">
      <thead
        data-slot="table-header"
        className={cn("[&_tr]:border-b [&_tr]:bg-card", sticky && "sticky top-0 z-10", className)}
        {...props}
      />
    </TableRowSizeContext>
  );
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
  rowKey?: RowKey;
}

const rowSizeStyles: Record<TableRowSize, string> = {
  compact: "h-11",
  spacious: "h-[4.5rem]"
};

function TableRow({ className, rowKey, ...props }: TableRowProps) {
  const selection = use(TableSelectionContext);
  const rowSize = use(TableRowSizeContext);
  const isSelectable = selection != null && rowKey != null;
  const isSelected = isSelectable && selection.selectedKeys.has(rowKey);
  const tabIndex = isSelectable ? (selection.focusedKey === rowKey ? 0 : -1) : undefined;
  const suppressHover = selection?.isKeyboardNavigating ?? false;

  return (
    <tr
      data-slot="table-row"
      data-row-key={rowKey != null ? String(rowKey) : undefined}
      data-state={isSelected ? "selected" : undefined}
      className={cn(
        // Selected and keyboard-focused rows are marked with a 4px primary-colored bar on the
        // left edge (drawn via inset box-shadow so it doesn't shift content). No outline ring —
        // the bar replaces it for both states. Mouse-selected and keyboard-focused look identical.
        "border-b transition-colors focus-visible:shadow-[inset_4px_0_0_var(--primary)] focus-visible:outline-none active:bg-muted data-[state=selected]:bg-active-background data-[state=selected]:shadow-[inset_4px_0_0_var(--primary)]",
        !suppressHover && "hover:bg-hover-background data-[state=selected]:hover:bg-active-background",
        rowSizeStyles[rowSize],
        isSelectable && "cursor-pointer select-none",
        className
      )}
      tabIndex={tabIndex}
      {...props}
    />
  );
}

function TableHead({ className, onClick, onKeyDown, children, ...props }: React.ComponentProps<"th">) {
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
        "h-10 rounded-sm px-2 text-left align-middle text-xs font-bold whitespace-nowrap text-foreground outline-ring focus-visible:outline-2 focus-visible:-outline-offset-2 [&:has([role=checkbox])]:pr-0",
        isInteractive && "cursor-pointer select-none",
        className
      )}
      onClick={onClick}
      onKeyDown={handleKeyDown}
      tabIndex={isInteractive ? 0 : undefined}
      {...props}
    >
      <span className="inline-flex items-center gap-1">{children}</span>
    </th>
  );
}

// Any focusable descendant of a body <TableCell> is pulled out of the tab order so large tables
// don't force keyboard users to cycle through every row's action buttons, checkboxes, and dropdowns.
// Rows themselves are focusable via roving tabindex; activation happens on Enter/Space. Header cells
// are exempt. Opt back in per element with `data-keep-tab-stop`.
function useSuppressTabStops(cellRef: React.RefObject<HTMLElement | null>) {
  useLayoutEffect(() => {
    const cell = cellRef.current;
    if (!cell) {
      return;
    }
    const selector =
      'button, [role="checkbox"], [role="switch"], [role="radio"], a[href], input, select, textarea, [tabindex]';
    const focusable = cell.querySelectorAll<HTMLElement>(selector);
    focusable.forEach((element) => {
      if (element.dataset.keepTabStop !== undefined) {
        return;
      }
      if (element.tabIndex !== -1) {
        element.tabIndex = -1;
      }
    });
  });
}

function TableCell({ className, ...props }: React.ComponentProps<"td">) {
  const cellRef = useRef<HTMLTableCellElement>(null);
  useSuppressTabStops(cellRef);
  return (
    <td
      ref={cellRef}
      data-slot="table-cell"
      className={cn("p-2 align-middle whitespace-nowrap [&:has([role=checkbox])]:pr-0", className)}
      {...props}
    />
  );
}

function TableCaption({ className, ...props }: React.ComponentProps<"caption">) {
  return (
    <caption data-slot="table-caption" className={cn("mt-4 text-sm text-muted-foreground", className)} {...props} />
  );
}

export { Table, TableHeader, TableBody, TableFooter, TableHead, TableRow, TableCell, TableCaption };
export type { RowKey, TableRowSize };

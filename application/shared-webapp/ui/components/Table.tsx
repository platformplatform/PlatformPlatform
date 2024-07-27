/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/table--docs
 * ref: https://ui.shadcn.com/docs/components/table
 */
import { ArrowUp } from "lucide-react";
import {
  Cell as AriaCell,
  Column as AriaColumn,
  Row as AriaRow,
  Table as AriaTable,
  TableHeader as AriaTableHeader,
  Button,
  type CellProps,
  Collection,
  type ColumnProps,
  ColumnResizer,
  Group,
  ResizableTableContainer,
  type RowProps,
  type TableHeaderProps,
  type TableProps,
  composeRenderProps,
  useTableOptions
} from "react-aria-components";
import { tv } from "tailwind-variants";
import { focusRing } from "./focusRing";
import { composeTailwindRenderProps } from "./utils";
import { Checkbox } from "./Checkbox";

export { TableBody, useContextProps } from "react-aria-components";

export function Table(props: Readonly<TableProps>) {
  return (
    <div className="relative h-full w-full" aria-hidden>
      <div className="absolute top-0 left-0 right-0 bottom-0 overflow-hidden" aria-hidden>
        <ResizableTableContainer
          className="relative h-full w-full scroll-pt-[2.281rem] overflow-auto rounded-md"
          aria-hidden
        >
          <AriaTable {...props} className="border-separate border-spacing-0" />
        </ResizableTableContainer>
      </div>
    </div>
  );
}

const columnStyles = tv({
  extend: focusRing,
  base: "flex w-full gap-1 items-center text-xs font-bold"
});

const resizerStyles = tv({
  extend: focusRing,
  base: "column-resizer absolute right-0 top-1.5 w-px h-6 px-2 py-1 shrink-0 translate-x-2 box-content bg-clip-content cursor-col-resize rounded -outline-offset-2",
  variants: {
    isResizing: {
      false: "bg-border forced-colors:bg-[ButtonBorder] ",
      true: "bg-ring forced-colors:bg-[Highlight]"
    },
    isHovered: {
      true: "bg-muted-foreground/80"
    }
  }
});

export function Column({ children, className, ...props }: Readonly<ColumnProps>) {
  return (
    <AriaColumn
      {...props}
      className={composeTailwindRenderProps(
        className,
        "relative h-12 cursor-default text-start text-muted-foreground [&:focus-within]:z-20 [&:hover]:z-20"
      )}
    >
      {composeRenderProps(children, (children, { allowsSorting, sortDirection }) => (
        <div className="flex px-2 items-center w-full">
          <Group role="presentation" tabIndex={-1} className={columnStyles}>
            <span className="truncate">{children}</span>
            {allowsSorting && (
              <span
                className={`flex h-4 w-4 items-center justify-center transition ${
                  sortDirection === "descending" ? "rotate-180" : ""
                }`}
              >
                {sortDirection && (
                  <ArrowUp aria-hidden className="h-4 w-4 text-muted-foreground forced-colors:text-[ButtonText]" />
                )}
              </span>
            )}
          </Group>
          {!props.width && <ColumnResizer className={(rp) => resizerStyles(rp)} />}
        </div>
      ))}
    </AriaColumn>
  );
}

export function TableHeader<T extends object>({
  className,
  children,
  ...tableHeaderProps
}: Readonly<TableHeaderProps<T>>) {
  const { selectionBehavior, selectionMode, allowsDragging } = useTableOptions();

  return (
    <AriaTableHeader
      {...tableHeaderProps}
      className={composeTailwindRenderProps(
        className,
        "sticky [&>tr>th:first-child]:pl-4 [&>tr>th:last-child]:pr-4 top-0 z-10 h-16 rounded-lg bg-background backdrop-blur-3xl supports-[-moz-appearance:none]:bg-accent forced-colors:bg-[Canvas] [&>tr>th:last-child_.column-resizer]:hidden"
      )}
    >
      {/* Add extra columns for drag and drop and selection. */}
      {allowsDragging && <Column />}
      {selectionBehavior === "toggle" && (
        <AriaColumn width={52} minWidth={52}>
          {selectionMode === "multiple" && <Checkbox slot="selection" />}
        </AriaColumn>
      )}
      <Collection items={tableHeaderProps.columns}>{children}</Collection>
    </AriaTableHeader>
  );
}

const rowStyles = tv({
  extend: focusRing,
  base: "h-16 [&>td:first-child]:pl-4 [&>td:last-child]:pr-4 transition-colors group/row relative cursor-default select-none -outline-offset-2 text-xs font-normal",
  variants: {
    isDisabled: {
      false: "text-muted-foreground hover:bg-muted/80",
      true: "text-muted-foreground/90"
    },
    isSelected: {
      true: "bg-muted"
    },
    isHovered: {
      true: "text-foreground"
    }
  }
});

export function Row<T extends object>({ id, columns, children, ...rowProps }: Readonly<RowProps<T>>) {
  const { selectionBehavior, allowsDragging } = useTableOptions();

  return (
    <AriaRow id={id} {...rowProps} className={rowStyles}>
      {allowsDragging && (
        <Cell>
          <Button slot="drag">≡</Button>
        </Cell>
      )}
      {selectionBehavior === "toggle" && (
        <Cell>
          <Checkbox slot="selection" />
        </Cell>
      )}
      <Collection items={columns}>{children}</Collection>
    </AriaRow>
  );
}

const cellStyles = tv({
  extend: focusRing,
  base: "px-2 border-b border-b-border group-first/row:border-y group-first/row:border-t-border group-last/row:border-b-0 group-selected/row:border-ring [:has(+[data-selected])_&]:border-ring truncate -outline-offset-2"
});

export function Cell(props: Readonly<CellProps>) {
  return <AriaCell {...props} className={cellStyles} />;
}

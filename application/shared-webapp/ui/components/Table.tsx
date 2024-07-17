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
import { twMerge } from "tailwind-merge";
import { tv } from "tailwind-variants";
import { Checkbox } from "./Checkbox";
import { focusRing } from "./focusRing";
import { composeTailwindRenderProps } from "./utils";

export { TableBody, useContextProps } from "react-aria-components";

export function Table(props: Readonly<TableProps>) {
  return (
    <div className="h-full w-full overflow-hidden">
      <ResizableTableContainer className="relative h-full w-full scroll-pt-[2.281rem] overflow-auto rounded-md">
        <AriaTable {...props} className="border-separate border-spacing-0" />
      </ResizableTableContainer>
    </div>
  );
}

const columnStyles = tv({
  extend: focusRing,
  base: "px-4 h-12 flex-1 flex gap-1 items-center"
});

const resizerStyles = tv({
  extend: focusRing,
  base: "w-px px-[8px] translate-x-[8px] box-content py-1 h-6 bg-clip-content bg-muted-foreground forced-colors:bg-[ButtonBorder] cursor-col-resize rounded resizing:bg-ring forced-colors:resizing:bg-[Highlight] resizing:w-[2px] resizing:pl-[7px] -outline-offset-2"
});

export function Column({ children, className, ...props }: Readonly<ColumnProps>) {
  return (
    <AriaColumn
      {...props}
      className={composeTailwindRenderProps(
        className,
        "cursor-default text-start font-semibold text-muted-foreground [&:focus-within]:z-20 [&:hover]:z-20"
      )}
    >
      {composeRenderProps(children, (children, { allowsSorting, sortDirection }) => (
        <div className="flex items-center">
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
          {!props.width && <ColumnResizer className={resizerStyles({ className: "column-resizer" })} />}
        </div>
      ))}
    </AriaColumn>
  );
}

export function TableHeader<T extends object>(props: Readonly<TableHeaderProps<T>>) {
  const { selectionBehavior, selectionMode, allowsDragging } = useTableOptions();

  return (
    <AriaTableHeader
      {...props}
      className={twMerge(
        "sticky top-0 z-10 h-12 rounded-lg bg-background backdrop-blur-3xl hover:bg-accent supports-[-moz-appearance:none]:bg-accent forced-colors:bg-[Canvas] [&>tr>th:last-child_.column-resizer]:hidden",
        props.className
      )}
    >
      {/* Add extra columns for drag and drop and selection. */}
      {allowsDragging && <Column />}
      {selectionBehavior === "toggle" && (
        <AriaColumn width={36} minWidth={36} className="cursor-default p-4 text-start font-semibold text-sm">
          {selectionMode === "multiple" && <Checkbox slot="selection" />}
        </AriaColumn>
      )}
      <Collection items={props.columns}>{props.children}</Collection>
    </AriaTableHeader>
  );
}

const rowStyles = tv({
  extend: focusRing,
  base: "h-12 transition-colors group/row relative cursor-default select-none -outline-offset-2 text-sm",
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

export function Row<T extends object>({ id, columns, children, ...otherProps }: Readonly<RowProps<T>>) {
  const { selectionBehavior, allowsDragging } = useTableOptions();

  return (
    <AriaRow id={id} {...otherProps} className={rowStyles}>
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
  base: "border-b border-b-border group-first/row:border-y group-first/row:border-t-border group-last/row:border-b-0 group-selected/row:border-ring [:has(+[data-selected])_&]:border-ring p-4 truncate -outline-offset-2"
});

export function Cell(props: Readonly<CellProps>) {
  return <AriaCell {...props} className={cellStyles} />;
}

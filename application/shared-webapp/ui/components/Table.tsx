/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/table--docs
 * ref: https://ui.shadcn.com/docs/components/table
 */
import { ArrowUp } from "lucide-react";
import {
  Cell as AriaCell,
  type CellProps as AriaCellProps,
  Column as AriaColumn,
  Row as AriaRow,
  Table as AriaTable,
  TableHeader as AriaTableHeader,
  Button,
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
import { Checkbox } from "./Checkbox";
import { focusRing } from "./focusRing";
import { composeTailwindRenderProps } from "./utils";

export { TableBody, useContextProps } from "react-aria-components";

export function Table(props: Readonly<TableProps>) {
  return (
    <div className="relative h-full w-full" aria-hidden={true}>
      <div className="absolute top-0 right-0 bottom-0 left-0 overflow-hidden" aria-hidden={true}>
        <ResizableTableContainer
          className="relative h-full w-full scroll-pt-[2.281rem] overflow-auto rounded-md"
          aria-hidden={true}
        >
          <AriaTable {...props} className="border-separate border-spacing-0" />
        </ResizableTableContainer>
      </div>
    </div>
  );
}

const columnStyles = tv({
  extend: focusRing,
  base: "flex w-full items-center gap-1 font-bold text-xs"
});

const resizerStyles = tv({
  extend: focusRing,
  base: "column-resizer -outline-offset-2 absolute right-0 box-content h-6 w-px shrink-0 translate-x-2 cursor-col-resize rounded bg-clip-content px-2 py-1",
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
        "relative h-12 cursor-default text-start text-foreground/70 [&:focus-within]:z-20 [&:hover]:z-20"
      )}
    >
      {composeRenderProps(children, (children, { allowsSorting, sortDirection }) => (
        <div className="flex w-full items-center px-2">
          <Group role="presentation" tabIndex={-1} className={columnStyles}>
            <span className="truncate">{children}</span>
            {allowsSorting && (
              <span
                className={`flex h-4 w-4 items-center justify-center transition ${
                  sortDirection === "descending" ? "rotate-180" : ""
                }`}
              >
                {sortDirection && (
                  <ArrowUp
                    aria-hidden={true}
                    className="h-4 w-4 text-muted-foreground forced-colors:text-[ButtonText]"
                  />
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
        "sticky top-0 z-10 rounded-lg bg-background backdrop-blur-3xl supports-[-moz-appearance:none]:bg-accent forced-colors:bg-[Canvas] [&>tr>th:first-child]:pl-4 [&>tr>th:last-child]:pr-4 [&>tr>th:last-child_.column-resizer]:hidden"
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
  base: "group/row -outline-offset-2 relative cursor-default select-none font-normal text-sm transition-colors [&>td:first-child]:pl-4 [&>td:last-child]:pr-4",
  variants: {
    isDisabled: {
      false: "text-muted-foreground hover:bg-muted/80",
      true: "text-muted-foreground/90"
    },
    isSelected: {
      true: "bg-muted"
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
  base: "-outline-offset-2 truncate border-b border-b-border p-2 group-first/row:border-y group-first/row:border-t-border group-last/row:border-b-0 group-selected/row:border-ring [:has(+[data-selected])_&]:border-ring"
});

type CellProps = {
  className?: string;
} & AriaCellProps;

export function Cell({ className, ...props }: Readonly<CellProps>) {
  return <AriaCell {...props} className={(renderProps) => cellStyles({ ...renderProps, className })} />;
}

/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/index.html?path=/docs/gridlist--docs
 */
import {
  GridList as AriaGridList,
  GridListItem as AriaGridListItem,
  Button,
  type GridListItemProps,
  type GridListProps
} from "react-aria-components";
import { tv } from "tailwind-variants";
import { Checkbox } from "./Checkbox";
import { focusRing } from "./focusRing";
import { composeTailwindRenderProps } from "./utils";

export function GridList<T extends object>({ children, ...props }: Readonly<GridListProps<T>>) {
  return (
    <AriaGridList
      {...props}
      className={composeTailwindRenderProps(props.className, "relative overflow-auto rounded-lg border border-border")}
    >
      {children}
    </AriaGridList>
  );
}

const itemStyles = tv({
  extend: focusRing,
  base: "relative -mb-px flex cursor-default select-none gap-3 border-transparent border-y border-y-border px-3 py-2 text-sm -outline-offset-2 first:rounded-t-md first:border-t-0 last:mb-0 last:rounded-b-md last:border-b-0",
  variants: {
    isSelected: {
      false: "bg-background pressed:bg-muted/90 hover:bg-muted",
      true: "z-20 bg-muted/50 pressed:bg-muted/80 hover:bg-muted/90"
    },
    isDisabled: {
      true: "z-10 text-muted-foreground/50 forced-colors:text-[GrayText]"
    }
  }
});

export function GridListItem({ children, ...props }: Readonly<GridListItemProps>) {
  const textValue = typeof children === "string" ? children : undefined;
  return (
    <AriaGridListItem textValue={textValue} {...props} className={itemStyles}>
      {({ selectionMode, selectionBehavior, allowsDragging }) => (
        <>
          {/* Add elements for drag and drop and selection. */}
          {allowsDragging && <Button slot="drag">≡</Button>}
          {selectionMode === "multiple" && selectionBehavior === "toggle" && <Checkbox slot="selection" />}
          {children}
        </>
      )}
    </AriaGridListItem>
  );
}

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
  base: "relative flex gap-3 cursor-default select-none py-2 px-3 text-sm border-y border-y-border border-transparent first:border-t-0 last:border-b-0 first:rounded-t-md last:rounded-b-md -mb-px last:mb-0 -outline-offset-2",
  variants: {
    isSelected: {
      false: "bg-background hover:bg-muted pressed:bg-muted/90",
      true: "bg-muted/50 hover:bg-muted/90 pressed:bg-muted/80 z-20"
    },
    isDisabled: {
      true: "text-muted-foreground/50 forced-colors:text-[GrayText] z-10"
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

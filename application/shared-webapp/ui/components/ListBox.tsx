/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/listbox--docs
 */
import {
  ListBox as AriaListBox,
  ListBoxItem as AriaListBoxItem,
  type ListBoxProps as AriaListBoxProps,
  type ListBoxItemProps,
  composeRenderProps
} from "react-aria-components";
import { tv } from "tailwind-variants";
import { focusRing } from "./focusRing";
import { composeTailwindRenderProps } from "./utils";

interface ListBoxProps<T> extends Omit<AriaListBoxProps<T>, "layout" | "orientation"> {}

export function ListBox<T extends object>({ children, ...props }: ListBoxProps<T>) {
  return (
    <AriaListBox
      {...props}
      className={composeTailwindRenderProps(props.className, "rounded-lg border border-border p-1 outline-0")}
    >
      {children}
    </AriaListBox>
  );
}

export const itemStyles = tv({
  extend: focusRing,
  base: "group relative flex items-center gap-8 cursor-default select-none py-1.5 px-2.5 rounded-md will-change-transform text-sm forced-color-adjust-none",
  variants: {
    isSelected: {
      false: "bg-background hover:bg-muted pressed:bg-muted/90",
      true: "bg-muted/50 hover:bg-muted/90 pressed:bg-muted/80  forced-colors:bg-[Highlight] forced-colors:text-[HighlightText] forced-colors:outline-[HighlightText]"
    },
    isDisabled: {
      true: "text-muted-foreground/50 forced-colors:text-[GrayText]"
    }
  }
});

export function ListBoxItem(props: Readonly<ListBoxItemProps>) {
  const textValue = props.textValue ?? (typeof props.children === "string" ? props.children : undefined);
  return (
    <AriaListBoxItem {...props} textValue={textValue} className={itemStyles}>
      {composeRenderProps(props.children, (children) => (
        <>
          {children}
          <div className="absolute right-4 bottom-0 left-4 hidden h-px bg-border forced-colors:bg-[HighlightText] [.group[data-selected]:has(+[data-selected])_&]:block" />
        </>
      ))}
    </AriaListBoxItem>
  );
}

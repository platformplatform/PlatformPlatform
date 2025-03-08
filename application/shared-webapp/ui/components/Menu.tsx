/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/menu--docs
 * ref: https://ui.shadcn.com/docs/components/context-menu
 */
import { Check, ChevronRight } from "lucide-react";
import type React from "react";
import {
  Menu as AriaMenu,
  MenuItem as AriaMenuItem,
  type MenuProps as AriaMenuProps,
  Header,
  Keyboard,
  type MenuItemProps,
  Separator,
  type SeparatorProps,
  composeRenderProps
} from "react-aria-components";
import { tv } from "tailwind-variants";
import { DropdownSection, type DropdownSectionProps, dropdownItemStyles } from "./Dropdown";
import { Popover, type PopoverProps } from "./Popover";

export { MenuTrigger, SubmenuTrigger } from "react-aria-components";

interface MenuProps<T> extends AriaMenuProps<T> {
  placement?: PopoverProps["placement"];
}

export function Menu<T extends object>(props: Readonly<MenuProps<T>>) {
  return (
    <Popover placement={props.placement} className="min-w-[150px] p-px">
      <AriaMenu {...props} className="max-h-[inherit] overflow-auto rounded-t-sm p-1 outline outline-0" />
    </Popover>
  );
}

export function MenuItem(props: Readonly<MenuItemProps>) {
  return (
    <AriaMenuItem {...props} className={dropdownItemStyles}>
      {composeRenderProps(props.children, (children, { selectionMode, isSelected, hasSubmenu }) => (
        <>
          {selectionMode !== "none" && (
            <span className="flex w-4 items-center">
              {isSelected && <Check aria-hidden={true} className="h-4 w-4" />}
            </span>
          )}
          <span className="flex flex-1 items-center gap-2 truncate px-2 py-1 font-normal group-selected:font-semibold">
            {children}
          </span>
          {hasSubmenu && <ChevronRight aria-hidden={true} className="absolute right-2 h-4 w-4" />}
        </>
      ))}
    </AriaMenuItem>
  );
}

export function MenuSeparator(props: Readonly<SeparatorProps>) {
  return <Separator {...props} className="mx-3 my-1 border-border border-b" />;
}

export function MenuSection<T extends object>(props: Readonly<DropdownSectionProps<T>>) {
  return <DropdownSection {...props} className="bg-transparent" />;
}

const keyboardStyles = tv({
  base: "ml-auto text-xs tracking-widest opacity-60"
});

export function MenuKeyboard({ className, ...props }: Readonly<React.HTMLAttributes<HTMLSpanElement>>) {
  return <Keyboard className={keyboardStyles({ className })} {...props} />;
}

export interface MenuHeaderProps extends React.ComponentProps<typeof Header> {
  inset?: boolean;
  separator?: boolean;
}

const headerStyles = tv({
  base: "px-2 py-1.5 font-semibold text-sm",
  variants: {
    inset: {
      true: "pl-8"
    },
    separator: {
      true: "-mx-1 mb-1 border-b border-b-border px-3 pb-[0.625rem]"
    }
  }
});

export function MenuHeader({ className, inset, separator = false, ...props }: Readonly<MenuHeaderProps>) {
  return <Header className={headerStyles({ className, inset, separator })} {...props} />;
}

import { Check } from "lucide-react";
import type {
  MenuProps as AriaMenuProps,
  MenuItemProps,
  SeparatorProps,
} from "react-aria-components";
import {
  Menu as AriaMenu,
  MenuItem as AriaMenuItem,
  Separator,
  composeRenderProps,
} from "react-aria-components";
import type { DropdownSectionProps } from "./ListBox";
import { DropdownSection, dropdownItemStyles } from "./ListBox";
import type { PopoverProps } from "./Popover";
import { Popover } from "./Popover";

interface MenuProps<T> extends AriaMenuProps<T> {
  placement?: PopoverProps["placement"];
}

export function Menu<T extends object>(props: Readonly<MenuProps<T>>) {
  return (
    <Popover placement={props.placement} className="min-w-[150px]">
      <AriaMenu {...props} className="p-1 outline outline-0 max-h-[inherit] overflow-auto [clip-path:inset(0_0_0_0_round_.75rem)]" />
    </Popover>
  );
}

export function MenuItem(props: Readonly<MenuItemProps>) {
  return (
    <AriaMenuItem {...props} className={dropdownItemStyles}>
      {composeRenderProps(props.children, (children, { selectionMode, isSelected }) => (
        <>
          {selectionMode !== "none" && (
            <span className="w-4 flex items-center">
              {isSelected && <Check aria-hidden className="w-4 h-4" />}
            </span>
          )}
          <span className="flex-1 flex items-center gap-2 truncate font-normal group-selected:font-semibold">
            {children}
          </span>
        </>
      ))}
    </AriaMenuItem>
  );
}

export function MenuSeparator(props: Readonly<SeparatorProps>) {
  return <Separator {...props} className="border-b border-gray-300 dark:border-zinc-700 mx-3 my-1" />;
}

export function MenuSection<T extends object>(props: Readonly<DropdownSectionProps<T>>) {
  return <DropdownSection {...props} />;
}

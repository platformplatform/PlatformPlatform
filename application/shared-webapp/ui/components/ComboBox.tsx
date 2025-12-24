/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/index.html?path=/docs/combobox--docs
 * ref: https://ui.shadcn.com/docs/components/combobox
 */
import { ChevronsUpDownIcon } from "lucide-react";
import type React from "react";
import {
  ComboBox as AriaComboBox,
  type ComboBoxProps as AriaComboBoxProps,
  Input as AriaInput,
  ListBox,
  type ListBoxItemProps,
  type ValidationResult
} from "react-aria-components";
import { Button } from "./Button";
import { Description } from "./Description";
import { DropdownItem, DropdownSection, type DropdownSectionProps } from "./Dropdown";
import { FieldError } from "./FieldError";
import { FieldGroup } from "./fieldStyles";
import { LabelWithTooltip } from "./LabelWithTooltip";
import { LegacyPopover } from "./LegacyPopover";
import { composeTailwindRenderProps } from "./utils";

export interface ComboBoxProps<T extends object> extends Omit<AriaComboBoxProps<T>, "children"> {
  label?: string;
  description?: string | null;
  errorMessage?: string | ((validation: ValidationResult) => string);
  tooltip?: string;
  isOpen?: boolean;
  placeholder?: string;
  children: React.ReactNode | ((item: T) => React.ReactNode);
}

export function ComboBox<T extends object>({
  label,
  description,
  errorMessage,
  tooltip,
  isOpen,
  children,
  items,
  ...props
}: Readonly<ComboBoxProps<T>>) {
  return (
    <AriaComboBox {...props} className={composeTailwindRenderProps(props.className, "group flex flex-col gap-3")}>
      {label && <LabelWithTooltip tooltip={tooltip}>{label}</LabelWithTooltip>}
      <FieldGroup>
        <AriaInput className="h-9 w-full min-w-0 border-0 bg-transparent px-2.5 py-1 text-base shadow-none outline-none placeholder:text-muted-foreground focus:ring-0 md:text-sm" />
        <Button
          variant="ghost"
          size="icon"
          className="-me-px h-full w-auto rounded-none rounded-e-md px-2 outline-offset-0"
        >
          <ChevronsUpDownIcon aria-hidden={true} className="h-4 w-4" />
        </Button>
      </FieldGroup>
      {description && <Description>{description}</Description>}
      <FieldError>{errorMessage}</FieldError>
      <LegacyPopover className="w-(--trigger-width) bg-input-background" isOpen={isOpen}>
        <ListBox
          items={items}
          className="max-h-[inherit] overflow-auto p-1 outline-0 [clip-path:inset(0_0_0_0_round_.75rem)]"
        >
          {children}
        </ListBox>
      </LegacyPopover>
    </AriaComboBox>
  );
}

export function ComboBoxItem(props: Readonly<ListBoxItemProps>) {
  return <DropdownItem {...props} />;
}

export function ComboBoxSection<T extends object>(props: Readonly<DropdownSectionProps<T>>) {
  return <DropdownSection {...props} />;
}

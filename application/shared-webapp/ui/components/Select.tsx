/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/select--docs
 * ref: https://ui.shadcn.com/docs/components/select
 */
import { ChevronDown } from "lucide-react";
import type React from "react";
import { useContext } from "react";
import {
  Select as AriaSelect,
  type SelectProps as AriaSelectProps,
  Button,
  FormValidationContext,
  ListBox,
  type ListBoxItemProps,
  SelectValue,
  type ValidationResult
} from "react-aria-components";
import { tv } from "tailwind-variants";
import { Description } from "./Description";
import { DropdownItem, DropdownSection, type DropdownSectionProps } from "./Dropdown";
import { FieldError } from "./FieldError";
import { Label } from "./Label";
import { Popover } from "./Popover";
import { focusRing } from "./focusRing";
import { composeTailwindRenderProps } from "./utils";
export type { Key } from "react-aria-components";

const buttonStyles = tv({
  extend: focusRing,
  base: [
    "flex h-10 w-full min-w-[150px] cursor-default items-center gap-4 py-2 pr-2 pl-3 text-start transition",
    "rounded-md border border-border text-foreground"
  ],
  variants: {
    isInvalid: {
      true: "border-destructive group-invalid:border-destructive forced-colors:group-invalid:border-[Mark]"
    },
    isDisabled: {
      false: "pressed:bg-accent pressed:text-accent-foreground hover:bg-accent/90",
      true: "opacity-50 forced-colors:border-[GrayText] forced-colors:text-[GrayText]"
    }
  }
});

export interface SelectProps<T extends object> extends Omit<AriaSelectProps<T>, "children"> {
  label?: string;
  description?: string;
  errorMessage?: string | ((validation: ValidationResult) => string);
  items?: Iterable<T>;
  children: React.ReactNode | ((item: T) => React.ReactNode);
}

export function Select<T extends object>({
  name,
  label,
  description,
  errorMessage,
  children,
  items,
  className,
  ...props
}: Readonly<SelectProps<T>>) {
  const errors = useContext(FormValidationContext);
  const isInvalid = props.isInvalid || Boolean(name != null && name in errors ? errors?.[name] : undefined);

  return (
    <AriaSelect {...props} name={name} className={composeTailwindRenderProps(className, "group flex flex-col gap-1")}>
      {label && <Label>{label}</Label>}
      <Button className={(renderProps) => buttonStyles({ ...renderProps, isInvalid })}>
        <SelectValue className="flex-1 text-sm placeholder-shown:italic" />
        <ChevronDown
          aria-hidden={true}
          className="h-4 w-4 text-muted-foreground group-disabled:text-muted forced-colors:text-[ButtonText] forced-colors:group-disabled:text-[GrayText]"
        />
      </Button>
      {description && <Description>{description}</Description>}
      <FieldError>{errorMessage}</FieldError>
      <Popover className="min-w-[--trigger-width]">
        <ListBox
          items={items}
          className="max-h-[inherit] overflow-auto p-1 outline-none [clip-path:inset(0_0_0_0_round_.75rem)]"
        >
          {children}
        </ListBox>
      </Popover>
    </AriaSelect>
  );
}

export function SelectItem(props: Readonly<ListBoxItemProps>) {
  return <DropdownItem {...props} />;
}

export function SelectSection<T extends object>(props: Readonly<DropdownSectionProps<T>>) {
  return <DropdownSection {...props} />;
}

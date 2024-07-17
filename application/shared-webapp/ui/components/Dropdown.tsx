/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/select--docs
 */
import { Check } from "lucide-react";
import {
  ListBoxItem as AriaListBoxItem,
  Collection,
  Header,
  type ListBoxItemProps,
  Section,
  type SectionProps,
  composeRenderProps
} from "react-aria-components";
import { tv } from "tailwind-variants";

export const dropdownItemStyles = tv({
  base: "group flex items-center gap-4 cursor-default select-none py-2 pl-3 pr-1 rounded-md outline outline-0 text-sm forced-color-adjust-none",
  variants: {
    isDisabled: {
      false: "text-foreground",
      true: "text-accent forced-colors:text-[GrayText]"
    },
    isFocused: {
      true: "bg-accent text-accent-foreground forced-colors:bg-[Highlight] forced-colors:text-[HighlightText]"
    }
  },
  compoundVariants: [
    {
      isFocused: false,
      isOpen: true,
      className: ""
    }
  ]
});

export function DropdownItem(props: Readonly<ListBoxItemProps>) {
  const textValue = props.textValue ?? (typeof props.children === "string" ? props.children : undefined);
  return (
    <AriaListBoxItem {...props} textValue={textValue} className={dropdownItemStyles}>
      {composeRenderProps(props.children, (children, { isSelected }) => (
        <>
          <span className="flex flex-1 items-center gap-2 truncate font-normal group-selected:font-semibold">
            {children}
          </span>
          <span className="flex w-5 items-center">{isSelected && <Check className="h-4 w-4" />}</span>
        </>
      ))}
    </AriaListBoxItem>
  );
}

export interface DropdownSectionProps<T> extends SectionProps<T> {
  title?: string;
}

export function DropdownSection<T extends object>({
  title,
  items,
  children,
  ...props
}: Readonly<DropdownSectionProps<T>>) {
  return (
    <Section className="first:-mt-[5px] after:block after:h-[5px] after:content-['']" {...props}>
      {title && (
        <Header className="-top-[5px] -mt-px -mx-1 sticky z-10 truncate border-y bg-transparent px-4 py-1 font-semibold text-accent-foreground text-sm backdrop-blur-md supports-[-moz-appearance:none]:bg-accent [&+*]:mt-1">
          {title}
        </Header>
      )}
      <Collection items={items}>{children}</Collection>
    </Section>
  );
}

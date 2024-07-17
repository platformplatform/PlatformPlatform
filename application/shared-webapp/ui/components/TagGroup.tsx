/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/taggroup--docs
 */
import { XIcon } from "lucide-react";
import { createContext, useContext } from "react";
import {
  Tag as AriaTag,
  TagGroup as AriaTagGroup,
  type TagGroupProps as AriaTagGroupProps,
  type TagProps as AriaTagProps,
  Button,
  TagList,
  type TagListProps,
  Text,
  composeRenderProps
} from "react-aria-components";
import { twMerge } from "tailwind-merge";
import { tv } from "tailwind-variants";
import { Description } from "./Description";
import { Label } from "./Label";
import { focusRing } from "./focusRing";

const variants = {
  primary:
    "bg-primary text-primary-foreground border-primary-200 hover:border-primary-300 dark:bg-primary-400/20 dark:text-primary-300 dark:border-primary-400/10 dark:hover:border-primary-400/20",
  secondary:
    "bg-secondary text-secondary-foreground border-secondary-200 hover:border-secondary-300 dark:bg-secondary-400/20 dark:text-secondary-300 dark:border-secondary-400/10 dark:hover:border-secondary-400/20",
  destructive:
    "bg-destructive text-destructive-foreground border-destructive-200 hover:border-destructive-300 dark:bg-destructive-400/20 dark:text-destructive-300 dark:border-destructive-400/10 dark:hover:border-destructive-400/20",
  outline: "border-accent bg-background hover:bg-accent hover:text-accent-foreground pressed:bg-accent/80",
  gray: "bg-gray-100 text-gray-600 border-gray-200 hover:border-gray-300 dark:bg-zinc-700 dark:text-zinc-300 dark:border-zinc-600 dark:hover:border-zinc-500",
  green:
    "bg-green-100 text-green-700 border-green-200 hover:border-green-300 dark:bg-green-300/20 dark:text-green-400 dark:border-green-300/10 dark:hover:border-green-300/20",
  yellow:
    "bg-yellow-100 text-yellow-700 border-yellow-200 hover:border-yellow-300 dark:bg-yellow-300/20 dark:text-yellow-400 dark:border-yellow-300/10 dark:hover:border-yellow-300/20",
  blue: "bg-blue-100 text-blue-700 border-blue-200 hover:border-blue-300 dark:bg-blue-400/20 dark:text-blue-300 dark:border-blue-400/10 dark:hover:border-blue-400/20"
};

type VariantKey = keyof typeof variants;
const VariantContext = createContext<VariantKey>("gray");

const tagStyles = tv({
  extend: focusRing,
  base: "transition cursor-default text-xs rounded-full border px-3 py-0.5 flex items-center max-w-fit gap-1",
  variants: {
    variant: {
      primary: "",
      secondary: "",
      destructive: "",
      outline: "",
      gray: "",
      green: "",
      yellow: "",
      blue: ""
    },
    allowsRemoving: {
      true: "pr-1"
    },
    isSelected: {
      true: "bg-primary text-primary-foreground border-transparent forced-colors:bg-[Highlight] forced-colors:text-[HighlightText] forced-color-adjust-none"
    },
    isDisabled: {
      true: "bg-muted/80 text-muted-foreground forced-colors:text-[GrayText]"
    }
  },
  compoundVariants: (Object.keys(variants) as VariantKey[]).map((variant) => ({
    isSelected: false,
    isDisabled: false,
    variant,
    class: variants[variant]
  }))
});

export interface TagGroupProps<T>
  extends Omit<AriaTagGroupProps, "children">,
    Pick<TagListProps<T>, "items" | "children" | "renderEmptyState"> {
  variant?: VariantKey;
  label?: string;
  description?: string;
  errorMessage?: string;
}

export interface TagProps extends AriaTagProps {
  variant?: VariantKey;
}

export function TagGroup<T extends object>({
  label,
  description,
  errorMessage,
  items,
  variant = "gray",
  className,
  children,
  renderEmptyState,
  ...props
}: Readonly<TagGroupProps<T>>) {
  return (
    <AriaTagGroup {...props} className={twMerge("flex flex-col gap-1", className)}>
      <Label>{label}</Label>
      <VariantContext.Provider value={variant}>
        <TagList items={items} renderEmptyState={renderEmptyState} className="flex flex-wrap gap-1">
          {children}
        </TagList>
      </VariantContext.Provider>
      {description && <Description>{description}</Description>}
      {errorMessage && (
        <Text slot="errorMessage" className="text-destructive text-sm">
          {errorMessage}
        </Text>
      )}
    </AriaTagGroup>
  );
}

const removeButtonStyles = tv({
  extend: focusRing,
  base: "cursor-default rounded-full transition-[background-color] p-0.5 flex items-center justify-center hover:bg-black/10 dark:hover:bg-white/10 pressed:bg-black/20 dark:pressed:bg-white/20"
});

export function Tag({ children, variant, ...props }: Readonly<TagProps>) {
  const textValue = typeof children === "string" ? children : undefined;
  const groupVariant = useContext(VariantContext);
  return (
    <AriaTag
      textValue={textValue}
      {...props}
      className={composeRenderProps(props.className, (className, renderProps) =>
        tagStyles({ ...renderProps, className, variant: variant ?? groupVariant })
      )}
    >
      {({ allowsRemoving }) => (
        <>
          {children}
          {allowsRemoving && (
            <Button slot="remove" className={removeButtonStyles}>
              <XIcon aria-hidden className="h-3 w-3" />
            </Button>
          )}
        </>
      )}
    </AriaTag>
  );
}

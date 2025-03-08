/**
 * ref: https://react-spectrum.adobe.com/react-spectrum/StatusLight.html
 * ref: https://react.fluentui.dev/?path=/docs/components-badge-presencebadge--default
 */
import type { PropsWithChildren } from "react";
import { tv } from "tailwind-variants";

const lightStyles = tv({
  base: "mr-3 rounded-full",
  variants: {
    variant: {
      neutral: "bg-muted",
      success: "bg-success",
      warning: "bg-warning",
      danger: "bg-danger",
      info: "bg-info",
      primary: "bg-primary",
      secondary: "bg-secondary"
    },
    size: {
      sm: "h-2 w-2",
      md: "h-2.5 w-2.5",
      lg: "h-3 w-3",
      xl: "h-3.5 w-3.5"
    },
    isDisabled: {
      true: "bg-muted-foreground/50"
    }
  },
  defaultVariants: {
    variant: "neutral",
    size: "md"
  }
});

const statusStyles = tv({
  base: "font-medium",
  variants: {
    variant: {
      neutral: "text-muted-foreground italic",
      success: "",
      warning: "",
      danger: "",
      info: "",
      primary: "",
      secondary: ""
    },
    size: {
      sm: "text-xs",
      md: "text-sm",
      lg: "text-base",
      xl: "text-lg"
    },
    isDisabled: {
      true: "text-muted-foreground/50"
    }
  },
  defaultVariants: {
    variant: "neutral",
    size: "md"
  }
});

type Size = keyof typeof statusStyles.variants.size;
type Variants = keyof typeof lightStyles.variants.variant;

type StatusLightProps = {
  variant?: Variants;
  size?: Size;
  isDisabled?: boolean;
} & PropsWithChildren;

export function StatusLight({ variant, size, isDisabled, children }: StatusLightProps) {
  return (
    <div className="flex items-center">
      <div className={lightStyles({ variant, size, isDisabled })} />
      <span className={statusStyles({ variant, size, isDisabled })}>{children}</span>
    </div>
  );
}

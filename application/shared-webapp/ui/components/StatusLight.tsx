/**
 * ref: https://react-spectrum.adobe.com/react-spectrum/StatusLight.html
 * ref: https://react.fluentui.dev/?path=/docs/components-badge-presencebadge--default
 */
import { cva, type VariantProps } from "class-variance-authority";
import type { PropsWithChildren } from "react";
import { cn } from "../utils";

const lightStyles = cva("mr-3 rounded-full", {
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
      true: "bg-muted-foreground/50",
      false: ""
    }
  },
  defaultVariants: {
    variant: "neutral",
    size: "md",
    isDisabled: false
  }
});

const statusStyles = cva("font-medium", {
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
      true: "text-muted-foreground/50",
      false: ""
    }
  },
  defaultVariants: {
    variant: "neutral",
    size: "md",
    isDisabled: false
  }
});

type StatusLightProps = {
  variant?: VariantProps<typeof lightStyles>["variant"];
  size?: VariantProps<typeof lightStyles>["size"];
  isDisabled?: boolean;
} & PropsWithChildren;

export function StatusLight({ variant, size, isDisabled, children }: StatusLightProps) {
  return (
    <div className="flex items-center">
      <div className={cn(lightStyles({ variant, size, isDisabled }))} />
      <span className={cn(statusStyles({ variant, size, isDisabled }))}>{children}</span>
    </div>
  );
}

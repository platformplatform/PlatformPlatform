/**
 * ref: https://react.fluentui.dev/?path=/docs/components-badge-badge--default
 * ref: https://ui.shadcn.com/docs/components/badge
 */
import type { PropsWithChildren } from "react";
import { tv } from "tailwind-variants";

const styles = tv({
  base: "flex h-6 w-fit items-center justify-center gap-2 truncate rounded-full px-2 py-1 font-medium text-xs [&>svg]:h-5",
  variants: {
    variant: {
      neutral: "bg-muted text-muted-foreground",
      success: "bg-success text-success-foreground",
      warning: "bg-warning text-warning-foreground",
      danger: "bg-danger text-danger-foreground",
      info: "bg-info text-info-foreground",
      primary: "bg-primary text-primary-foreground",
      secondary: "bg-secondary text-secondary-foreground",
      outline: "border border-muted-foreground text-muted-foreground"
    }
  },
  defaultVariants: {
    variant: "neutral"
  }
});

type Variant = keyof typeof styles.variants.variant;

type BadgeProps = {
  variant?: Variant;
  className?: string;
} & PropsWithChildren;

export function Badge({ variant, className, children }: BadgeProps) {
  return <div className={styles({ variant, className })}>{children}</div>;
}

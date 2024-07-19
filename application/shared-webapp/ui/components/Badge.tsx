/**
 * ref: https://react.fluentui.dev/?path=/docs/components-badge-badge--default
 * ref: https://ui.shadcn.com/docs/components/badge
 */
import type { PropsWithChildren } from "react";
import { tv } from "tailwind-variants";

const styles = tv({
  base: "flex gap-2 py-1 px-2 h-6 w-fit rounded-full text-xs font-medium items-center justify-center [&>svg]:h-5",
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
} & PropsWithChildren;

export function Badge({ variant, children }: BadgeProps) {
  return <div className={styles({ variant })}>{children}</div>;
}

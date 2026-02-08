import { Toggle as TogglePrimitive } from "@base-ui/react/toggle";
import { cva, type VariantProps } from "class-variance-authority";

import { cn } from "../utils";

// NOTE: This diverges from stock ShadCN to use outline-based focus ring instead of ring utilities,
// and active:bg-accent for press feedback.
const toggleVariants = cva(
  "group/toggle inline-flex cursor-pointer items-center justify-center gap-1 whitespace-nowrap rounded-md font-medium text-sm outline-ring transition-[color,box-shadow] hover:bg-muted hover:text-foreground focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 active:bg-accent disabled:pointer-events-none disabled:opacity-50 aria-pressed:bg-muted aria-invalid:border-destructive aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 [&_svg:not([class*='size-'])]:size-4 [&_svg]:pointer-events-none [&_svg]:shrink-0",
  {
    variants: {
      variant: {
        default: "bg-transparent",
        outline: "border border-input bg-transparent shadow-xs hover:bg-muted"
      },
      // NOTE: This diverges from stock ShadCN to use CSS variable heights for Apple HIG compliance (44px tap targets).
      size: {
        default: "h-[var(--control-height)] min-w-[var(--control-height)] px-2",
        sm: "h-[var(--control-height-sm)] min-w-[var(--control-height-sm)] px-1.5",
        lg: "h-[var(--control-height-lg)] min-w-[var(--control-height-lg)] px-2.5"
      }
    },
    defaultVariants: {
      variant: "default",
      size: "default"
    }
  }
);

function Toggle({
  className,
  variant = "default",
  size = "default",
  ...props
}: TogglePrimitive.Props & VariantProps<typeof toggleVariants>) {
  return <TogglePrimitive data-slot="toggle" className={cn(toggleVariants({ variant, size, className }))} {...props} />;
}

export { Toggle, toggleVariants };

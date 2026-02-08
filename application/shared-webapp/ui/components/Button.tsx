import { Button as ButtonPrimitive } from "@base-ui/react/button";
import { cva, type VariantProps } from "class-variance-authority";

import { cn } from "../utils";

// NOTE: This diverges from stock ShadCN to use outline-based focus ring and per-variant active backgrounds for press feedback.
const buttonVariants = cva(
  "group/button inline-flex shrink-0 cursor-pointer select-none items-center justify-center whitespace-nowrap rounded-md font-medium text-sm transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 disabled:pointer-events-none disabled:opacity-50 aria-invalid:border-destructive aria-invalid:ring-[0.1875rem] aria-invalid:ring-destructive/20 dark:aria-invalid:border-destructive/50 dark:aria-invalid:ring-destructive/40 [&_svg:not([class*='size-'])]:size-4 [&_svg]:pointer-events-none [&_svg]:shrink-0",
  {
    variants: {
      variant: {
        default: "bg-primary text-primary-foreground outline-primary hover:bg-primary/80 active:bg-primary/70",
        // NOTE: This diverges from stock ShadCN to use bg-white instead of bg-background for light mode.
        outline:
          "border-border bg-white shadow-xs outline-ring hover:bg-muted hover:text-foreground active:bg-accent aria-expanded:bg-muted aria-expanded:text-foreground dark:border-input dark:bg-input/30 dark:active:bg-input/60 dark:hover:bg-input/50",
        // NOTE: This diverges from stock ShadCN to use bg-white instead of bg-secondary for a neutral look.
        secondary:
          "bg-white text-foreground outline-ring hover:bg-muted active:bg-accent aria-expanded:bg-muted dark:bg-input/30 dark:active:bg-input/60 dark:hover:bg-input/50",
        ghost:
          "outline-ring hover:bg-muted hover:text-foreground active:bg-accent aria-expanded:bg-muted aria-expanded:text-foreground dark:active:bg-muted/70 dark:hover:bg-muted/50",
        // NOTE: This diverges from stock ShadCN to use solid background with white text for accessibility.
        destructive:
          "bg-destructive text-destructive-foreground outline-destructive hover:bg-destructive/90 active:bg-destructive/80",
        link: "text-primary underline-offset-4 outline-ring hover:underline active:opacity-70"
      },
      // NOTE: This diverges from stock ShadCN to use CSS variable heights for Apple HIG compliance (44px default tap targets).
      size: {
        default:
          "h-[var(--control-height)] w-fit min-w-11 gap-1.5 in-data-[slot=button-group]:rounded-md px-2.5 has-data-[icon=inline-end]:pr-2 has-data-[icon=inline-start]:pl-2",
        xs: "h-[var(--control-height-xs)] min-w-7 gap-1 in-data-[slot=button-group]:rounded-md rounded-[min(var(--radius-md),0.5rem)] px-2 text-xs has-data-[icon=inline-end]:pr-1.5 has-data-[icon=inline-start]:pl-1.5 [&_svg:not([class*='size-'])]:size-3",
        sm: "h-[var(--control-height-sm)] min-w-9 gap-1 in-data-[slot=button-group]:rounded-md rounded-[min(var(--radius-md),0.625rem)] px-2.5 has-data-[icon=inline-end]:pr-1.5 has-data-[icon=inline-start]:pl-1.5",
        lg: "h-[var(--control-height-lg)] w-fit min-w-12 gap-1.5 px-2.5 has-data-[icon=inline-end]:pr-3 has-data-[icon=inline-start]:pl-3",
        icon: "h-11 w-11 min-w-11 p-0",
        "icon-xs":
          "h-7 w-7 min-w-7 in-data-[slot=button-group]:rounded-md rounded-[min(var(--radius-md),0.5rem)] p-0 [&_svg:not([class*='size-'])]:size-3",
        "icon-sm":
          "h-9 w-9 min-w-9 in-data-[slot=button-group]:rounded-md rounded-[min(var(--radius-md),0.625rem)] p-0",
        "icon-lg": "h-12 w-12 min-w-12 p-0"
      }
    },
    defaultVariants: {
      variant: "default",
      size: "default"
    }
  }
);

function Button({
  className,
  variant = "default",
  size = "default",
  ...props
}: ButtonPrimitive.Props & VariantProps<typeof buttonVariants>) {
  return <ButtonPrimitive data-slot="button" className={cn(buttonVariants({ variant, size, className }))} {...props} />;
}

export { Button, buttonVariants };

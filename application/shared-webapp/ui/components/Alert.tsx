import { cva, type VariantProps } from "class-variance-authority";

import { cn } from "../utils";

const alertVariants = cva(
  "relative flex w-full items-start gap-3 rounded-lg border p-4 text-sm [&>svg]:size-5 [&>svg]:shrink-0",
  {
    variants: {
      variant: {
        default: "border-border bg-muted/50 text-foreground [&>svg]:text-foreground",
        destructive: "border-destructive/50 bg-destructive/10 text-destructive [&>svg]:text-destructive",
        warning: "border-warning/50 bg-warning/10 text-warning-foreground [&>svg]:text-warning",
        info: "border-info/50 bg-info/10 text-info-foreground [&>svg]:text-info"
      }
    },
    defaultVariants: {
      variant: "default"
    }
  }
);

function Alert({ className, variant, ...props }: React.ComponentProps<"div"> & VariantProps<typeof alertVariants>) {
  return <div data-slot="alert" role="alert" className={cn(alertVariants({ variant }), className)} {...props} />;
}

function AlertTitle({ className, ...props }: React.ComponentProps<"h5">) {
  return <h5 data-slot="alert-title" className={cn("font-medium leading-none", className)} {...props} />;
}

function AlertDescription({ className, ...props }: React.ComponentProps<"p">) {
  return <p data-slot="alert-description" className={cn("text-sm", className)} {...props} />;
}

export { Alert, AlertTitle, AlertDescription, alertVariants };

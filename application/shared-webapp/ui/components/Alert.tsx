import { cva, type VariantProps } from "class-variance-authority";

import { cn } from "../utils";

const alertVariants = cva(
  "relative w-full rounded-lg border p-4 text-sm [&>svg]:absolute [&>svg]:top-4 [&>svg]:left-4 [&>svg]:size-5 [&>svg~*]:pl-7",
  {
    variants: {
      variant: {
        default: "border-border bg-muted/50 text-foreground [&>svg]:text-foreground",
        destructive: "border-destructive/50 bg-destructive/10 text-destructive [&>svg]:text-destructive",
        warning: "border-warning/50 bg-warning/10 text-warning [&>svg]:text-warning",
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

function AlertTitle({ className, children, ...props }: React.ComponentProps<"div">) {
  return (
    <div data-slot="alert-title" className={cn("mb-1 leading-none font-medium tracking-tight", className)} {...props}>
      {children}
    </div>
  );
}

function AlertDescription({ className, ...props }: React.ComponentProps<"div">) {
  return <div data-slot="alert-description" className={cn("text-sm", className)} {...props} />;
}

export { Alert, AlertTitle, AlertDescription, alertVariants };

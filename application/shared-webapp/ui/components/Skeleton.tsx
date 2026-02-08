import { cn } from "../utils";

// NOTE: This diverges from stock ShadCN to use bg-muted instead of bg-accent for better visual consistency.
function Skeleton({ className, ...props }: React.ComponentProps<"div">) {
  return <div data-slot="skeleton" className={cn("animate-pulse rounded-md bg-muted", className)} {...props} />;
}

export { Skeleton };

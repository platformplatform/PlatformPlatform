import { Tabs as TabsPrimitive } from "@base-ui/react/tabs";
import { cva, type VariantProps } from "class-variance-authority";

import { cn } from "../utils";

function Tabs({ className, ...props }: TabsPrimitive.Root.Props) {
  return <TabsPrimitive.Root data-slot="tabs" className={cn("flex flex-col gap-2", className)} {...props} />;
}

function TabsList({ className, ...props }: TabsPrimitive.List.Props) {
  return (
    <TabsPrimitive.List
      data-slot="tabs-list"
      className={cn("relative flex gap-1 border-border border-b", className)}
      {...props}
    />
  );
}

// NOTE: This diverges from stock ShadCN to use outline-based focus ring, CSS variable heights for Apple HIG compliance,
// data-[active] selectors instead of data-[selected] (BaseUI uses data-active, not data-selected like Radix),
// and active:bg-muted/50 for press feedback.
const tabTriggerVariants = cva(
  "relative inline-flex cursor-pointer items-center justify-center gap-2 whitespace-nowrap rounded-md px-4 py-2 font-semibold not-data-[active]:text-muted-foreground text-sm outline-ring transition-colors after:absolute after:right-1 after:-bottom-px after:left-1 after:h-0.5 not-data-[active]:after:bg-transparent after:transition-colors not-data-[active]:hover:text-muted-foreground/90 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 active:bg-muted/50 disabled:pointer-events-none disabled:opacity-50 data-[active]:text-foreground data-[active]:after:bg-primary",
  {
    variants: {
      size: {
        default: "h-[var(--control-height)]",
        sm: "h-[var(--control-height-sm)] text-xs",
        lg: "h-[var(--control-height-lg)]"
      }
    },
    defaultVariants: {
      size: "default"
    }
  }
);

function TabsTrigger({
  className,
  size = "default",
  ...props
}: TabsPrimitive.Tab.Props & VariantProps<typeof tabTriggerVariants>) {
  return (
    <TabsPrimitive.Tab data-slot="tabs-trigger" className={cn(tabTriggerVariants({ size, className }))} {...props} />
  );
}

function TabsContent({ className, ...props }: TabsPrimitive.Panel.Props) {
  return (
    <TabsPrimitive.Panel
      data-slot="tabs-content"
      className={cn(
        "outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2",
        className
      )}
      {...props}
    />
  );
}

export { Tabs, TabsList, TabsTrigger, TabsContent, tabTriggerVariants };

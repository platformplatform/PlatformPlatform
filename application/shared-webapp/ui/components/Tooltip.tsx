import { Tooltip as TooltipPrimitive } from "@base-ui/react/tooltip";
import { cn } from "../utils";

export const DEFAULT_TOOLTIP_DELAY = 200;

function TooltipProvider({ children, ...props }: TooltipPrimitive.Provider.Props) {
  return (
    <TooltipPrimitive.Provider delay={DEFAULT_TOOLTIP_DELAY} {...props}>
      {children}
    </TooltipPrimitive.Provider>
  );
}

function Tooltip({ ...props }: TooltipPrimitive.Root.Props) {
  return <TooltipPrimitive.Root data-slot="tooltip" {...props} />;
}

function TooltipTrigger({ className, ...props }: TooltipPrimitive.Trigger.Props) {
  return (
    <TooltipPrimitive.Trigger data-slot="tooltip-trigger" className={cn("cursor-default", className)} {...props} />
  );
}

export interface TooltipContentProps
  extends TooltipPrimitive.Popup.Props,
    Pick<TooltipPrimitive.Positioner.Props, "side" | "sideOffset" | "align" | "alignOffset"> {}

function TooltipContent({
  className,
  side = "top",
  sideOffset = 8,
  align = "center",
  alignOffset = 0,
  children,
  ...props
}: TooltipContentProps) {
  return (
    <TooltipPrimitive.Portal>
      <TooltipPrimitive.Positioner side={side} sideOffset={sideOffset} align={align} alignOffset={alignOffset}>
        <TooltipPrimitive.Popup
          data-slot="tooltip-content"
          className={cn(
            "z-50 overflow-hidden rounded-md border border-border bg-popover px-3 py-1 text-popover-foreground text-sm shadow-md",
            "origin-(--transform-origin) transition-[opacity,transform] duration-200",
            "data-[starting-style]:scale-95 data-[starting-style]:opacity-0",
            "data-[ending-style]:scale-95 data-[ending-style]:opacity-0",
            className
          )}
          {...props}
        >
          {children}
        </TooltipPrimitive.Popup>
      </TooltipPrimitive.Positioner>
    </TooltipPrimitive.Portal>
  );
}

export { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger };

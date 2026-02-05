import { Tooltip as TooltipPrimitive } from "@base-ui/react/tooltip";
import { createContext, use, useState } from "react";

import { cn } from "../utils";

function TooltipProvider({ delay = 0, ...props }: TooltipPrimitive.Provider.Props) {
  return <TooltipPrimitive.Provider data-slot="tooltip-provider" delay={delay} {...props} />;
}

// Context to pass setOpen from Tooltip to TooltipTrigger for tap-to-open functionality
const TooltipOpenContext = createContext<(() => void) | null>(null);

// NOTE: This diverges from stock ShadCN to support tap-to-open on touch devices.
// Uses controlled state to allow TooltipTrigger to toggle the tooltip on click/tap.
function Tooltip({ open: controlledOpen, onOpenChange, ...props }: TooltipPrimitive.Root.Props) {
  const [internalOpen, setInternalOpen] = useState(false);
  const isControlled = controlledOpen !== undefined;
  const open = isControlled ? controlledOpen : internalOpen;

  const handleOpenChange: TooltipPrimitive.Root.Props["onOpenChange"] = (newOpen, eventDetails) => {
    // On touch devices, prevent the close on trigger-press since we handle toggle via onClick
    if (!newOpen && eventDetails.reason === "trigger-press") {
      eventDetails.cancel();
      return;
    }
    if (!isControlled) {
      setInternalOpen(newOpen);
    }
    onOpenChange?.(newOpen, eventDetails);
  };

  // Toggle function for tap-to-open on touch devices
  const toggleOpen = () => {
    const newOpen = !open;
    if (!isControlled) {
      setInternalOpen(newOpen);
    }
    // Call onOpenChange with a synthetic event details object for click/tap
    onOpenChange?.(newOpen, { reason: "trigger-press" } as TooltipPrimitive.Root.ChangeEventDetails);
  };

  return (
    <TooltipProvider>
      <TooltipOpenContext value={toggleOpen}>
        <TooltipPrimitive.Root data-slot="tooltip" open={open} onOpenChange={handleOpenChange} {...props} />
      </TooltipOpenContext>
    </TooltipProvider>
  );
}

// NOTE: This diverges from stock ShadCN to add tap-to-open support for touch devices.
function TooltipTrigger({ className, onClick, ...props }: TooltipPrimitive.Trigger.Props) {
  const toggleOpen = use(TooltipOpenContext);

  const handleClick: TooltipPrimitive.Trigger.Props["onClick"] = (event) => {
    // Toggle tooltip on click/tap for touch device support
    toggleOpen?.();
    onClick?.(event);
  };

  return (
    <TooltipPrimitive.Trigger data-slot="tooltip-trigger" className={className} onClick={handleClick} {...props} />
  );
}

function TooltipContent({
  className,
  side = "top",
  sideOffset = 4,
  align = "center",
  alignOffset = 0,
  children,
  ...props
}: TooltipPrimitive.Popup.Props &
  Pick<TooltipPrimitive.Positioner.Props, "align" | "alignOffset" | "side" | "sideOffset">) {
  return (
    <TooltipPrimitive.Portal>
      <TooltipPrimitive.Positioner
        align={align}
        alignOffset={alignOffset}
        side={side}
        sideOffset={sideOffset}
        className="isolate z-50"
      >
        <TooltipPrimitive.Popup
          data-slot="tooltip-content"
          className={cn(
            "data-open:fade-in-0 data-open:zoom-in-95 data-[state=delayed-open]:fade-in-0 data-[state=delayed-open]:zoom-in-95 data-closed:fade-out-0 data-closed:zoom-out-95 data-[side=bottom]:slide-in-from-top-2 data-[side=left]:slide-in-from-right-2 data-[side=right]:slide-in-from-left-2 data-[side=top]:slide-in-from-bottom-2 z-50 w-fit max-w-xs origin-(--transform-origin) rounded-md bg-foreground px-3 py-1.5 text-background text-sm data-[state=delayed-open]:animate-in data-closed:animate-out data-open:animate-in",
            className
          )}
          {...props}
        >
          {children}
          <TooltipPrimitive.Arrow className="z-50 size-2.5 translate-y-[calc(-50%_-_2px)] rotate-45 rounded-[0.125rem] bg-foreground fill-foreground data-[side=bottom]:top-1 data-[side=left]:top-1/2! data-[side=right]:top-1/2! data-[side=left]:-right-1 data-[side=top]:-bottom-2.5 data-[side=right]:-left-1 data-[side=left]:-translate-y-1/2 data-[side=right]:-translate-y-1/2" />
        </TooltipPrimitive.Popup>
      </TooltipPrimitive.Positioner>
    </TooltipPrimitive.Portal>
  );
}

export { Tooltip, TooltipTrigger, TooltipContent, TooltipProvider };

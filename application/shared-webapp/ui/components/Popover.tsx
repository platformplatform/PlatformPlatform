/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/popover--docs
 * ref: https://ui.shadcn.com/docs/components/popover
 */
import type React from "react";
import {
  Popover as AriaPopover,
  type PopoverProps as AriaPopoverProps,
  OverlayArrow,
  PopoverContext,
  composeRenderProps,
  useSlottedContext
} from "react-aria-components";
import { tv } from "tailwind-variants";

export interface PopoverProps extends Omit<AriaPopoverProps, "children"> {
  showArrow?: boolean;
  children: React.ReactNode;
}

const styles = tv({
  base: "bg-popover backdrop-blur-2xl forced-colors:bg-[Canvas] shadow-lg rounded-lg border text-popover-foreground",
  variants: {
    isEntering: {
      true: "animate-in fade-in-0 zoom-in-95 placement-bottom:slide-in-from-top-2 placement-top:slide-in-from-bottom-2 placement-left:slide-in-from-right-2 placement-right:slide-in-from-left-2 ease-out duration-200"
    },
    isExiting: {
      true: "animate-out fade-out-0 zoom-out-95 placement-bottom:slide-out-to-top-2 placement-top:slide-out-to-bottom-2 placement-left:slide-out-to-right-2 placement-right:slide-out-to-left-2 ease-in duration-150"
    }
  }
});

export function Popover({ children, showArrow, className, ...props }: Readonly<PopoverProps>) {
  const popoverContext = useSlottedContext(PopoverContext);
  const isSubmenu = popoverContext?.trigger === "SubmenuTrigger";
  let offset = showArrow ? 12 : 8;
  offset = isSubmenu ? offset - 6 : offset;
  return (
    <AriaPopover
      offset={offset}
      {...props}
      className={composeRenderProps(className, (className, renderProps) => styles({ ...renderProps, className }))}
    >
      {showArrow && (
        <OverlayArrow className="group">
          <svg
            width={12}
            height={12}
            viewBox="0 0 12 12"
            className="group-placement-left:-rotate-90 block fill-popover stroke-1 stroke-border group-placement-bottom:rotate-180 group-placement-right:rotate-90 forced-colors:fill-[Canvas] forced-colors:stroke-[ButtonBorder]"
          >
            <title>Popover arrow</title>
            <path d="M0 0 L6 6 L12 0" />
          </svg>
        </OverlayArrow>
      )}
      {children}
    </AriaPopover>
  );
}

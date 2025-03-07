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
  base: "rounded-lg border bg-popover text-popover-foreground shadow-lg backdrop-blur-2xl transition-opacity forced-colors:bg-[Canvas]",
  variants: {
    isEntering: {
      true: "fade-in-0 animate-in duration-200 ease-out"
    },
    isExiting: {
      true: "fade-out-0 animate-out duration-150 ease-in"
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

/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/tooltip--docs
 * ref: https://ui.shadcn.com/docs/components/tooltip
 */
import type React from "react";
import {
  Tooltip as AriaTooltip,
  type TooltipProps as AriaTooltipProps,
  OverlayArrow,
  composeRenderProps
} from "react-aria-components";
import { tv } from "tailwind-variants";

export { TooltipTrigger } from "react-aria-components";

export interface TooltipProps extends Omit<AriaTooltipProps, "children"> {
  children: React.ReactNode;
}

const styles = tv({
  base: "group rounded-md border border-border bg-popover px-3 py-1 text-popover-foreground text-sm shadow-md drop-shadow-lg will-change-transform",
  variants: {
    isEntering: {
      true: "fade-in placement-bottom:slide-in-from-top-0.5 placement-top:slide-in-from-bottom-0.5 placement-left:slide-in-from-right-0.5 placement-right:slide-in-from-left-0.5 animate-in duration-200 ease-out"
    },
    isExiting: {
      true: "fade-out placement-bottom:slide-out-to-top-0.5 placement-top:slide-out-to-bottom-0.5 placement-left:slide-out-to-right-0.5 placement-right:slide-out-to-left-0.5 animate-out duration-150 ease-in"
    }
  }
});

export function Tooltip({ children, ...props }: Readonly<TooltipProps>) {
  return (
    <AriaTooltip
      {...props}
      offset={10}
      className={composeRenderProps(props.className, (className, renderProps) => styles({ ...renderProps, className }))}
    >
      <OverlayArrow>
        <svg
          width={8}
          height={8}
          viewBox="0 0 8 8"
          className="group-placement-left:-rotate-90 fill-popover stroke-border group-placement-bottom:rotate-180 group-placement-right:rotate-90 forced-colors:fill-[Canvas] forced-colors:stroke-[ButtonBorder]"
        >
          <title>Tooltip arrow</title>
          <path d="M0 0 L4 4 L8 0" />
        </svg>
      </OverlayArrow>
      {children}
    </AriaTooltip>
  );
}

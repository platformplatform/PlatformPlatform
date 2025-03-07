/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/switch--docs
 * ref: https://ui.shadcn.com/docs/components/switch
 */
import type React from "react";
import { Switch as AriaSwitch, type SwitchProps as AriaSwitchProps } from "react-aria-components";
import { tv } from "tailwind-variants";
import { focusRing } from "./focusRing";
import { composeTailwindRenderProps } from "./utils";

export interface SwitchProps extends Omit<AriaSwitchProps, "children"> {
  children: React.ReactNode;
}

const trackStyles = tv({
  extend: focusRing,
  base: "flex h-6 w-11 shrink-0 cursor-default items-center rounded-full border border-transparent px-px shadow-inner transition duration-200 ease-in-out",
  variants: {
    isSelected: {
      false: "bg-input group-pressed:bg-input/80",
      true: "forced-colors:!bg-[Highlight] bg-primary group-pressed:bg-primary/80"
    },
    isDisabled: {
      true: "cursor-not-allowed opacity-50"
    }
  }
});

const handleStyles = tv({
  base: "-outline-offset-1 h-5 w-5 transform rounded-full bg-background shadow outline outline-1 outline-transparent transition duration-200 ease-in-out",
  variants: {
    isSelected: {
      false: "translate-x-0",
      true: "translate-x-[100%]"
    },
    isDisabled: {
      true: "forced-colors:outline-[GrayText]"
    }
  }
});

export function Switch({ children, className, ...props }: Readonly<SwitchProps>) {
  return (
    <AriaSwitch
      {...props}
      className={composeTailwindRenderProps(
        className,
        "group flex items-center gap-2 text-muted-foreground text-sm transition disabled:opacity-50 forced-colors:disabled:text-[GrayText]"
      )}
    >
      {(renderProps) => (
        <>
          <div className={trackStyles(renderProps)}>
            <span className={handleStyles(renderProps)} />
          </div>
          {children}
        </>
      )}
    </AriaSwitch>
  );
}

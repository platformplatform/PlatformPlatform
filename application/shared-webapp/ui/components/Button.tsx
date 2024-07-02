import type { ButtonProps as RACButtonProps } from "react-aria-components";
import { Button as RACButton, composeRenderProps } from "react-aria-components";
import { tv } from "tailwind-variants";
import { focusRing } from "./utils";

export interface ButtonProps extends RACButtonProps {
  variant?: "primary" | "secondary" | "destructive" | "neutral" | "icon" | "ghost";
}

const button = tv({
  extend: focusRing,
  base: "px-5 py-2 text-sm text-center transition font-semibold rounded-lg text-base shadow-[inset_0_1px_0_0_rgba(255,255,255,0.1)] dark:shadow-none cursor-default",
  variants: {
    variant: {
      primary: "bg-neutral-900 hover:bg-neutral-700 pressed:bg-neutral-800 text-white border-gray-700",
      secondary:
        "bg-gray-200 hover:bg-gray-300 pressed:bg-gray-400 slate-700 dark:bg-zinc-600 dark:hover:bg-zinc-500 dark:pressed:bg-zinc-400 dark:text-zinc-100",
      neutral: "bg-neutral-600 hover:bg-neutral-700 pressed:bg-neutral-800 text-white",
      destructive: "bg-red-700 hover:bg-red-800 pressed:bg-red-900 text-white",
      icon: "border-0 p-1 flex items-center justify-center text-gray-600 hover:bg-black/[5%] pressed:bg-black/10 dark:text-zinc-400 dark:hover:bg-white/10 dark:pressed:bg-white/20 disabled:bg-transparent",
      ghost: ""
    },
    isDisabled: {
      true: "bg-gray-100 dark:bg-zinc-800 text-gray-300 dark:text-zinc-600 forced-colors:text-[GrayText] border-black/5 dark:border-white/5"
    }
  },
  defaultVariants: {
    variant: "primary"
  }
});

export function Button(props: Readonly<ButtonProps>) {
  return (
    <RACButton
      {...props}
      className={composeRenderProps(props.className, (className, renderProps) =>
        button({ ...renderProps, variant: props.variant, className })
      )}
    />
  );
}

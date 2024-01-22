import { composeRenderProps } from "react-aria-components";
import { twMerge } from "tailwind-merge";
import { tv } from "tailwind-variants";

export const focusRing = tv({
  base: "outline outline-blue-600 dark:outline-blue-500 forced-colors:outline-[Highlight] outline-offset-2",
  variants: {
    isFocusVisible: {
      false: "outline-0",
      true: "outline-2",
    },
  },
});

export type ComposeClass<T> = string | ((v: T) => string);

export function composeTailwindRenderProps<T>(className: ComposeClass<T> | undefined, tw: string): ComposeClass<T> {
  return composeRenderProps(className, className => twMerge(tw, className));
}

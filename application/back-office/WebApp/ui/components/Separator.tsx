import type { SeparatorProps } from "react-aria-components";
import { Separator as RACSeparator } from "react-aria-components";
import { tv } from "tailwind-variants";

const styles = tv({
  base: "bg-gray-300 dark:bg-zinc-600 forced-colors:bg-[ButtonBorder]",
  variants: {
    orientation: {
      horizontal: "h-px w-full",
      vertical: "w-px",
    },
  },
  defaultVariants: {
    orientation: "horizontal",
  },
});

export function Separator(props: Readonly<SeparatorProps>) {
  return (
    <RACSeparator
      {...props}
      className={styles({ orientation: props.orientation, className: props.className })}
    />
  );
}

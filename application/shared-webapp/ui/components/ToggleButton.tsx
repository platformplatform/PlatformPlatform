/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/togglebutton--docs
 * ref: https://ui.shadcn.com/docs/components/toggle
 */
import {
  ToggleButton as AriaToggleButton,
  type ToggleButtonProps as AriaToggleButtonProps,
  composeRenderProps
} from "react-aria-components";
import { tv } from "tailwind-variants";
import { focusRing } from "./focusRing";

export type ToggleButtonProps = AriaToggleButtonProps & {
  /**
   * The variant of the toggle button.
   * @default primary
   */
  variant?: "primary" | "secondary" | "destructive" | "outline" | "ghost";
  /**
   * The size of the toggle button.
   * @default md
   */
  size?: "md" | "sm" | "lg";
};

const styles = tv({
  extend: focusRing,
  base: "inline-flex cursor-pointer items-center justify-center gap-1 whitespace-nowrap rounded-md px-5 py-2 text-sm transition forced-color-adjust-none forced-colors:border-[ButtonBorder]",
  variants: {
    variant: {
      primary: "[--color:theme(colors.primary.DEFAULT)] [--text-color:theme(colors.primary.foreground)]",
      secondary: "[--color:theme(colors.secondary.DEFAULT)] [--text-color:theme(colors.secondary.foreground)]",
      destructive: "[--color:theme(colors.destructive.DEFAULT)] [--text-color:theme(colors.destructive.foreground)]",
      outline:
        "border border-input [--color:theme(colors.accent.DEFAULT)] [--text-color:theme(colors.accent.foreground)]",
      ghost: "[--color:theme(colors.accent.DEFAULT)] [--text-color:theme(colors.accent.foreground)]"
    },
    size: {
      sm: "h-9 px-3",
      md: "h-10 px-4 py-2",
      lg: "h-11 px-6"
    },
    isDisabled: {
      true: "pointer-events-none opacity-50"
    },
    isSelected: {
      false:
        "bg-background text-foreground pressed:opacity-90 hover:bg-[--color] hover:text-[--text-color] hover:opacity-50",
      true: "border-[--color] bg-[--color] text-[--text-color] pressed:opacity-80 hover:opacity-90"
    }
  },
  defaultVariants: {
    variant: "secondary",
    size: "md"
  }
});

export function ToggleButton({ size, variant, ...props }: Readonly<ToggleButtonProps>) {
  return (
    <AriaToggleButton
      {...props}
      className={composeRenderProps(props.className, (className, renderProps) =>
        styles({ ...renderProps, size, variant, className })
      )}
    />
  );
}

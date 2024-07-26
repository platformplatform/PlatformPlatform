/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/index.html?path=/docs/button--docs
 * ref: https://ui.shadcn.com/docs/components/button
 */
import { Button as AriaButton, type ButtonProps as AriaButtonProps, composeRenderProps } from "react-aria-components";
import { tv } from "tailwind-variants";
import { focusRing } from "./focusRing";

export interface ButtonProps extends AriaButtonProps, React.RefAttributes<HTMLButtonElement> {
  /**
   * The variant of the button.
   * @default primary
   */
  variant?: "primary" | "secondary" | "destructive" | "outline" | "ghost" | "link" | "icon";
  /**
   * The size of the button.
   * @default md
   */
  size?: "xs" | "sm" | "md" | "lg" | "icon";
}

const button = tv({
  extend: focusRing,
  base: "inline-flex gap-2 w-fit items-center justify-center w-fit whitespace-nowrap rounded-md text-sm font-medium ring-offset-background transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
  variants: {
    variant: {
      primary: "bg-primary text-primary-foreground hover:bg-primary/90 pressed:bg-primary/80",
      secondary: "bg-secondary text-secondary-foreground hover:bg-secondary/80 pressed:bg-secondary/70",
      destructive: "bg-destructive text-destructive-foreground hover:bg-destructive/90 pressed:bg-destructive/80",
      outline:
        "border border-input text-accent-foreground hover:bg-accent hover:text-accent-foreground/90 pressed:bg-accent/80",
      ghost: "hover:bg-accent text-accent-foreground hover:text-accent-foreground/90 pressed:bg-accent/80",
      link: "text-primary underline-offset-4 hover:underline pressed:text-primary/80"
    },
    size: {
      xs: "h-6 w-6 shrink-0",
      sm: "h-9 rounded-md px-3",
      md: "h-10 px-4 py-2",
      lg: "h-11 rounded-md px-8",
      icon: "h-10 w-10 shrink-0"
    },
    isDisabled: {
      true: "pointer-events-none opacity-50"
    }
  },
  defaultVariants: {
    variant: "primary",
    size: "md"
  }
});

export function Button({ className, variant, size, ...props }: Readonly<ButtonProps>) {
  return (
    <AriaButton
      {...props}
      className={composeRenderProps(className, (className, renderProps) =>
        button({
          ...renderProps,
          size: variant === "icon" ? "icon" : size,
          variant: variant === "icon" ? "ghost" : variant,
          className
        })
      )}
    />
  );
}

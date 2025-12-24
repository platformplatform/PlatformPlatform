import type { VariantProps } from "class-variance-authority";
import { Button as AriaButton, type ButtonProps as AriaButtonProps } from "react-aria-components";
import { cn } from "../utils";
import { buttonVariants } from "./Button";

type MenuButtonProps = AriaButtonProps & VariantProps<typeof buttonVariants>;

export function MenuButton({ className, variant = "default", size = "default", ...props }: MenuButtonProps) {
  return <AriaButton className={cn(buttonVariants({ variant, size, className }))} {...props} />;
}

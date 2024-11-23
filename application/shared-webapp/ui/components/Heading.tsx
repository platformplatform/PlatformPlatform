/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/alertdialog--docs
 * ref: https://ui.shadcn.com/docs/components/alert-dialog
 */
import { Heading as AriaHeading } from "react-aria-components";
import type { HeadingProps } from "react-aria-components";
import { tv } from "tailwind-variants";

interface ExtendedHeadingProps extends HeadingProps {
  size?: "md" | "lg";
}

const headingStyles = tv({
  base: "my-0 font-semibold leading-6",
  variants: {
    size: {
      md: "text-lg",
      lg: "text-xl"
    }
  },
  defaultVariants: {
    size: "lg"
  }
});

export function Heading({ className, slot = "title", size, ...props }: Readonly<ExtendedHeadingProps>) {
  return <AriaHeading {...props} slot={slot} className={headingStyles({ size, className })} />;
}

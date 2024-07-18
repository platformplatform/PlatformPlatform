/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/textfield--docs
 * ref: https://ui.shadcn.com/docs/components/label
 */
import { Label as AriaLabel, type LabelProps } from "react-aria-components";
import { tv } from "tailwind-variants";

const labelStyles = tv({
  base: "text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70 mb-2"
});

export function Label({ className, ...props }: Readonly<LabelProps>) {
  return <AriaLabel {...props} className={labelStyles({ className })} />;
}

/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/alertdialog--docs
 */
import { Dialog as AriaDialog, type DialogProps } from "react-aria-components";
import { twMerge } from "tailwind-merge";

export { DialogTrigger } from "react-aria-components";

export function Dialog(props: Readonly<DialogProps>) {
  return (
    <AriaDialog
      {...props}
      className={twMerge(
        "relative max-h-[inherit] overflow-auto p-6 outline outline-0 [[data-placement]>&]:p-4",
        props.className
      )}
    />
  );
}

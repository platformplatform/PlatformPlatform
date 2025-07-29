import { Dialog as AriaDialog, type DialogProps } from "react-aria-components";
import { twMerge } from "tailwind-merge";

export { DialogTrigger } from "react-aria-components";

export function Dialog(props: Readonly<DialogProps>) {
  return (
    <AriaDialog
      {...props}
      className={twMerge(
        "relative max-h-full min-h-0 overflow-y-auto overscroll-contain p-4 outline outline-0 sm:p-11 [[data-placement]>&]:p-4",
        props.className
      )}
    />
  );
}

import { Dialog as AriaDialog, type DialogProps } from "react-aria-components";
import { twMerge } from "tailwind-merge";

export { DialogTrigger } from "react-aria-components";

export function Dialog(props: Readonly<DialogProps>) {
  return (
    <AriaDialog
      {...props}
      className={twMerge(
        "relative max-h-full min-h-0 outline outline-0",
        "max-sm:flex max-sm:h-full max-sm:w-full max-sm:max-w-full max-sm:flex-col",
        "sm:overflow-y-auto sm:overscroll-contain",
        "p-6 sm:p-8",
        "[[data-placement]>&]:p-4",
        props.className
      )}
    />
  );
}

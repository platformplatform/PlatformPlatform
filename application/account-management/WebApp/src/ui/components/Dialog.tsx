import type { DialogProps } from "react-aria-components";
import { Dialog as RACDialog } from "react-aria-components";
import { twMerge } from "tailwind-merge";

export function Dialog(props: Readonly<DialogProps>) {
  return <RACDialog {...props} className={twMerge("outline outline-0 p-6 [[data-placement]>&]:p-4 max-h-[inherit] overflow-auto relative", props.className)} />;
}

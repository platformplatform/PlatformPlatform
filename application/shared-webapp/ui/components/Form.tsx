import type { FormProps } from "react-aria-components";
import { Form as RACForm } from "react-aria-components";
import { twMerge } from "tailwind-merge";

export function Form(props: Readonly<FormProps>) {
  return <RACForm {...props} className={twMerge("flex flex-col gap-4", props.className)} />;
}

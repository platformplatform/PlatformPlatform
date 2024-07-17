/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/textfield--docs
 */
import { FieldError as AriaFieldError, type FieldErrorProps } from "react-aria-components";
import { composeTailwindRenderProps } from "./utils";

export function FieldError(props: Readonly<FieldErrorProps>) {
  return (
    <AriaFieldError
      {...props}
      className={composeTailwindRenderProps(props.className, "text-destructive text-sm forced-colors:text-[Mark]")}
    />
  );
}

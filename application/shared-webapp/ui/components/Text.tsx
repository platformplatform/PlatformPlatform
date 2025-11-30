/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter
 */
import type { TextProps } from "react-aria-components";
import { Text as AriaText } from "react-aria-components";
import { twMerge } from "tailwind-merge";

export function Text({ className, ...props }: Readonly<TextProps>) {
  return <AriaText {...props} className={twMerge("block", className)} />;
}

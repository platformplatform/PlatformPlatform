/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter
 */
import type { TextProps } from "react-aria-components";
import { Text as AriaText } from "react-aria-components";

export function Text(props: Readonly<TextProps>) {
  return <AriaText {...props} />;
}

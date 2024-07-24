/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter
 */
import { Text, type TextProps } from "react-aria-components";
import { twMerge } from "tailwind-merge";

export function Description({ className, ...props }: Readonly<TextProps>) {
  return <Text {...props} slot="description" className={twMerge("mt-1 text-muted-foreground text-sm", className)} />;
}

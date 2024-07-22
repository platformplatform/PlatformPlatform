/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/textfield--docs
 */
import type { GroupProps } from "react-aria-components";
import { Group as AriaGroup, composeRenderProps } from "react-aria-components";
import { tv } from "tailwind-variants";
import { focusRing } from "./focusRing";
import type { RefAttributes } from "react";

const styles = tv({
  extend: focusRing,
  base: "flex rounded-md border-2 border-border [&>*:not(:last-child)]:border-r-2 [&>*:not(:last-child)]:rounded-r-none"
});
export function Group({ className, children, ...props }: Readonly<GroupProps & RefAttributes<HTMLDivElement>>) {
  return (
    <AriaGroup
      {...props}
      className={composeRenderProps(className, (className, renderProps) =>
        styles({
          ...renderProps,
          className
        })
      )}
    >
      {children}
    </AriaGroup>
  );
}

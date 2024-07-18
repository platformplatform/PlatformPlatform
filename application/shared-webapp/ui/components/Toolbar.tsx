/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/toolbar--docs
 */
import { Toolbar as AriaToolbar, type ToolbarProps, composeRenderProps } from "react-aria-components";
import { tv } from "tailwind-variants";

export { Group as ToolbarGroup } from "react-aria-components";

const styles = tv({
  base: "flex gap-2",
  variants: {
    orientation: {
      horizontal: "flex-row",
      vertical: "flex-col items-start"
    }
  }
});

export function Toolbar(props: Readonly<ToolbarProps>) {
  return (
    <AriaToolbar
      {...props}
      className={composeRenderProps(props.className, (className, renderProps) => styles({ ...renderProps, className }))}
    />
  );
}

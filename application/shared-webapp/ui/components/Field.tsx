/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/textfield--docs
 */
import { Group, type GroupProps, composeRenderProps } from "react-aria-components";
import { tv } from "tailwind-variants";
import { focusRing } from "./focusRing";

export const fieldBorderStyles = tv({
  variants: {
    isFocusWithin: {
      false: "border-input forced-colors:border-[ButtonBorder]",
      true: "border-border forced-colors:border-[Highlight]"
    },
    isInvalid: {
      true: "border-destructive forced-colors:border-[Mark]"
    },
    isDisabled: {
      true: "opacity-50 cursor-not-allowed forced-colors:border-[GrayText]"
    }
  }
});

export const fieldGroupStyles = tv({
  extend: focusRing,
  base: "group flex items-center h-10 bg-background forced-colors:bg-[Field] border-2 rounded-md overflow-hidden",
  variants: fieldBorderStyles.variants
});

export function FieldGroup(props: Readonly<GroupProps>) {
  return (
    <Group
      {...props}
      className={composeRenderProps(props.className, (className, renderProps) =>
        fieldGroupStyles({ ...renderProps, className })
      )}
    />
  );
}

import type React from "react";
/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/textfield--docs
 * ref: https://ui.shadcn.com/docs/components/input
 */
import type { InputProps as AriaInputProps } from "react-aria-components";
import { Input as AriaInput, composeRenderProps } from "react-aria-components";
import { tv } from "tailwind-variants";
import { focusRing } from "./focusRing";

export interface InputProps extends Omit<AriaInputProps, "disabled">, React.RefAttributes<HTMLInputElement> {
  isDisabled?: boolean;
  isReadOnly?: boolean;
  isEmbedded?: boolean;
  startIcon?: React.ReactNode;
}

const inputStyles = tv({
  extend: focusRing,
  base: "h-10 w-full shrink-0 rounded-md border bg-input-background px-2 py-1.5 text-foreground text-sm placeholder:text-muted-foreground",
  variants: {
    isInvalid: {
      true: "border-destructive"
    },
    isDisabled: {
      true: "cursor-not-allowed opacity-50"
    },
    isReadOnly: {
      true: "cursor-default opacity-50"
    },
    isFile: {
      true: "cursor-pointer file:cursor-pointer file:border-0 file:bg-transparent file:font-medium file:text-sm"
    },
    isEmbedded: {
      true: "border-0 border-muted bg-transparent"
    },
    hasStartIcon: {
      true: "pl-9"
    }
  }
});

export function Input({
  className,
  type,
  isEmbedded,
  isDisabled,
  isReadOnly,
  startIcon,
  ...props
}: Readonly<InputProps>) {
  const inputElement = (
    <AriaInput
      {...props}
      disabled={isDisabled}
      readOnly={isReadOnly}
      type={type}
      className={composeRenderProps(
        className,
        (className, { isFocusVisible, isDisabled, ...renderProps }) =>
          `${inputStyles({
            ...renderProps,
            isFile: type === "file",
            isFocusVisible: isEmbedded ? false : isFocusVisible,
            isEmbedded,
            isDisabled,
            isReadOnly,
            hasStartIcon: !!startIcon,
            className
          })} ${isDisabled || isReadOnly ? "border-input" : ""}`
      )}
    />
  );

  if (startIcon) {
    return (
      <div className="relative">
        <div className="-translate-y-1/2 pointer-events-none absolute top-1/2 left-3 flex items-center">
          {startIcon}
        </div>
        {inputElement}
      </div>
    );
  }

  return inputElement;
}

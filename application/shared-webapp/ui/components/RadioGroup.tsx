/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/radiogroup--docs
 * ref: https://ui.shadcn.com/docs/components/radio-group
 */
import type { ReactNode } from "react";
import { Radio as AriaRadio, RadioGroup as AriaRadioGroup, composeRenderProps } from "react-aria-components";
import type { RadioGroupProps as AriaRadioGroupProps, RadioProps, ValidationResult } from "react-aria-components";
import { tv } from "tailwind-variants";
import { Description } from "./Description";
import { FieldError } from "./FieldError";
import { Label } from "./Label";
import { focusRing } from "./focusRing";
import { composeTailwindRenderProps } from "./utils";

export interface RadioGroupProps extends Omit<AriaRadioGroupProps, "children"> {
  label?: string;
  children?: ReactNode;
  description?: string;
  errorMessage?: string | ((validation: ValidationResult) => string);
}

export function RadioGroup(props: Readonly<RadioGroupProps>) {
  return (
    <AriaRadioGroup {...props} className={composeTailwindRenderProps(props.className, "group flex flex-col gap-2")}>
      <Label>{props.label}</Label>
      <div className="flex gap-2 group-orientation-vertical:flex-col group-orientation-horizontal:gap-4">
        {props.children}
      </div>
      {props.description && <Description>{props.description}</Description>}
      <FieldError>{props.errorMessage}</FieldError>
    </AriaRadioGroup>
  );
}

const indicatorStyles = tv({
  extend: focusRing,
  base: "flex w-5 h-5 items-center justify-center rounded-full border-2 bg-accent/50 transition-all",
  variants: {
    isSelected: {
      false: "bg-background border-[--color] [--color:theme(colors.foreground)] group-pressed:opacity-90",
      true: "border-[7px] border-[--color] [--color:theme(colors.primary.DEFAULT)] group-pressed:group-pressed:opacity-90"
    },
    isInvalid: {
      true: "text-destructive-foreground [--color:theme(colors.destructive.DEFAULT)] group-pressed:group-pressed:opacity-90"
    },
    isDisabled: {
      true: "opacity-50 cursor-not-allowed"
    }
  }
});

const radioStyles = tv({
  base: "group flex items-center gap-2 text-foreground text-sm transition forced-colors:disabled:text-[GrayText]",
  variants: {
    isDisabled: {
      true: "opacity-50"
    }
  }
});

export function Radio({ className, children, ...props }: Readonly<RadioProps>) {
  return (
    <AriaRadio
      {...props}
      className={composeRenderProps(className, (className, renderProps) =>
        radioStyles({
          ...renderProps,
          className
        })
      )}
    >
      {(renderProps) => (
        <>
          <div className={indicatorStyles(renderProps)} />
          {children}
        </>
      )}
    </AriaRadio>
  );
}

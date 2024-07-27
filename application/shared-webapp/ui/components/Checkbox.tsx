/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/index.html?path=/docs/checkbox--docs
 * ref: https://ui.shadcn.com/docs/components/checkbox
 */
import { Check, Minus } from "lucide-react";
import type { ReactNode } from "react";
import {
  Checkbox as AriaCheckbox,
  CheckboxGroup as AriaCheckboxGroup,
  type CheckboxGroupProps as AriaCheckboxGroupProps,
  type CheckboxProps,
  type ValidationResult,
  composeRenderProps
} from "react-aria-components";
import { tv } from "tailwind-variants";
import { Description } from "./Description";
import { FieldError } from "./FieldError";
import { Label } from "./Label";
import { focusRing } from "./focusRing";
import { composeTailwindRenderProps } from "./utils";

export interface CheckboxGroupProps extends Omit<AriaCheckboxGroupProps, "children"> {
  label?: string;
  children?: ReactNode;
  description?: string;
  errorMessage?: string | ((validation: ValidationResult) => string);
}

export function CheckboxGroup(props: Readonly<CheckboxGroupProps>) {
  return (
    <AriaCheckboxGroup {...props} className={composeTailwindRenderProps(props.className, "flex flex-col gap-2")}>
      <Label>{props.label}</Label>
      {props.children}
      {props.description && <Description>{props.description}</Description>}
      <FieldError>{props.errorMessage}</FieldError>
    </AriaCheckboxGroup>
  );
}

const checkboxStyles = tv({
  base: "flex h-5 w-5 gap-2 items-center group text-sm transition",
  variants: {
    isDisabled: {
      false: "text-foreground",
      true: "text-foreground/50"
    }
  }
});

const boxStyles = tv({
  extend: focusRing,
  base: "w-full h-full flex-shrink-0 rounded flex items-center justify-center border transition",
  variants: {
    isSelected: {
      false: "bg-background border-[--color] [--color:theme(colors.foreground)] group-pressed:opacity-90",
      true: "text-primary-foreground bg-[--color] border-[--color] [--color:theme(colors.primary.DEFAULT)] group-pressed:group-pressed:opacity-90"
    },
    isInvalid: {
      true: "text-destructive-foreground [--color:theme(colors.destructive.DEFAULT)] group-pressed:group-pressed:opacity-90"
    },
    isDisabled: {
      true: "opacity-50 cursor-not-allowed"
    }
  }
});

const iconStyles = "w-4 h-4";

export function Checkbox({ className, children, ...props }: Readonly<CheckboxProps>) {
  return (
    <AriaCheckbox
      {...props}
      className={composeRenderProps(className, (className, renderProps) =>
        checkboxStyles({ ...renderProps, className })
      )}
    >
      {({ isSelected, isIndeterminate, ...renderProps }) => (
        <>
          <div className={boxStyles({ isSelected: isSelected || isIndeterminate, ...renderProps })}>
            <SelectionIcon isIndeterminate={isIndeterminate} isSelected={isSelected} />
          </div>
          {children}
        </>
      )}
    </AriaCheckbox>
  );
}

type SelectionIconProps = {
  isSelected: boolean;
  isIndeterminate: boolean;
};

function SelectionIcon({ isSelected, isIndeterminate }: Readonly<SelectionIconProps>) {
  if (isIndeterminate) return <Minus aria-hidden className={iconStyles} />;
  if (isSelected) return <Check aria-hidden className={iconStyles} />;

  return null;
}

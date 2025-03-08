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
  base: "group flex h-5 w-5 items-center gap-2 text-sm transition",
  variants: {
    isDisabled: {
      false: "text-foreground",
      true: "text-foreground/50"
    }
  }
});

const boxStyles = tv({
  extend: focusRing,
  base: "flex h-full w-full flex-shrink-0 items-center justify-center rounded border transition",
  variants: {
    isSelected: {
      false: "border-[--color] bg-background [--color:theme(colors.foreground)] group-pressed:opacity-90",
      true: "border-[--color] bg-[--color] text-primary-foreground [--color:theme(colors.primary.DEFAULT)] group-pressed:group-pressed:opacity-90"
    },
    isInvalid: {
      true: "text-destructive-foreground [--color:theme(colors.destructive.DEFAULT)] group-pressed:group-pressed:opacity-90"
    },
    isDisabled: {
      true: "cursor-not-allowed opacity-50"
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
        // @ts-ignore - TypeScript 5.7.2 doesn't recognize that render prop children can return ReactNode[]
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
  if (isIndeterminate) {
    return <Minus aria-hidden={true} className={iconStyles} />;
  }
  if (isSelected) {
    return <Check aria-hidden={true} className={iconStyles} />;
  }

  return null;
}

/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/numberfield--docs
 * ref: https://ui.shadcn.com/docs/components/input
 */
import { ChevronDown, ChevronUp } from "lucide-react";
import {
  Input as AriaInput,
  NumberField as AriaNumberField,
  type NumberFieldProps as AriaNumberFieldProps,
  Button,
  type ButtonProps,
  type ValidationResult
} from "react-aria-components";
import { tv } from "tailwind-variants";
import { Description } from "./Description";
import { FieldGroup, fieldBorderStyles } from "./Field";
import { FieldError } from "./FieldError";
import { Label } from "./Label";
import { composeTailwindRenderProps } from "./utils";

export interface NumberFieldProps extends AriaNumberFieldProps {
  label?: string;
  description?: string;
  errorMessage?: string | ((validation: ValidationResult) => string);
  tooltip?: string;
}

export function NumberField({ label, description, errorMessage, tooltip, ...props }: Readonly<NumberFieldProps>) {
  return (
    <AriaNumberField {...props} className={composeTailwindRenderProps(props.className, "group flex flex-col gap-1")}>
      {label && <Label tooltip={tooltip}>{label}</Label>}
      <FieldGroup>
        {(renderProps) => (
          <>
            <AriaInput className="h-9 w-full min-w-0 border-0 bg-transparent px-2.5 py-1 text-base shadow-none outline-none placeholder:text-muted-foreground focus:ring-0 md:text-sm" />
            <div className={fieldBorderStyles({ ...renderProps, class: "-me-px flex flex-col border-s-2" })}>
              <StepperButton slot="increment" className="px-1.5">
                <ChevronUp aria-hidden={true} className="h-4 w-4" />
              </StepperButton>
              <div className={fieldBorderStyles({ ...renderProps, class: "border-b-2" })} />
              <StepperButton slot="decrement" className="px-1.5">
                <ChevronDown aria-hidden={true} className="h-4 w-4" />
              </StepperButton>
            </div>
          </>
        )}
      </FieldGroup>
      {description && <Description>{description}</Description>}
      <FieldError>{errorMessage}</FieldError>
    </AriaNumberField>
  );
}

const stepperButtonStyles = tv({
  base: "cursor-default text-accent-foreground/90",
  variants: {
    isDisabled: {
      true: "cursor-not-allowed text-accent-foreground/35 forced-colors:text-[GrayText]"
    },
    isHovered: {
      true: "bg-accent text-accent-foreground"
    },
    isPressed: {
      true: "bg-accent/80 text-accent-foreground/50"
    },
    isFocused: {
      true: "ring-accent ring-opacity-50"
    }
  }
});

function StepperButton({ className, ...props }: Readonly<ButtonProps>) {
  return <Button {...props} className={stepperButtonStyles} />;
}

/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/numberfield--docs
 * ref: https://ui.shadcn.com/docs/components/input
 */
import { ChevronDown, ChevronUp } from "lucide-react";
import {
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
import { Input } from "./Input";
import { Label } from "./Label";
import { composeTailwindRenderProps } from "./utils";

export interface NumberFieldProps extends AriaNumberFieldProps {
  label?: string;
  description?: string;
  errorMessage?: string | ((validation: ValidationResult) => string);
}

export function NumberField({ label, description, errorMessage, ...props }: Readonly<NumberFieldProps>) {
  return (
    <AriaNumberField {...props} className={composeTailwindRenderProps(props.className, "group flex flex-col gap-1")}>
      <Label>{label}</Label>
      <FieldGroup>
        {(renderProps) => (
          <>
            <Input isEmbedded />
            <div className={fieldBorderStyles({ ...renderProps, class: "flex flex-col border-s-2" })}>
              <StepperButton slot="increment">
                <ChevronUp aria-hidden className="h-4 w-4" />
              </StepperButton>
              <div className={fieldBorderStyles({ ...renderProps, class: "border-b-2" })} />
              <StepperButton slot="decrement">
                <ChevronDown aria-hidden className="h-4 w-4" />
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
  base: "cursor-default text-accent-foreground/90 px-0.5",
  variants: {
    isDisabled: {
      true: "text-muted cursor-not-allowed group-disabled:text-muted forced-colors:group-disabled:text-[GrayText]"
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

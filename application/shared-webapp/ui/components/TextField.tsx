import type { RefAttributes } from "react";
/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/textfield--docs
 */
import {
  Input as AriaInput,
  TextField as AriaTextField,
  type TextFieldProps as AriaTextFieldProps,
  composeRenderProps,
  type ValidationResult
} from "react-aria-components";
import { cn } from "../utils";
import { Description } from "./Description";
import { FieldError } from "./FieldError";
import { Label } from "./Label";
import { composeTailwindRenderProps } from "./utils";

const inputStyles =
  "h-9 w-full min-w-0 rounded-md border border-input bg-transparent px-2.5 py-1 text-base shadow-xs outline-none transition-[color,box-shadow] file:inline-flex file:h-7 file:border-0 file:bg-transparent file:font-medium file:text-foreground file:text-sm placeholder:text-muted-foreground focus:border-ring focus:ring-[3px] focus:ring-ring/50 disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 invalid:border-destructive invalid:ring-[3px] invalid:ring-destructive/20 md:text-sm dark:bg-input/30 dark:invalid:border-destructive/50 dark:invalid:ring-destructive/40";

export interface TextFieldProps
  extends AriaTextFieldProps,
    Partial<Pick<HTMLInputElement, "autocomplete" | "placeholder">>,
    RefAttributes<HTMLInputElement> {
  label?: string;
  description?: string;
  errorMessage?: string | ((validation: ValidationResult) => string);
  tooltip?: string;
  isDisabled?: boolean;
  isReadOnly?: boolean;
  inputClassName?: string;
  startIcon?: React.ReactNode;
}

export function TextField({
  label,
  description,
  errorMessage,
  tooltip,
  className,
  isDisabled,
  isReadOnly,
  inputClassName,
  startIcon,
  ...props
}: Readonly<TextFieldProps>) {
  if (props.children) {
    return <AriaTextField {...props} className={composeTailwindRenderProps(className, "flex flex-col gap-1")} />;
  }

  const inputElement = (
    <AriaInput
      className={composeRenderProps(cn(inputStyles, startIcon && "pl-9", inputClassName), (className, renderProps) =>
        cn(className, renderProps.isInvalid && "border-destructive ring-[3px] ring-destructive/20")
      )}
    />
  );

  return (
    <AriaTextField
      {...props}
      isDisabled={isDisabled}
      isReadOnly={isReadOnly}
      className={composeTailwindRenderProps(className, "flex flex-col gap-1")}
    >
      {label && <Label tooltip={tooltip}>{label}</Label>}
      {startIcon ? (
        <div className="relative">
          <div className="pointer-events-none absolute top-1/2 left-3 flex -translate-y-1/2 items-center">
            {startIcon}
          </div>
          {inputElement}
        </div>
      ) : (
        inputElement
      )}
      {description && <Description>{description}</Description>}
      <FieldError>{errorMessage}</FieldError>
    </AriaTextField>
  );
}

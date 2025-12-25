import { createContext, type FormEvent, type FormHTMLAttributes } from "react";
import { cn } from "../utils";

export type ValidationErrors = Record<string, string | string[]>;

export const FormValidationContext = createContext<ValidationErrors>({});

export interface FormProps extends FormHTMLAttributes<HTMLFormElement> {
  validationErrors?: ValidationErrors;
  validationBehavior?: "aria" | "native";
}

export function Form({
  className,
  children,
  validationErrors,
  validationBehavior,
  onSubmit,
  ...props
}: Readonly<FormProps>) {
  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    if (validationBehavior === "aria") {
      event.preventDefault();
    }
    onSubmit?.(event);
  };

  return (
    <FormValidationContext.Provider value={validationErrors ?? {}}>
      <form
        {...props}
        className={cn("flex flex-col gap-4", className)}
        onSubmit={handleSubmit}
        noValidate={validationBehavior === "aria"}
      >
        {children}
      </form>
    </FormValidationContext.Provider>
  );
}

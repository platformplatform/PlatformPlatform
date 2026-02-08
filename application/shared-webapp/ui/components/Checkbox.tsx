import { Checkbox as CheckboxPrimitive } from "@base-ui/react/checkbox";
import { CheckIcon } from "lucide-react";
import { cn } from "../utils";

function Checkbox({ className, ...props }: CheckboxPrimitive.Root.Props) {
  return (
    <CheckboxPrimitive.Root
      data-slot="checkbox"
      className={cn(
        // NOTE: This diverges from stock ShadCN to increase size to 20px, use outline-based focus ring, add 44px tap target via after pseudo-element,
        // and active:border-primary for press feedback.
        "peer relative flex size-5 shrink-0 cursor-pointer items-center justify-center rounded-[0.3125rem] border border-input shadow-xs outline-ring transition-shadow after:absolute after:-inset-3 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 active:border-primary disabled:cursor-not-allowed disabled:opacity-50 group-has-disabled/field:opacity-50 aria-invalid:border-destructive aria-invalid:ring-[0.1875rem] aria-invalid:ring-destructive/20 aria-invalid:aria-checked:border-primary data-checked:border-primary data-checked:bg-primary data-checked:text-primary-foreground dark:bg-input/30 dark:data-checked:bg-primary dark:aria-invalid:border-destructive/50 dark:aria-invalid:ring-destructive/40",
        className
      )}
      {...props}
    >
      <CheckboxPrimitive.Indicator
        data-slot="checkbox-indicator"
        className="grid place-content-center text-current transition-none [&>svg]:size-4"
      >
        <CheckIcon />
      </CheckboxPrimitive.Indicator>
    </CheckboxPrimitive.Root>
  );
}

export { Checkbox };

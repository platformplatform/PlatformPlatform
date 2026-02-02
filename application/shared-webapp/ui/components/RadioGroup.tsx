import { Radio as RadioPrimitive } from "@base-ui/react/radio";
import { RadioGroup as RadioGroupPrimitive } from "@base-ui/react/radio-group";
import { CircleIcon } from "lucide-react";
import { cn } from "../utils";

function RadioGroup({ className, ...props }: RadioGroupPrimitive.Props) {
  return <RadioGroupPrimitive data-slot="radio-group" className={cn("grid w-full gap-3", className)} {...props} />;
}

function RadioGroupItem({ className, ...props }: RadioPrimitive.Root.Props) {
  return (
    <RadioPrimitive.Root
      data-slot="radio-group-item"
      // NOTE: This diverges from stock ShadCN to use outline-based focus ring instead of ring utilities,
      // and active:border-primary for press feedback.
      className={cn(
        "group/radio-group-item peer relative flex aspect-square size-4 shrink-0 cursor-pointer rounded-full border border-input text-primary shadow-xs outline-ring after:absolute after:-inset-x-3 after:-inset-y-2 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 active:border-primary disabled:cursor-not-allowed disabled:opacity-50 aria-invalid:border-destructive aria-invalid:outline-destructive dark:bg-input/30 dark:aria-invalid:border-destructive/50",
        className
      )}
      {...props}
    >
      <RadioPrimitive.Indicator
        data-slot="radio-group-indicator"
        className="flex size-4 items-center justify-center text-primary group-aria-invalid/radio-group-item:text-destructive"
      >
        <CircleIcon className="absolute top-1/2 left-1/2 size-2 -translate-x-1/2 -translate-y-1/2 fill-current" />
      </RadioPrimitive.Indicator>
    </RadioPrimitive.Root>
  );
}

export { RadioGroup, RadioGroupItem };

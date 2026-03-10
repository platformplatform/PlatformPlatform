import { Switch as SwitchPrimitive } from "@base-ui/react/switch";

import { cn } from "../utils";

function Switch({ className, ...props }: SwitchPrimitive.Root.Props) {
  return (
    <SwitchPrimitive.Root
      data-slot="switch"
      className={cn(
        // NOTE: This diverges from stock ShadCN to use outline-based focus ring, add 44px tap target via after pseudo-element,
        // and active:bg-* for press feedback. Uses @base-ui/react/switch which provides role="switch" and aria-checked natively.
        "peer relative inline-flex h-5 w-9 shrink-0 cursor-pointer items-center rounded-full border-2 border-transparent shadow-xs outline-ring transition-colors group-has-disabled/field:opacity-50 after:absolute after:-inset-3 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 disabled:cursor-not-allowed disabled:opacity-50 data-checked:bg-primary data-checked:active:bg-primary/80 data-unchecked:bg-input data-unchecked:active:bg-input/80 dark:data-unchecked:bg-input/80",
        className
      )}
      {...props}
    >
      <SwitchPrimitive.Thumb
        data-slot="switch-thumb"
        className="pointer-events-none block size-4 rounded-full bg-background shadow-lg ring-0 transition-transform data-checked:translate-x-4 data-unchecked:translate-x-0"
      />
    </SwitchPrimitive.Root>
  );
}

export { Switch };

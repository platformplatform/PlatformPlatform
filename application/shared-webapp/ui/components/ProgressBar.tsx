import { Progress as ProgressPrimitive } from "@base-ui/react/progress";

import { cn } from "../utils";
import { Label } from "./Label";

export interface ProgressBarProps extends ProgressPrimitive.Root.Props {
  label?: string;
}

export function ProgressBar({ label, className, value, ...props }: Readonly<ProgressBarProps>) {
  const isIndeterminate = value === null || value === undefined;

  return (
    <ProgressPrimitive.Root
      data-slot="progress"
      value={value}
      className={cn("flex flex-col gap-1", className)}
      {...props}
    >
      <div className="flex justify-between gap-2">
        {label && <Label>{label}</Label>}
        <ProgressPrimitive.Value className="text-muted-foreground text-sm" />
      </div>
      <ProgressPrimitive.Track className="relative h-2 w-64 overflow-hidden rounded-full bg-muted outline outline-1 outline-transparent -outline-offset-1">
        <ProgressPrimitive.Indicator
          className={cn(
            "h-full rounded-full bg-info forced-colors:bg-[Highlight]",
            isIndeterminate &&
              "slide-out-to-right-full animate-in [--tw-enter-translate-x:calc(-16rem-100%)] [animation-duration:1s] [animation-iteration-count:infinite] [animation-timing-function:ease-out]"
          )}
        />
      </ProgressPrimitive.Track>
    </ProgressPrimitive.Root>
  );
}

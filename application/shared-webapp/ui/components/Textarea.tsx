import type * as React from "react";

import { cn } from "../utils";

function Textarea({ className, ...props }: React.ComponentProps<"textarea">) {
  return (
    <textarea
      data-slot="textarea"
      className={cn(
        "min-h-16 w-full min-w-0 resize-y rounded-md border border-input bg-white px-2.5 pt-3 pb-2.5 text-sm shadow-xs outline-ring transition-[color,box-shadow] [field-sizing:content] placeholder:text-muted-foreground focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 aria-invalid:outline aria-invalid:outline-2 aria-invalid:outline-destructive aria-invalid:outline-offset-2 dark:bg-input/30",
        className
      )}
      {...props}
    />
  );
}

export { Textarea };

import { t } from "@lingui/core/macro";
import { InfoIcon } from "lucide-react";
import type * as React from "react";
import { cn } from "../utils";
import { Label } from "./Label";
import { Tooltip, TooltipContent, TooltipTrigger } from "./Tooltip";

export interface LabelWithTooltipProps extends React.ComponentProps<"label"> {
  tooltip?: string;
}

function LabelWithTooltip({ tooltip, children, className, ...props }: Readonly<LabelWithTooltipProps>) {
  if (!tooltip) {
    return (
      <Label {...props} className={className}>
        {children}
      </Label>
    );
  }

  return (
    <Label {...props} className={cn("flex items-center gap-2", className)}>
      {children}
      <Tooltip>
        <TooltipTrigger
          aria-label={t`More information`}
          className="inline-flex size-4 shrink-0 items-center justify-center rounded-full p-0 outline-ring hover:bg-muted focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-1"
        >
          <InfoIcon className="size-4 text-muted-foreground" />
        </TooltipTrigger>
        <TooltipContent>{tooltip}</TooltipContent>
      </Tooltip>
    </Label>
  );
}

export { LabelWithTooltip };

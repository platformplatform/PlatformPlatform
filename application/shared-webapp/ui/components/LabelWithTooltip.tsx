import { t } from "@lingui/core/macro";
import { InfoIcon } from "lucide-react";
import { Button as AriaButton, Label as AriaLabel, type LabelProps as AriaLabelProps } from "react-aria-components";
import { cn } from "../utils";
import { Tooltip, TooltipTrigger } from "./Tooltip";

export interface LabelWithTooltipProps extends AriaLabelProps {
  tooltip?: string;
}

const labelClassName =
  "flex select-none items-center gap-2 font-medium text-sm leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-50 group-data-[disabled=true]:pointer-events-none group-data-[disabled=true]:opacity-50";

function LabelWithTooltip({ tooltip, children, className, ...props }: Readonly<LabelWithTooltipProps>) {
  if (!tooltip) {
    return (
      <AriaLabel {...props} className={cn(labelClassName, className)}>
        {children}
      </AriaLabel>
    );
  }

  return (
    <AriaLabel {...props} className={cn(labelClassName, className)}>
      {children}
      <TooltipTrigger delay={300}>
        <AriaButton
          aria-label={t`More information`}
          className="inline-flex h-4 w-4 shrink-0 items-center justify-center rounded-md p-0 outline-none hover:bg-muted focus-visible:ring-2 focus-visible:ring-ring"
        >
          <InfoIcon className="h-4 w-4 text-muted-foreground" />
        </AriaButton>
        <Tooltip>{tooltip}</Tooltip>
      </TooltipTrigger>
    </AriaLabel>
  );
}

export { LabelWithTooltip };

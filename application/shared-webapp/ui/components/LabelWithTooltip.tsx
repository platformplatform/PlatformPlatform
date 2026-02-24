import { t } from "@lingui/core/macro";
import { InfoIcon } from "lucide-react";
import type * as React from "react";
import { Tooltip, TooltipContent, TooltipTrigger } from "./Tooltip";

export interface LabelWithTooltipProps {
  tooltip?: string;
  children: React.ReactNode;
}

function LabelWithTooltip({ tooltip, children }: Readonly<LabelWithTooltipProps>) {
  if (!tooltip) {
    return children;
  }

  return (
    <>
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
    </>
  );
}

export { LabelWithTooltip };

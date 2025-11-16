/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/textfield--docs
 * ref: https://ui.shadcn.com/docs/components/label
 */

import { InfoIcon } from "lucide-react";
import { Label as AriaLabel, type LabelProps as AriaLabelProps } from "react-aria-components";
import { tv } from "tailwind-variants";
import { Button } from "./Button";
import { Tooltip, TooltipTrigger } from "./Tooltip";

const labelStyles = tv({
  base: "mb-2 text-sm leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
});

export interface LabelProps extends AriaLabelProps {
  tooltip?: string;
}

export function Label({ className, tooltip, children, ...props }: Readonly<LabelProps>) {
  if (!tooltip) {
    return (
      <AriaLabel {...props} className={labelStyles({ className })}>
        {children}
      </AriaLabel>
    );
  }

  return (
    <AriaLabel {...props} className={labelStyles({ className })}>
      <span className="inline-flex items-center gap-2">
        {children}
        <TooltipTrigger delay={300}>
          <Button variant="icon" className="h-4 w-4 p-0">
            <InfoIcon className="h-4 w-4 text-muted-foreground" />
          </Button>
          <Tooltip>{tooltip}</Tooltip>
        </TooltipTrigger>
      </span>
    </AriaLabel>
  );
}

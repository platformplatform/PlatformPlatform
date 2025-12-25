import type { HTMLAttributes } from "react";
import { cn } from "../utils";

export interface DescriptionProps extends HTMLAttributes<HTMLSpanElement> {
  className?: string;
}

export function Description({ className, ...props }: Readonly<DescriptionProps>) {
  return <span {...props} className={cn("mt-1 block text-muted-foreground text-sm", className)} />;
}

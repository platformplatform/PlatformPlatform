import type * as React from "react";
import { cn } from "../utils";

interface TextProps extends React.HTMLAttributes<HTMLSpanElement> {
  as?: "span" | "p";
}

export function Text({ as: Component = "span", className, children, ...props }: Readonly<TextProps>) {
  return (
    <Component data-slot="text" className={cn("block", className)} {...props}>
      {children}
    </Component>
  );
}

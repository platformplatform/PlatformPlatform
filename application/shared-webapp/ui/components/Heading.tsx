import type * as React from "react";
import { cn } from "../utils";

type HeadingLevel = 1 | 2 | 3 | 4 | 5 | 6;
type HeadingSize = "sm" | "md" | "lg" | "xl" | "2xl";

interface HeadingProps extends React.HTMLAttributes<HTMLHeadingElement> {
  level?: HeadingLevel;
  size?: HeadingSize;
}

const sizeStyles: Record<HeadingSize, string> = {
  sm: "text-sm",
  md: "text-base",
  lg: "text-lg",
  xl: "text-xl",
  "2xl": "text-2xl"
};

export function Heading({ level = 2, size, className, children, ...props }: Readonly<HeadingProps>) {
  const Tag = `h${level}` as const;
  const sizeClass = size ? sizeStyles[size] : undefined;

  return (
    <Tag data-slot="heading" className={cn("my-0 font-semibold leading-6", sizeClass, className)} {...props}>
      {children}
    </Tag>
  );
}

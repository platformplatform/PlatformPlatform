import { cva, type VariantProps } from "class-variance-authority";
import { CircleCheckBigIcon, InfoIcon, type LucideIcon, TriangleAlertIcon } from "lucide-react";
import type React from "react";
import { cn } from "../utils";

const inlineAlertVariants = cva(
  "relative flex w-full flex-col rounded-md border-2 bg-background p-5 text-foreground outline outline-0 outline-ring outline-offset-2 focus-visible:outline-2 forced-colors:outline-[Highlight]",
  {
    variants: {
      variant: {
        neutral: "border-muted",
        info: "border-info",
        warning: "border-warning",
        danger: "border-danger",
        success: "border-success"
      }
    },
    defaultVariants: {
      variant: "neutral"
    }
  }
);

const iconVariants = cva("absolute top-5 right-5 font-semibold text-md", {
  variants: {
    variant: {
      neutral: "stroke-muted",
      info: "stroke-info",
      warning: "stroke-warning",
      danger: "stroke-danger",
      success: "stroke-success"
    }
  },
  defaultVariants: {
    variant: "neutral"
  }
});

type Variant = NonNullable<VariantProps<typeof inlineAlertVariants>["variant"]>;

const alertIcon: Record<Variant, LucideIcon | null> = {
  neutral: null,
  success: CircleCheckBigIcon,
  info: InfoIcon,
  warning: TriangleAlertIcon,
  danger: TriangleAlertIcon
};

type InlineAlertProps = {
  variant?: Variant;
  autoFocus?: boolean;
  className?: string;
  children: React.ReactNode;
};

export function InlineAlert({ variant, autoFocus, className, children }: InlineAlertProps) {
  const Icon = variant != null ? alertIcon[variant] : null;
  return (
    // biome-ignore lint/a11y/noAutofocus: This is a design system component, and the `autoFocus` prop is intentional.
    <div role="alert" className={cn(inlineAlertVariants({ variant }), className)} autoFocus={autoFocus}>
      {Icon && <Icon className={iconVariants({ variant })} />}
      {children}
    </div>
  );
}

type HeadingProps = {
  className?: string;
  children: React.ReactNode;
};

export function Heading({ className, children }: HeadingProps) {
  return <h3 className={cn("pr-8 font-semibold text-md", className)}>{children}</h3>;
}

type ContentProps = {
  className?: string;
  children: React.ReactNode;
};

export function Content({ className, children }: ContentProps) {
  return <section className={cn("mt-4 font-medium text-sm", className)}>{children}</section>;
}

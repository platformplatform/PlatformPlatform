/**
 * ref: https://react-spectrum.adobe.com/react-spectrum/InlineAlert.html
 * ref: https://mui.com/material-ui/react-alert/#severity
 */
import { CircleCheckBigIcon, InfoIcon, type LucideIcon, TriangleAlertIcon } from "lucide-react";
import { tv } from "tailwind-variants";
import { focusRing } from "./focusRing";

const styles = tv({
  extend: focusRing,
  base: "relative flex flex-col w-full border-2 rounded-md p-5 bg-background text-foreground",
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
});

const iconStyles = tv({
  base: "absolute right-5 top-5 text-md font-semibold",
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

const headingStyles = tv({
  base: "text-md font-semibold pr-8"
});

const contentStyles = tv({
  base: "text-sm font-medium mt-4"
});

type Variant = keyof typeof styles.variants.variant;

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
    <div role="alert" className={styles({ variant, className })} autoFocus={autoFocus}>
      {Icon && <Icon className={iconStyles({ variant })} />}
      {children}
    </div>
  );
}

type HeadingProps = {
  className?: string;
  children: React.ReactNode;
};

export function Heading({ className, children }: HeadingProps) {
  return <h3 className={headingStyles({ className })}>{children}</h3>;
}

type ContentProps = {
  className?: string;
  children: React.ReactNode;
};

export function Content({ className, children }: ContentProps) {
  return <section className={contentStyles({ className })}>{children}</section>;
}

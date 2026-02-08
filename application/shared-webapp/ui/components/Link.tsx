import { Link as RouterLink } from "@tanstack/react-router";
import { cva, type VariantProps } from "class-variance-authority";
import type { ComponentProps, MouseEvent, ReactNode } from "react";
import { cn } from "../utils";

// NOTE: Button-styled variants (button-primary, button-secondary, button-destructive) diverge from stock ShadCN Link
// to include active backgrounds for press feedback, matching the Button component.
const linkVariants = cva(
  "inline-flex cursor-pointer items-center justify-center gap-2 whitespace-nowrap rounded-md px-1 py-0.5 font-medium outline-0 transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2",
  {
    variants: {
      variant: {
        primary: "text-primary outline-ring hover:text-primary/90",
        secondary: "text-secondary-foreground outline-ring hover:text-secondary-foreground/90",
        destructive: "text-destructive outline-ring hover:text-destructive/90",
        ghost: "text-accent-foreground outline-ring hover:bg-hover-background hover:text-accent-foreground/90",
        logo: "p-0 outline-ring hover:bg-transparent",
        icon: "size-10 rounded-lg bg-background/50 text-muted-foreground outline-ring hover:bg-background hover:text-foreground",
        button: "outline-ring",
        "button-primary":
          "h-[var(--control-height)] bg-primary px-2.5 text-primary-foreground outline-primary hover:bg-primary/90 active:bg-primary/80",
        "button-secondary":
          "h-[var(--control-height)] border border-border bg-background px-2.5 text-foreground outline-ring hover:bg-hover-background active:bg-accent",
        "button-destructive":
          "h-[var(--control-height)] bg-destructive px-2.5 text-destructive-foreground outline-destructive hover:bg-destructive/90 active:bg-destructive/80"
      },
      underline: {
        true: "underline",
        hover: "no-underline hover:underline",
        false: "no-underline"
      },
      size: {
        md: "text-base",
        sm: "text-sm",
        lg: "text-lg"
      },
      disabled: {
        true: "pointer-events-none opacity-50"
      }
    },
    compoundVariants: [
      { variant: "button", class: "no-underline hover:no-underline" },
      { variant: "button-primary", class: "no-underline hover:no-underline" },
      { variant: "button-secondary", class: "no-underline hover:no-underline" },
      { variant: "button-destructive", class: "no-underline hover:no-underline" }
    ],
    defaultVariants: {
      variant: "primary",
      underline: "hover"
    }
  }
);

type LinkVariantProps = VariantProps<typeof linkVariants>;

interface LinkBaseProps extends LinkVariantProps {
  children?: ReactNode;
  className?: string;
  disabled?: boolean;
  "aria-current"?: "page" | "step" | "location" | "date" | "time" | "true" | "false";
}

interface InternalLinkProps extends LinkBaseProps {
  href: string;
  onClick?: (event: MouseEvent<HTMLAnchorElement>) => void;
}

interface ExternalLinkProps extends LinkBaseProps {
  href: string;
  target?: string;
  rel?: string;
  onClick?: (event: MouseEvent<HTMLAnchorElement>) => void;
}

interface ButtonLinkProps extends LinkBaseProps {
  href?: undefined;
  onClick?: (event: MouseEvent<HTMLButtonElement>) => void;
}

type LinkProps = InternalLinkProps | ExternalLinkProps | ButtonLinkProps;

function isExternalLink(href: string): boolean {
  return href.startsWith("http://") || href.startsWith("https://") || href.startsWith("mailto:");
}

export function Link({
  href,
  children,
  className,
  variant,
  underline,
  size,
  disabled,
  onClick,
  ...props
}: Readonly<LinkProps>) {
  const linkClassName = cn(
    linkVariants({
      variant,
      underline,
      size,
      disabled: disabled ?? false
    }),
    className
  );

  if (disabled) {
    return (
      <span className={linkClassName} aria-disabled="true" {...props}>
        {children}
      </span>
    );
  }

  if (href === undefined) {
    return (
      // biome-ignore lint/a11y/useSemanticElements: Button with role="link" is intentional for click-only links used in navigation menus
      <button
        type="button"
        role="link"
        className={linkClassName}
        onClick={onClick as (event: MouseEvent<HTMLButtonElement>) => void}
        {...props}
      >
        {children}
      </button>
    );
  }

  if (isExternalLink(href)) {
    const { target, rel, "aria-current": ariaCurrent } = props as ExternalLinkProps;
    return (
      <a
        href={href}
        className={linkClassName}
        target={target ?? "_blank"}
        rel={rel ?? "noopener noreferrer"}
        onClick={onClick as (event: MouseEvent<HTMLAnchorElement>) => void}
        aria-current={ariaCurrent}
      >
        {children}
      </a>
    );
  }

  const routerLinkProps = props as Omit<ComponentProps<typeof RouterLink>, "to" | "className">;
  return (
    <RouterLink
      to={href}
      className={linkClassName}
      onClick={onClick as (event: MouseEvent<HTMLAnchorElement>) => void}
      {...routerLinkProps}
    >
      {children}
    </RouterLink>
  );
}

export { linkVariants };

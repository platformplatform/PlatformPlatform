import { Link as RouterLink } from "@tanstack/react-router";
import { cva, type VariantProps } from "class-variance-authority";
import type { ComponentProps, MouseEvent, ReactNode } from "react";
import { cn } from "../utils";

const linkVariants = cva(
  "inline-flex cursor-pointer items-center justify-center gap-2 whitespace-nowrap rounded-md font-medium outline-none transition-colors focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
  {
    variants: {
      variant: {
        primary: "text-primary hover:text-primary/90",
        secondary: "text-secondary-foreground hover:text-secondary-foreground/90",
        destructive: "text-destructive hover:text-destructive/90",
        ghost: "text-accent-foreground hover:bg-hover-background hover:text-accent-foreground/90",
        button: "hover:opacity-90"
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

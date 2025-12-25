/**
 * ref: https://ui.shadcn.com/docs/components/breadcrumb
 */
import { ChevronRight } from "lucide-react";
import type { ReactNode } from "react";
import {
  Breadcrumb as AriaBreadcrumb,
  Breadcrumbs as AriaBreadcrumbs,
  type BreadcrumbProps,
  type BreadcrumbsProps
} from "react-aria-components";
import { twMerge } from "tailwind-merge";
import { Link } from "./Link";
import { composeTailwindRenderProps } from "./utils";

export function Breadcrumbs<T extends object>(props: Readonly<BreadcrumbsProps<T>>) {
  return <AriaBreadcrumbs {...props} className={twMerge("flex gap-2", props.className)} />;
}

interface BreadcrumbItemProps extends BreadcrumbProps {
  href?: string;
  children?: ReactNode;
}

export function Breadcrumb({ href, children, ...props }: Readonly<BreadcrumbItemProps>) {
  return (
    <AriaBreadcrumb {...props} className={composeTailwindRenderProps(props.className, "flex items-center gap-2")}>
      {href !== undefined ? (
        <Link href={href} variant="primary" size="sm">
          {children}
        </Link>
      ) : (
        <span className="text-muted-foreground text-sm">{children}</span>
      )}
      {href !== undefined && <ChevronRight className="h-3 w-3 text-muted-foreground" />}
    </AriaBreadcrumb>
  );
}

/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/index.html?path=/docs/breadcrumbs--docs
 * ref: https://ui.shadcn.com/docs/components/breadcrumb
 */
import { ChevronRight } from "lucide-react";
import {
  Breadcrumb as AriaBreadcrumb,
  Breadcrumbs as AriaBreadcrumbs,
  type BreadcrumbProps,
  type BreadcrumbsProps,
  type LinkProps
} from "react-aria-components";
import { twMerge } from "tailwind-merge";
import { Link } from "./Link";

export function Breadcrumbs<T extends object>(props: Readonly<BreadcrumbsProps<T>>) {
  return <AriaBreadcrumbs {...props} className={twMerge("flex gap-2", props.className)} />;
}

export function Breadcrumb(props: BreadcrumbProps & LinkProps) {
  return (
    <AriaBreadcrumb {...props} className={twMerge("flex items-center gap-2", props.className)}>
      <Link variant="primary" {...props} />
      {props.href && <ChevronRight className="h-3 w-3 text-muted-foreground" />}
    </AriaBreadcrumb>
  );
}

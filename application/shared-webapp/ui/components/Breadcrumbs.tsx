/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/index.html?path=/docs/breadcrumbs--docs
 * ref: https://ui.shadcn.com/docs/components/breadcrumb
 */
import { ChevronRight } from "lucide-react";
import {
  Breadcrumb as AriaBreadcrumb,
  Breadcrumbs as AriaBreadcrumbs,
  Link as AriaLink,
  type BreadcrumbProps,
  type BreadcrumbsProps,
  composeRenderProps,
  type LinkProps
} from "react-aria-components";
import { twMerge } from "tailwind-merge";
import { composeTailwindRenderProps } from "./utils";

const linkStyles =
  "inline-flex cursor-pointer items-center justify-center gap-2 whitespace-nowrap rounded-md font-medium text-primary text-sm transition-colors hover:text-primary/90 no-underline hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 data-[disabled]:pointer-events-none data-[disabled]:opacity-50";

export function Breadcrumbs<T extends object>(props: Readonly<BreadcrumbsProps<T>>) {
  return <AriaBreadcrumbs {...props} className={twMerge("flex gap-2", props.className)} />;
}

export function Breadcrumb(props: BreadcrumbProps & Omit<LinkProps, "className">) {
  return (
    <AriaBreadcrumb {...props} className={composeTailwindRenderProps(props.className, "flex items-center gap-2")}>
      <AriaLink {...props} className={linkStyles}>
        {props.children}
      </AriaLink>
      <AriaLink className={composeRenderProps("", (_, { isCurrent }) => (isCurrent ? "hidden" : ""))}>
        <ChevronRight className="h-3 w-3 text-muted-foreground" />
      </AriaLink>
    </AriaBreadcrumb>
  );
}

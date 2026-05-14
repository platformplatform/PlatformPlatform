import type * as React from "react";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { ChevronLeftIcon, ChevronRightIcon, MoreHorizontalIcon } from "lucide-react";

import { cn } from "../utils";
import { Button } from "./Button";

function Pagination({ className, ...props }: React.ComponentProps<"nav">) {
  return (
    <nav
      aria-label={t`Pagination`}
      data-slot="pagination"
      className={cn("mx-auto flex w-full justify-center", className)}
      {...props}
    />
  );
}

function PaginationContent({ className, ...props }: React.ComponentProps<"ul">) {
  return <ul data-slot="pagination-content" className={cn("flex items-center gap-1", className)} {...props} />;
}

function PaginationItem({ ...props }: React.ComponentProps<"li">) {
  return <li data-slot="pagination-item" {...props} />;
}

type PaginationLinkProps = {
  isActive?: boolean;
} & Pick<React.ComponentProps<typeof Button>, "size"> &
  React.ComponentProps<"a">;

function PaginationLink({ className, isActive, size = "icon", children, ...props }: PaginationLinkProps) {
  return (
    <Button
      variant={isActive ? "outline" : "ghost"}
      size={size}
      className={cn(className)}
      nativeButton={false}
      render={
        <a aria-current={isActive ? "page" : undefined} data-slot="pagination-link" data-active={isActive} {...props}>
          {children}
        </a>
      }
    />
  );
}

function PaginationPrevious({ className, ...props }: React.ComponentProps<typeof PaginationLink>) {
  return (
    <PaginationLink aria-label={t`Go to previous page`} size="default" className={cn("pl-2!", className)} {...props}>
      <ChevronLeftIcon data-icon="inline-start" />
      <span className="hidden sm:block">
        <Trans>Previous</Trans>
      </span>
    </PaginationLink>
  );
}

function PaginationNext({ className, ...props }: React.ComponentProps<typeof PaginationLink>) {
  return (
    <PaginationLink aria-label={t`Go to next page`} size="default" className={cn("pr-2!", className)} {...props}>
      <span className="hidden sm:block">
        <Trans>Next</Trans>
      </span>
      <ChevronRightIcon data-icon="inline-end" />
    </PaginationLink>
  );
}

function PaginationEllipsis({ className, ...props }: React.ComponentProps<"span">) {
  return (
    <span
      aria-hidden={true}
      data-slot="pagination-ellipsis"
      className={cn(
        // `relative` keeps the inner `sr-only` span's `position: absolute` containing block scoped to
        // this ellipsis. Without it the sr-only falls back to the next positioned ancestor (the page's
        // outer <main>) where the auto top calculation lands at the bottom of the document, inflating
        // the page scrollHeight and producing a phantom second window scrollbar.
        "relative flex size-[var(--control-height)] items-center justify-center [&_svg:not([class*='size-'])]:size-4",
        className
      )}
      {...props}
    >
      <MoreHorizontalIcon />
      <span className="sr-only">
        <Trans>More pages</Trans>
      </span>
    </span>
  );
}

export {
  Pagination,
  PaginationContent,
  PaginationEllipsis,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious
};

import { ChevronLeftIcon, ChevronRightIcon } from "lucide-react";
import type { ReactNode } from "react";
import { Button } from "./Button";
import { Pagination, PaginationContent, PaginationEllipsis, PaginationItem, PaginationLink } from "./Pagination";

type WindowWithTracking = {
  __trackInteraction?: (name: string, type: string, action: string, extraProperties?: Record<string, string>) => void;
};

type TablePaginationProps = {
  paginationSize?: number;
  currentPage: number;
  totalPages: number;
  previousLabel?: ReactNode;
  nextLabel?: ReactNode;
  onPageChange: (page: number) => void;
  trackingTitle?: string;
  className?: string;
};

export function TablePagination({
  paginationSize = 7,
  currentPage,
  totalPages,
  onPageChange,
  previousLabel,
  nextLabel,
  trackingTitle,
  className
}: Readonly<TablePaginationProps>) {
  if (paginationSize % 2 === 0 || paginationSize < 5) {
    throw new Error("Pagination size must be an odd number greater than or equal to 5.");
  }

  const pageNumbers = getPageNumbers(currentPage, totalPages, paginationSize);

  const handlePageChange = (page: number, action: string) => {
    if (trackingTitle) {
      (window as unknown as WindowWithTracking).__trackInteraction?.(trackingTitle, "interaction", action);
    }
    onPageChange(page);
  };

  return (
    <Pagination className={className}>
      <PaginationContent>
        <PaginationItem>
          <Button
            variant="ghost"
            size="default"
            onClick={() => handlePageChange(currentPage - 1, "Previous page")}
            disabled={currentPage === 1}
            aria-label={previousLabel ? String(previousLabel) : "Previous page"}
            className="gap-1 pl-2.5"
          >
            <ChevronLeftIcon className="size-4" />
            {previousLabel && <span className="hidden sm:block">{previousLabel}</span>}
          </Button>
        </PaginationItem>

        {pageNumbers.map((page, index) =>
          page === "ellipsis" ? (
            <PaginationItem key={`ellipsis-${index}`}>
              <PaginationEllipsis />
            </PaginationItem>
          ) : (
            <PaginationItem key={page}>
              <PaginationLink
                href="#"
                onClick={(e) => {
                  e.preventDefault();
                  handlePageChange(page, `Page ${page}`);
                }}
                isActive={page === currentPage}
                className="tabular-nums"
              >
                {page}
              </PaginationLink>
            </PaginationItem>
          )
        )}

        <PaginationItem>
          <Button
            variant="ghost"
            size="default"
            onClick={() => handlePageChange(currentPage + 1, "Next page")}
            disabled={currentPage === totalPages}
            aria-label={nextLabel ? String(nextLabel) : "Next page"}
            className="gap-1 pr-2.5"
          >
            {nextLabel && <span className="hidden sm:block">{nextLabel}</span>}
            <ChevronRightIcon className="size-4" />
          </Button>
        </PaginationItem>
      </PaginationContent>
    </Pagination>
  );
}

function getPageNumbers(currentPage: number, totalPages: number, paginationSize: number): (number | "ellipsis")[] {
  if (totalPages <= paginationSize) {
    return Array.from({ length: totalPages }, (_, i) => i + 1);
  }

  const sideCount = Math.floor((paginationSize - 3) / 2);
  const pages: (number | "ellipsis")[] = [];

  if (currentPage <= sideCount + 2) {
    for (let i = 1; i <= paginationSize - 2; i++) {
      pages.push(i);
    }
    pages.push("ellipsis");
    pages.push(totalPages);
  } else if (currentPage >= totalPages - sideCount - 1) {
    pages.push(1);
    pages.push("ellipsis");
    for (let i = totalPages - paginationSize + 3; i <= totalPages; i++) {
      pages.push(i);
    }
  } else {
    pages.push(1);
    pages.push("ellipsis");
    for (let i = currentPage - sideCount; i <= currentPage + sideCount; i++) {
      pages.push(i);
    }
    pages.push("ellipsis");
    pages.push(totalPages);
  }

  return pages;
}

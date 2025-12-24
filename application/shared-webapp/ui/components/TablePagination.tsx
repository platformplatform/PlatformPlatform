import { ArrowLeftIcon, ArrowRightIcon } from "lucide-react";
import type { ReactNode } from "react";
import { useCallback, useRef } from "react";
import { cn } from "../utils";
import { Button } from "./Button";
import { Pagination, PaginationContent, PaginationEllipsis, PaginationItem } from "./Pagination";

type TablePaginationProps = {
  paginationSize?: number;
  currentPage: number;
  totalPages: number;
  previousLabel?: ReactNode;
  nextLabel?: ReactNode;
  onPageChange: (page: number) => void;
  className?: string;
};

export function TablePagination({
  paginationSize = 9,
  currentPage,
  totalPages,
  onPageChange,
  previousLabel,
  nextLabel,
  className
}: Readonly<TablePaginationProps>) {
  if (paginationSize % 2 === 0 || paginationSize < 5) {
    throw new Error("Pagination size must be an odd number greater than or equal to 5.");
  }

  const handlePrevious = () => {
    onPageChange(currentPage - 1);
  };

  const handleNext = () => {
    onPageChange(currentPage + 1);
  };

  return (
    <Pagination className={cn("justify-between gap-4", className)}>
      <Button
        variant="secondary"
        onClick={handlePrevious}
        disabled={currentPage === 1}
        aria-label={previousLabel ? String(previousLabel) : "Previous page"}
      >
        <ArrowLeftIcon className="h-4 w-4" />
        {previousLabel && <span className="hidden sm:block">{previousLabel}</span>}
      </Button>

      <PaginationContent>
        <PageNumberButtons
          paginationSize={paginationSize}
          currentPage={currentPage}
          totalPages={totalPages}
          onPageChange={onPageChange}
        />
      </PaginationContent>

      <Button
        variant="secondary"
        onClick={handleNext}
        disabled={currentPage === totalPages}
        aria-label={nextLabel ? String(nextLabel) : "Next page"}
      >
        {nextLabel && <span className="hidden sm:block">{nextLabel}</span>}
        <ArrowRightIcon className="h-4 w-4" />
      </Button>
    </Pagination>
  );
}

type PageNumberProps = {
  paginationSize: number;
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
};

function PageNumberButtons({ paginationSize, currentPage, totalPages, onPageChange }: Readonly<PageNumberProps>) {
  const containerRef = useRef<HTMLDivElement>(null);

  const isSmall = totalPages <= paginationSize;
  const isBeginning = isSmall || currentPage <= Math.ceil(paginationSize / 2);
  const isEnd = !isBeginning && currentPage > totalPages - paginationSize + Math.ceil(paginationSize / 2);
  const isCenter = !isBeginning && !isEnd;

  const startCount = isSmall ? totalPages : isBeginning ? Math.min(totalPages, Math.max(2, paginationSize - 2)) : 1;
  const centerCount = isCenter ? Math.max(1, paginationSize - 4) : 0;
  const endCount = isSmall ? 0 : isEnd ? paginationSize - startCount - centerCount - 1 : 1;

  const startPages = Array.from({ length: startCount }, (_, i) => i + 1);
  const centerPages = Array.from({ length: centerCount }, (_, i) => currentPage - (centerCount - 1) / 2 + i);
  const endPages = Array.from({ length: endCount }, (_, i) => totalPages - endCount + i + 1);

  const handleKeyDown = useCallback(
    (event: React.KeyboardEvent) => {
      if (event.key === "ArrowLeft") {
        event.preventDefault();
        const prevPage = currentPage - 1;
        if (prevPage >= 1) {
          onPageChange(prevPage);
        } else {
          onPageChange(totalPages);
        }
        focusPreviousButton(containerRef.current, event.currentTarget as HTMLButtonElement);
      }
      if (event.key === "ArrowRight") {
        event.preventDefault();
        const nextPage = currentPage + 1;
        if (nextPage <= totalPages) {
          onPageChange(nextPage);
        } else {
          onPageChange(1);
        }
        focusNextButton(containerRef.current, event.currentTarget as HTMLButtonElement);
      }
    },
    [currentPage, totalPages, onPageChange]
  );

  return (
    <div ref={containerRef} className="flex items-center gap-1 rounded-md">
      {startPages.map((page) => (
        <PaginationItem key={page}>
          <PageNumberButton
            page={page}
            currentPage={currentPage}
            onPageChange={onPageChange}
            onKeyDown={handleKeyDown}
          />
        </PaginationItem>
      ))}
      {!isSmall && (
        <PaginationItem>
          <PaginationEllipsis />
        </PaginationItem>
      )}
      {centerPages.map((page) => (
        <PaginationItem key={page}>
          <PageNumberButton
            page={page}
            currentPage={currentPage}
            onPageChange={onPageChange}
            onKeyDown={handleKeyDown}
          />
        </PaginationItem>
      ))}
      {isCenter && (
        <PaginationItem>
          <PaginationEllipsis />
        </PaginationItem>
      )}
      {endPages.map((page) => (
        <PaginationItem key={page}>
          <PageNumberButton
            page={page}
            currentPage={currentPage}
            onPageChange={onPageChange}
            onKeyDown={handleKeyDown}
          />
        </PaginationItem>
      ))}
    </div>
  );
}

type PageNumberButtonProps = {
  page: number;
  currentPage: number;
  onPageChange: (page: number) => void;
  onKeyDown: (event: React.KeyboardEvent) => void;
};

function PageNumberButton({ page, currentPage, onPageChange, onKeyDown }: Readonly<PageNumberButtonProps>) {
  return (
    <Button
      aria-label={`Page number ${page}`}
      onClick={() => onPageChange(page)}
      variant={page === currentPage ? "secondary" : "ghost"}
      size="sm"
      className="tabular-nums duration-0"
      onKeyDown={onKeyDown}
    >
      {page}
    </Button>
  );
}

function focusNextButton(container: HTMLElement | null, currentButton: HTMLButtonElement) {
  if (!container) {
    return;
  }
  const buttons = Array.from(container.querySelectorAll<HTMLButtonElement>("button"));
  const currentIndex = buttons.indexOf(currentButton);
  const nextIndex = (currentIndex + 1) % buttons.length;
  buttons[nextIndex]?.focus();
}

function focusPreviousButton(container: HTMLElement | null, currentButton: HTMLButtonElement) {
  if (!container) {
    return;
  }
  const buttons = Array.from(container.querySelectorAll<HTMLButtonElement>("button"));
  const currentIndex = buttons.indexOf(currentButton);
  const prevIndex = (currentIndex - 1 + buttons.length) % buttons.length;
  buttons[prevIndex]?.focus();
}

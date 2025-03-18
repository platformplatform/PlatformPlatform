import { ArrowLeftIcon, ArrowRightIcon } from "lucide-react";
import type { ReactNode } from "react";
import { FocusScope, useFocusManager, useKeyboard } from "react-aria";
import { tv } from "tailwind-variants";
import { Button } from "./Button";

const pageBackgroundStyle = tv({
  base: "flex h-10 items-center gap-1 rounded-md"
});

const paginationStyles = tv({
  base: "flex w-full justify-between gap-4"
});

type PaginationProps = {
  paginationSize?: number;
  currentPage: number;
  totalPages: number;
  previousLabel?: ReactNode;
  nextLabel?: ReactNode;
  onPageChange: (page: number) => void;
  className?: string;
};

export function Pagination({
  paginationSize = 9,
  currentPage,
  totalPages,
  onPageChange,
  previousLabel,
  nextLabel,
  className
}: Readonly<PaginationProps>) {
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
    <nav aria-label="Pagination" className={paginationStyles({ className })}>
      <Button variant="secondary" className="" onPress={handlePrevious} isDisabled={currentPage === 1}>
        <ArrowLeftIcon className="h-4 w-4" />
        {previousLabel && <span className="hidden sm:block">{previousLabel}</span>}
      </Button>

      <FocusScope>
        <PageNumberButtons
          paginationSize={paginationSize}
          currentPage={currentPage}
          totalPages={totalPages}
          onPageChange={onPageChange}
        />
      </FocusScope>

      <Button variant="secondary" className="" onPress={handleNext} isDisabled={currentPage === totalPages}>
        {nextLabel && <span className="hidden sm:block">{nextLabel}</span>}
        <ArrowRightIcon className="h-4 w-4" />
      </Button>
    </nav>
  );
}

type PageNumberProps = {
  paginationSize: number;
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
};

function PageNumberButtons({ paginationSize, currentPage, totalPages, onPageChange }: Readonly<PageNumberProps>) {
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

  return (
    <ul className={pageBackgroundStyle()}>
      {startPages.map((page) => (
        <PageNumberButton
          key={page}
          page={page}
          currentPage={currentPage}
          totalPages={totalPages}
          onPageChange={onPageChange}
        />
      ))}
      {!isSmall && <Separator />}
      {centerPages.map((page) => (
        <PageNumberButton
          key={page}
          page={page}
          currentPage={currentPage}
          totalPages={totalPages}
          onPageChange={onPageChange}
        />
      ))}
      {isCenter && <Separator />}
      {endPages.map((page) => (
        <PageNumberButton
          key={page}
          page={page}
          currentPage={currentPage}
          totalPages={totalPages}
          onPageChange={onPageChange}
        />
      ))}
    </ul>
  );
}

type PageNumberButtonProps = {
  page: number;
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
};

function PageNumberButton({ page, currentPage, totalPages, onPageChange }: Readonly<PageNumberButtonProps>) {
  const focusManager = useFocusManager();

  const { keyboardProps } = useKeyboard({
    onKeyUp(event) {
      if (event.key === "ArrowLeft") {
        const prevPage = currentPage - 1;
        if (prevPage >= 1) {
          onPageChange(prevPage);
        } else {
          onPageChange(totalPages);
        }
        focusManager?.focusPrevious({ wrap: true });
      }
      if (event.key === "ArrowRight") {
        const nextPage = currentPage + 1;
        if (nextPage <= totalPages) {
          onPageChange(nextPage);
        } else {
          onPageChange(1);
        }
        focusManager?.focusNext({ wrap: true });
      }
    }
  });

  return (
    <li key={page} {...keyboardProps}>
      <Button
        aria-label={`Page number ${page}`}
        onPress={() => onPageChange(page)}
        variant={page === currentPage ? "secondary" : "ghost"}
        size="sm"
        className="tabular-nums duration-0"
      >
        {page}
      </Button>
    </li>
  );
}

function Separator() {
  return (
    <li>
      <Button isDisabled={true} variant="ghost">
        ...
      </Button>
    </li>
  );
}

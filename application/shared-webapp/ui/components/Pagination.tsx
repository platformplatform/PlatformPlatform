import { FocusScope, useFocusManager, useFocusVisible, useKeyboard } from "react-aria";
import { Button } from "./Button";
import { ArrowLeftIcon, ArrowRightIcon } from "lucide-react";
import { tv } from "tailwind-variants";
import type { ReactNode } from "react";

const pageBackgroundStyle = tv({
  base: "flex gap-1 rounded-md h-10 w-full items-center"
});

const paginationStyles = tv({
  base: "flex gap-4 w-full justify-between"
});

type PaginationProps = {
  size?: number;
  currentPage: number;
  totalPages: number;
  previousLabel?: ReactNode;
  nextLabel?: ReactNode;
  onPageChange: (page: number) => void;
  className?: string;
};

export function Pagination({
  size = 9,
  currentPage,
  totalPages,
  onPageChange,
  previousLabel,
  nextLabel,
  className
}: Readonly<PaginationProps>) {
  if (size % 2 === 0) {
    throw new Error("Pagination size must be an odd number");
  }

  const isEdge = currentPage < Math.floor(size / 2) || currentPage > totalPages - Math.floor(size / 2);
  const isSmall = totalPages <= size || (size <= 5 && totalPages <= 5);

  const handlePrevious = () => {
    onPageChange(currentPage - 1);
  };

  const handleNext = () => {
    onPageChange(currentPage + 1);
  };

  const isFirstPage = currentPage === 1;
  const isLastPage = currentPage === totalPages;

  return (
    <nav aria-label="Pagination" className={paginationStyles({ className })}>
      <Button variant="secondary" className="" onPress={handlePrevious} isDisabled={isFirstPage}>
        <ArrowLeftIcon className="w-4 h-4" />
        {previousLabel && <span className="hidden sm:block">{previousLabel}</span>}
      </Button>
      <FocusScope>
        {isEdge && !isSmall && (
          <EdgePagination size={size} currentPage={currentPage} onPageChange={onPageChange} totalPages={totalPages} />
        )}
        {!isEdge && !isSmall && (
          <CenterPagination size={size} currentPage={currentPage} onPageChange={onPageChange} totalPages={totalPages} />
        )}
        {isSmall && (
          <SmallPagination size={size} currentPage={currentPage} onPageChange={onPageChange} totalPages={totalPages} />
        )}
      </FocusScope>

      <Button variant="secondary" className="" onPress={handleNext} isDisabled={isLastPage}>
        {nextLabel && <span className="hidden sm:block">{nextLabel}</span>}
        <ArrowRightIcon className="w-4 h-4" />
      </Button>
    </nav>
  );
}

type PageNumberProps = {
  currentPage: number;
  totalPages: number;
  size: number;
  onPageChange: (page: number) => void;
};

function CenterPagination({ currentPage, onPageChange, totalPages, size }: Readonly<PageNumberProps>) {
  const centerCount = size - 4;
  const sideCount = (centerCount - 1) / 2;
  const pages = Array.from({ length: centerCount }, (_, i) => currentPage - sideCount + i);
  return (
    <ul className={pageBackgroundStyle()}>
      <PageNumberButton
        key={1}
        currentPage={currentPage}
        onPageChange={onPageChange}
        page={1}
        totalPages={totalPages}
      />
      <Separator />
      {pages.map((page) => (
        <PageNumberButton
          key={page}
          currentPage={currentPage}
          onPageChange={onPageChange}
          page={page}
          totalPages={totalPages}
        />
      ))}
      <Separator />
      <PageNumberButton
        key={totalPages}
        currentPage={currentPage}
        onPageChange={onPageChange}
        page={totalPages}
        totalPages={totalPages}
      />
    </ul>
  );
}

function EdgePagination({ currentPage, onPageChange, totalPages, size }: Readonly<PageNumberProps>) {
  const edgeCount = Math.floor(size / 2);
  const startPages = Array.from({ length: edgeCount }, (_, i) => i + 1);
  const endPages = Array.from({ length: edgeCount }, (_, i) => totalPages - edgeCount + i + 1);
  return (
    <ul className={pageBackgroundStyle()}>
      {startPages.map((page) => (
        <PageNumberButton
          key={page}
          currentPage={currentPage}
          onPageChange={onPageChange}
          page={page}
          totalPages={totalPages}
        />
      ))}
      <Separator />
      {endPages.map((page) => (
        <PageNumberButton
          key={page}
          currentPage={currentPage}
          onPageChange={onPageChange}
          page={page}
          totalPages={totalPages}
        />
      ))}
    </ul>
  );
}

function SmallPagination({ currentPage, onPageChange, totalPages }: Readonly<PageNumberProps>) {
  const pages = Array.from({ length: totalPages }, (_, i) => i + 1);
  return (
    <ul className={pageBackgroundStyle()}>
      {pages.map((page) => (
        <PageNumberButton
          key={page}
          currentPage={currentPage}
          onPageChange={onPageChange}
          page={page}
          totalPages={totalPages}
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

function PageNumberButton({ currentPage, totalPages, onPageChange, page }: Readonly<PageNumberButtonProps>) {
  const focusManager = useFocusManager();
  const { isFocusVisible } = useFocusVisible();

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
    <li key={page} {...keyboardProps} className="flex flex-1 grow justify-center">
      <Button
        aria-label={`Page number ${page}`}
        onPress={() => onPageChange(page)}
        variant={page === currentPage ? "secondary" : "ghost"}
        size="sm"
        autoFocus={page === currentPage && isFocusVisible}
        className="tabular-nums duration-0"
      >
        {page}
      </Button>
    </li>
  );
}

function Separator() {
  return (
    <li className="flex-1 grow">
      <Button isDisabled variant="ghost" className="font-bold text-lg tabular-nums">
        ..
      </Button>
    </li>
  );
}

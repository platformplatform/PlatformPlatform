import { ArrowLeftIcon, ArrowRightIcon } from "lucide-react";
import type React from "react";
import { Button } from "./Button";
import { twMerge } from "tailwind-merge";

interface PaginationProps {
  pageOffset: number;
  totalPages: number;
  onPageChange: (pageOffset: number) => void;
  className?: string;
}

export function Pagination({ pageOffset, totalPages, onPageChange, className }: Readonly<PaginationProps>) {
  const handlePrevious = () => {
    if (pageOffset > 0) onPageChange(pageOffset - 1);
  };

  const handleNext = () => {
    if (pageOffset + 1 < totalPages) onPageChange(pageOffset + 1);
  };

  return (
    <div className={twMerge("flex justify-between items-center space-x-2 md:space-x-4", className)}>
      <Button
        variant="secondary"
        className="flex text-sm items-center"
        onPress={handlePrevious}
        isDisabled={pageOffset === 0}
      >
        <ArrowLeftIcon size={16} />
        Previous
      </Button>
      <span className="hidden text-gray-500 sm:block">
        Page {pageOffset + 1} of {totalPages}
      </span>
      <Button
        variant="secondary"
        className="flex text-sm items-center"
        onPress={handleNext}
        isDisabled={pageOffset + 1 === totalPages}
      >
        Next <ArrowRightIcon size={16} />
      </Button>
    </div>
  );
}

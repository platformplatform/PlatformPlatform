import { ArrowLeftIcon, ArrowRightIcon } from "lucide-react";
import type React from "react";
import { Button } from "./Button";
import { twMerge } from "tailwind-merge";

interface PaginationProps {
  total: number;
  itemsPerPage: number;
  currentPage: number;
  onPageChange: (page: number) => void;
  className?: string;
}

export function Pagination({ total, itemsPerPage, currentPage, onPageChange, className }: Readonly<PaginationProps>) {
  const totalPages = Math.ceil(total / itemsPerPage);

  const handlePrevious = () => {
    if (currentPage > 1) onPageChange(currentPage - 1);
  };

  const handleNext = () => {
    if (currentPage < totalPages) onPageChange(currentPage + 1);
  };

  return (
    <div className={twMerge("flex justify-between items-center space-x-2 md:space-x-4", className)}>
      <Button
        variant="secondary"
        className="flex text-sm items-center"
        onPress={handlePrevious}
        isDisabled={currentPage === 1}
      >
        <ArrowLeftIcon size={16} />
        Previous
      </Button>
      <span className="hidden text-gray-500 sm:block">
        Page {currentPage} of {totalPages}
      </span>
      <Button
        variant="secondary"
        className="flex text-sm items-center"
        onPress={handleNext}
        isDisabled={currentPage === totalPages}
      >
        Next <ArrowRightIcon size={16} />
      </Button>
    </div>
  );
}

import React from "react";
import { ArrowLeftIcon, ArrowRightIcon } from "lucide-react";
import { Button } from "./Button";

interface PaginationProps {
  total: number;
  itemsPerPage: number;
  currentPage: number;
  onPageChange: (page: number) => void;
}

const Pagination: React.FC<PaginationProps> = ({ total, itemsPerPage, currentPage, onPageChange }) => {
  const totalPages = Math.ceil(total / itemsPerPage);

  const handlePrevious = () => {
    if (currentPage > 1)
      onPageChange(currentPage - 1);
  };

  const handleNext = () => {
    if (currentPage < totalPages)
      onPageChange(currentPage + 1);
  };

  return (
    <div className="flex justify-between items-center space-x-2 md:space-x-4">
      <Button variant="secondary" className="flex text-slate-700 text-sm items-center" onPress={handlePrevious} isDisabled={currentPage === 1}>
        <ArrowLeftIcon size={16} />Previous
      </Button>
      <span className="hidden text-gray-500 sm:block">
        Page {currentPage} of {totalPages}
      </span>
      <Button variant="secondary" className="flex text-slate-700 text-sm items-center" onPress={handleNext} isDisabled={currentPage === totalPages}>
        Next <ArrowRightIcon size={16} />
      </Button>
    </div>
  );
};

export default Pagination;

import { format, parseISO } from "date-fns";
import { ChevronDownIcon } from "lucide-react";
import { useState } from "react";
import { cn } from "../utils";
import { Button } from "./Button";
import { Calendar } from "./Calendar";
import { Field, FieldLabel } from "./Field";
import { Popover, PopoverContent, PopoverTrigger } from "./Popover";

export interface DatePickerProps {
  label?: string;
  value?: string;
  onChange?: (value: string | undefined) => void;
  placeholder?: string;
  className?: string;
  disabled?: boolean;
}

export function DatePicker({
  label,
  value,
  onChange,
  placeholder = "Select date",
  className,
  disabled
}: Readonly<DatePickerProps>) {
  const [open, setOpen] = useState(false);

  const selectedDate = value ? parseISO(value) : undefined;

  const handleSelect = (date: Date | undefined) => {
    if (date) {
      onChange?.(format(date, "yyyy-MM-dd"));
    } else {
      onChange?.(undefined);
    }
    setOpen(false);
  };

  return (
    <Field className={cn("flex flex-col", className)}>
      {label && <FieldLabel>{label}</FieldLabel>}
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger
          render={
            <Button variant="outline" className="w-48 justify-between font-normal" disabled={disabled}>
              {selectedDate ? format(selectedDate, "yyyy-MM-dd") : placeholder}
              <ChevronDownIcon className="size-4" />
            </Button>
          }
        />
        <PopoverContent className="w-auto overflow-hidden p-0" align="start">
          <Calendar mode="single" selected={selectedDate} onSelect={handleSelect} />
        </PopoverContent>
      </Popover>
    </Field>
  );
}

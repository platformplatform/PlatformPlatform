/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/index.html?path=/docs/calendar--docs
 * ref: https://ui.shadcn.com/docs/components/calendar
 */
import { ChevronLeft, ChevronRight } from "lucide-react";
import { useMemo } from "react";
import {
  Calendar as AriaCalendar,
  CalendarGridHeader as AriaCalendarGridHeader,
  type CalendarProps as AriaCalendarProps,
  CalendarCell,
  CalendarGrid,
  CalendarGridBody,
  CalendarHeaderCell,
  type DateValue,
  Heading,
  Text,
  useLocale
} from "react-aria-components";
import { tv } from "tailwind-variants";
import { Button } from "./Button";
import { focusRing } from "./focusRing";

export const cellStyles = tv({
  extend: focusRing,
  base: "w-9 h-9 text-sm cursor-default rounded-md flex items-center justify-center forced-color-adjust-none",
  variants: {
    isSelected: {
      false: "text-foreground hover:bg-accent pressed:bg-muted",
      true: "text-primary-foreground bg-primary forced-colors:bg-[Highlight] forced-colors:text-[HighlightText]"
    },
    isUnavailable: {
      true: "text-muted-foreground forced-colors:text-[GrayText]"
    },
    isInvalid: {
      true: "bg-destructive text-destructive-foreground forced-colors:invalid:bg-[Mark]"
    },
    isDisabled: {
      true: "text-muted-foreground opacity-50 forced-colors:text-[GrayText] strike"
    },
    isHovered: {
      true: "opacity-90 forced-colors:bg-[Highlight] forced-colors:text-[HighlightText]"
    },
    isPressed: {
      true: "opacity-80 forced-colors:bg-[Highlight] forced-colors:text-[HighlightText]"
    },
    isOutsideMonth: {
      true: "text-muted-foreground bg-transparent"
    }
  }
});

export interface CalendarProps<T extends DateValue> extends Omit<AriaCalendarProps<T>, "visibleDuration"> {
  errorMessage?: string;
  visibleMonths?: number;
}

export function Calendar<T extends DateValue>({
  errorMessage,
  visibleMonths = 1,
  ...props
}: Readonly<CalendarProps<T>>) {
  const durationInterval = useMemo(() => Array.from(Array(visibleMonths).keys()), [visibleMonths]);
  return (
    <AriaCalendar {...props} visibleDuration={{ months: visibleMonths }}>
      <CalendarHeader />
      <div className="flex gap-8 overflow-auto p-1">
        {durationInterval.map((months) => (
          <CalendarGrid key={months} offset={{ months }} className="w-[252px]">
            <CalendarGridHeader />
            <CalendarGridBody>{(date) => <CalendarCell date={date} className={cellStyles} />}</CalendarGridBody>
          </CalendarGrid>
        ))}
      </div>
      {errorMessage && (
        <Text slot="errorMessage" className="p-2 text-destructive text-sm">
          {errorMessage}
        </Text>
      )}
    </AriaCalendar>
  );
}

export function CalendarHeader() {
  const { direction } = useLocale();

  return (
    <header className="flex w-full items-center gap-1 px-1 pb-4">
      <Button variant="icon" slot="previous">
        {direction === "rtl" ? <ChevronRight aria-hidden /> : <ChevronLeft aria-hidden />}
      </Button>
      <Heading className="mx-2 flex-1 text-center font-semibold text-foreground text-md" />
      <Button variant="icon" slot="next">
        {direction === "rtl" ? <ChevronLeft aria-hidden /> : <ChevronRight aria-hidden />}
      </Button>
    </header>
  );
}

export function CalendarGridHeader() {
  return (
    <AriaCalendarGridHeader>
      {(day) => <CalendarHeaderCell className="font-semibold text-muted-foreground text-xs">{day}</CalendarHeaderCell>}
    </AriaCalendarGridHeader>
  );
}

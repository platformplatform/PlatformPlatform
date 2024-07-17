/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/index.html?path=/docs/calendar--docs
 * ref: https://ui.shadcn.com/docs/components/calendar
 */
import { useMemo } from "react";
import {
  RangeCalendar as AriaRangeCalendar,
  type RangeCalendarProps as AriaRangeCalendarProps,
  CalendarCell,
  CalendarGrid,
  CalendarGridBody,
  type DateValue,
  Text
} from "react-aria-components";
import { tv } from "tailwind-variants";
import { CalendarGridHeader, CalendarHeader } from "./Calendar";
import { focusRing } from "./focusRing";

export interface RangeCalendarProps<T extends DateValue> extends Omit<AriaRangeCalendarProps<T>, "visibleDuration"> {
  errorMessage?: string;
  visibleMonths?: number;
}

const cell = tv({
  extend: focusRing,
  base: "w-9 h-9 text-sm cursor-default rounded-md flex items-center justify-center forced-color-adjust-none",
  variants: {
    isSelected: {
      false: "text-foreground hover:bg-accent pressed:bg-muted",
      true: [
        "bg-accent text-accent-foreground forced-colors:bg-[Highlight] forced-colors:text-[HighlightText] rounded-none",
        "group-hover:bg-green-500 forced-colors:group-hover:bg-[Highlight]",
        "group-invalid:group-hover:bg-destructive forced-colors:group-invalid:group-hover:bg-[Mark]",
        "group-pressed:bg-red-500 forced-colors:group-pressed:bg-[Highlight] forced-colors:text-[HighlightText]",
        "group-invalid:group-pressed:bg-destructive forced-colors:group-invalid:group-pressed:bg-[Mark]"
      ]
    },
    isSelectionStart: {
      true: "text-primary-foreground bg-primary rounded-l-md group-invalid:bg-destructive forced-colors:bg-[Highlight] forced-colors:group-invalid:bg-[Mark] forced-colors:text-[HighlightText]"
    },
    isSelectionEnd: {
      true: "text-primary-foreground bg-primary rounded-r-md group-invalid:bg-destructive forced-colors:bg-[Highlight] forced-colors:group-invalid:bg-[Mark] forced-colors:text-[HighlightText]"
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
    },
    isOutsideMonthHidden: {
      true: "hidden"
    }
  }
});

export function RangeCalendar<T extends DateValue>({
  errorMessage,
  visibleMonths = 1,
  ...props
}: Readonly<RangeCalendarProps<T>>) {
  const durationInterval = useMemo(() => Array.from(Array(visibleMonths).keys()), [visibleMonths]);

  return (
    <AriaRangeCalendar {...props} visibleDuration={{ months: visibleMonths }}>
      <CalendarHeader />
      <div className="flex gap-8 overflow-auto p-1">
        {durationInterval.map((months) => (
          <CalendarGrid key={months} offset={{ months }} className="w-[252px] [&_td]:px-0">
            <CalendarGridHeader />
            <CalendarGridBody>
              {(date) => (
                <CalendarCell date={date} className={cell}>
                  {({ formattedDate, isOutsideMonth, ...renderProps }) => (
                    <span
                      className={cell({
                        ...renderProps,
                        isOutsideMonth,
                        isOutsideMonthHidden: isOutsideMonth && visibleMonths > 1
                      })}
                    >
                      {formattedDate}
                    </span>
                  )}
                </CalendarCell>
              )}
            </CalendarGridBody>
          </CalendarGrid>
        ))}
      </div>
      {errorMessage && (
        <Text slot="errorMessage" className="p-2 text-destructive text-sm">
          {errorMessage}
        </Text>
      )}
    </AriaRangeCalendar>
  );
}

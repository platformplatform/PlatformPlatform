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
  base: "flex h-9 w-9 cursor-default items-center justify-center rounded-md text-sm forced-color-adjust-none",
  variants: {
    isSelected: {
      false: "pressed:bg-muted text-foreground hover:bg-accent",
      true: [
        "rounded-none bg-accent text-accent-foreground forced-colors:bg-[Highlight] forced-colors:text-[HighlightText]",
        "group-hover:bg-green-500 forced-colors:group-hover:bg-[Highlight]",
        "group-invalid:group-hover:bg-destructive forced-colors:group-invalid:group-hover:bg-[Mark]",
        "group-pressed:bg-red-500 forced-colors:text-[HighlightText] forced-colors:group-pressed:bg-[Highlight]",
        "group-invalid:group-pressed:bg-destructive forced-colors:group-invalid:group-pressed:bg-[Mark]"
      ]
    },
    isSelectionStart: {
      true: "rounded-l-md bg-primary text-primary-foreground group-invalid:bg-destructive forced-colors:bg-[Highlight] forced-colors:text-[HighlightText] forced-colors:group-invalid:bg-[Mark]"
    },
    isSelectionEnd: {
      true: "rounded-r-md bg-primary text-primary-foreground group-invalid:bg-destructive forced-colors:bg-[Highlight] forced-colors:text-[HighlightText] forced-colors:group-invalid:bg-[Mark]"
    },
    isUnavailable: {
      true: "text-muted-foreground forced-colors:text-[GrayText]"
    },
    isInvalid: {
      true: "bg-destructive text-destructive-foreground forced-colors:invalid:bg-[Mark]"
    },
    isDisabled: {
      true: "strike text-muted-foreground opacity-50 forced-colors:text-[GrayText]"
    },
    isHovered: {
      true: "opacity-90 forced-colors:bg-[Highlight] forced-colors:text-[HighlightText]"
    },
    isPressed: {
      true: "opacity-80 forced-colors:bg-[Highlight] forced-colors:text-[HighlightText]"
    },
    isOutsideMonth: {
      true: "bg-transparent text-muted-foreground"
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
  const durationInterval = useMemo(() => Array.from(new Array(visibleMonths).keys()), [visibleMonths]);

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

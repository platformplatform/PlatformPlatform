import { useLingui } from "@lingui/react";
import { ChevronDownIcon, ChevronLeftIcon, ChevronRightIcon } from "lucide-react";
import * as React from "react";
import { type DayButton, DayPicker, getDefaultClassNames, type Locale } from "react-day-picker";
import { da, enUS } from "react-day-picker/locale";
import { cn } from "../utils";
import { Button, buttonVariants } from "./Button";

/**
 * Maps app locale codes to react-day-picker locale objects.
 * Add new locales here when extending language support.
 */
const localeMap: Record<string, Locale> = {
  "en-US": enUS,
  "da-DK": da
};

/**
 * Capitalizes the first letter of a string.
 */
function capitalizeFirstLetter(text: string): string {
  return text.charAt(0).toUpperCase() + text.slice(1);
}

// NOTE: This diverges from stock ShadCN to use weekStartsOn=1 (Monday) instead of Sunday.
function Calendar({
  className,
  classNames,
  showOutsideDays = true,
  weekStartsOn = 1,
  captionLayout = "label",
  buttonVariant = "ghost",
  formatters,
  components,
  locale: localeProp,
  ...props
}: React.ComponentProps<typeof DayPicker> & {
  buttonVariant?: React.ComponentProps<typeof Button>["variant"];
}) {
  const { i18n } = useLingui();
  const locale = localeProp ?? localeMap[i18n.locale] ?? enUS;
  const defaultClassNames = getDefaultClassNames();

  return (
    <DayPicker
      locale={locale}
      showOutsideDays={showOutsideDays}
      weekStartsOn={weekStartsOn}
      className={cn(
        // NOTE: This diverges from stock ShadCN to use 44px cell size for Apple HIG touch target compliance.
        "group/calendar bg-background p-2 [--cell-radius:var(--radius-md)] [--cell-size:--spacing(11)] [--rdp-nav_button-height:--spacing(11)] [--rdp-nav_button-width:--spacing(11)] [[data-slot=card-content]_&]:bg-transparent [[data-slot=popover-content]_&]:bg-transparent",
        String.raw`rtl:[&_.rdp-button\_next>svg]:rotate-180`,
        String.raw`rtl:[&_.rdp-button\_previous>svg]:rotate-180`,
        className
      )}
      captionLayout={captionLayout}
      formatters={{
        // NOTE: This diverges from stock ShadCN to capitalize month names in caption (e.g., "Januar" instead of "januar" in Danish).
        formatCaption: (date, options) => {
          const month = date.toLocaleString(options?.locale?.code ?? "default", { month: "long" });
          const year = date.getFullYear();
          return `${capitalizeFirstLetter(month)} ${year}`;
        },
        formatMonthDropdown: (date) => date.toLocaleString("default", { month: "short" }),
        ...formatters
      }}
      classNames={{
        root: cn("w-fit", defaultClassNames.root),
        months: cn("relative flex flex-col gap-4 md:flex-row", defaultClassNames.months),
        month: cn("flex w-full flex-col gap-4", defaultClassNames.month),
        nav: cn("absolute inset-x-0 top-0 flex w-full items-center justify-between gap-1", defaultClassNames.nav),
        button_previous: cn(
          buttonVariants({ variant: buttonVariant, size: "icon" }),
          "size-(--cell-size) select-none aria-disabled:opacity-50",
          defaultClassNames.button_previous
        ),
        button_next: cn(
          buttonVariants({ variant: buttonVariant, size: "icon" }),
          "size-(--cell-size) select-none aria-disabled:opacity-50",
          defaultClassNames.button_next
        ),
        month_caption: cn(
          "flex h-(--cell-size) w-full items-center justify-center px-(--cell-size)",
          defaultClassNames.month_caption
        ),
        dropdowns: cn(
          "flex h-(--cell-size) w-full items-center justify-center gap-1.5 font-medium text-sm",
          defaultClassNames.dropdowns
        ),
        dropdown_root: cn(
          "cn-calendar-dropdown-root relative rounded-(--cell-radius)",
          defaultClassNames.dropdown_root
        ),
        dropdown: cn("absolute inset-0 bg-popover opacity-0", defaultClassNames.dropdown),
        caption_label: cn(
          "select-none font-medium",
          captionLayout === "label"
            ? "text-sm"
            : "cn-calendar-caption-label flex items-center gap-1 rounded-(--cell-radius) text-sm [&>svg]:size-3.5 [&>svg]:text-muted-foreground",
          defaultClassNames.caption_label
        ),
        table: "w-full border-collapse",
        weekdays: cn("flex", defaultClassNames.weekdays),
        weekday: cn(
          "flex-1 select-none rounded-(--cell-radius) font-normal text-[0.8rem] text-muted-foreground",
          defaultClassNames.weekday
        ),
        week: cn("mt-1 flex w-full", defaultClassNames.week),
        week_number_header: cn("w-(--cell-size) select-none", defaultClassNames.week_number_header),
        week_number: cn("select-none text-[0.8rem] text-muted-foreground", defaultClassNames.week_number),
        day: cn(
          "group/day relative aspect-square h-full w-full select-none rounded-(--cell-radius) p-0 text-center [&:last-child[data-selected=true]_button]:rounded-r-(--cell-radius)",
          props.showWeekNumber
            ? "[&:nth-child(2)[data-selected=true]_button]:rounded-l-(--cell-radius)"
            : "[&:first-child[data-selected=true]_button]:rounded-l-(--cell-radius)",
          defaultClassNames.day
        ),
        range_start: cn(
          "relative isolate -z-0 rounded-l-(--cell-radius) bg-muted after:absolute after:inset-y-0 after:right-0 after:w-4 after:bg-muted",
          defaultClassNames.range_start
        ),
        range_middle: cn("rounded-none", defaultClassNames.range_middle),
        range_end: cn(
          "relative isolate -z-0 rounded-r-(--cell-radius) bg-muted after:absolute after:inset-y-0 after:left-0 after:w-4 after:bg-muted",
          defaultClassNames.range_end
        ),
        today: cn(
          "rounded-(--cell-radius) bg-muted text-foreground data-[selected=true]:rounded-none",
          defaultClassNames.today
        ),
        outside: cn("text-muted-foreground aria-selected:text-muted-foreground", defaultClassNames.outside),
        disabled: cn("text-muted-foreground opacity-50", defaultClassNames.disabled),
        hidden: cn("invisible", defaultClassNames.hidden),
        ...classNames
      }}
      components={{
        Root: ({ className, rootRef, ...props }) => {
          return <div data-slot="calendar" ref={rootRef} className={cn(className)} {...props} />;
        },
        Chevron: ({ className, orientation, ...props }) => {
          if (orientation === "left") {
            return <ChevronLeftIcon className={cn("size-4", className)} {...props} />;
          }

          if (orientation === "right") {
            return <ChevronRightIcon className={cn("size-4", className)} {...props} />;
          }

          return <ChevronDownIcon className={cn("size-4", className)} {...props} />;
        },
        DayButton: CalendarDayButton,
        WeekNumber: ({ children, ...props }) => {
          return (
            <td {...props}>
              <div className="flex size-(--cell-size) items-center justify-center text-center">{children}</div>
            </td>
          );
        },
        ...components
      }}
      {...props}
    />
  );
}

// NOTE: This diverges from stock ShadCN to add hover variants that maintain selected styling when hovering over selected dates.
function CalendarDayButton({ className, day, modifiers, ...props }: React.ComponentProps<typeof DayButton>) {
  const defaultClassNames = getDefaultClassNames();

  const ref = React.useRef<HTMLButtonElement>(null);
  React.useEffect(() => {
    if (modifiers.focused) {
      ref.current?.focus();
    }
  }, [modifiers.focused]);

  return (
    <Button
      variant="ghost"
      size="icon"
      data-day={day.date.toLocaleDateString()}
      data-selected-single={
        modifiers.selected && !modifiers.range_start && !modifiers.range_end && !modifiers.range_middle
      }
      data-range-start={modifiers.range_start}
      data-range-end={modifiers.range_end}
      data-range-middle={modifiers.range_middle}
      className={cn(
        "relative isolate z-10 flex aspect-square size-auto w-full min-w-(--cell-size) flex-col gap-1 border-0 font-normal leading-none data-[range-end=true]:rounded-(--cell-radius) data-[range-middle=true]:rounded-none data-[range-start=true]:rounded-(--cell-radius) data-[range-end=true]:rounded-r-(--cell-radius) data-[range-start=true]:rounded-l-(--cell-radius) data-[range-end=true]:bg-primary data-[range-middle=true]:bg-muted data-[range-start=true]:bg-primary data-[selected-single=true]:bg-primary data-[range-end=true]:text-primary-foreground data-[range-middle=true]:text-foreground data-[range-start=true]:text-primary-foreground data-[selected-single=true]:text-primary-foreground hover:data-[range-end=true]:bg-primary hover:data-[range-start=true]:bg-primary hover:data-[selected-single=true]:bg-primary hover:data-[range-end=true]:text-primary-foreground hover:data-[range-start=true]:text-primary-foreground hover:data-[selected-single=true]:text-primary-foreground group-data-[focused=true]/day:relative group-data-[focused=true]/day:z-10 group-data-[focused=true]/day:border-ring group-data-[focused=true]/day:ring-[0.1875rem] group-data-[focused=true]/day:ring-ring/50 dark:hover:text-foreground [&>span]:text-xs [&>span]:opacity-70",
        defaultClassNames.day,
        className
      )}
      {...props}
    />
  );
}

export { Calendar, CalendarDayButton };

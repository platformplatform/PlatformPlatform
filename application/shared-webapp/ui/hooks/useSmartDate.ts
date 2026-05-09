import { plural, t } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { useCallback, useEffect, useState } from "react";

export type SmartDateType = "justNow" | "minutesAgo" | "hoursAgo" | "date";

export interface SmartDateResult {
  type: SmartDateType;
  value: number;
  formatted: string;
}

function formatDate(
  input: string,
  locale: string,
  includeTime = false,
  longMonth = false,
  omitCurrentYear = false,
  omitYear = false
): string {
  const date = new Date(input);
  // The year always renders as 4 digits when shown ("May 7, 2026", never "May 7, 26").
  // Callers that know they're rendering recent dates can pass `omitCurrentYear` to drop
  // the year for dates in the current calendar year ("May 7"); other years still show
  // the full 4-digit year so "May 7, 2025" stays unambiguous. Pass `omitYear` (e.g., for
  // narrow mobile cells) to drop the year unconditionally.
  const isCurrentYear = date.getFullYear() === new Date().getFullYear();
  const showYear = !omitYear && !(omitCurrentYear && isCurrentYear);
  const options: Intl.DateTimeFormatOptions = {
    month: longMonth ? "long" : "short",
    day: "numeric",
    ...(showYear ? { year: "numeric" } : {}),
    ...(includeTime && {
      hour: "2-digit",
      minute: "2-digit"
    })
  };
  return new Intl.DateTimeFormat(locale, options).format(date);
}

function getSmartDate(input: string | undefined | null, locale: string): SmartDateResult | null {
  if (!input) {
    return null;
  }

  const date = new Date(input);
  const now = new Date();
  const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);
  const diffInMinutes = Math.floor(diffInSeconds / 60);
  const diffInHours = Math.floor(diffInMinutes / 60);

  if (diffInSeconds < 60) {
    return { type: "justNow", value: 0, formatted: "" };
  }

  if (diffInMinutes < 60) {
    return { type: "minutesAgo", value: diffInMinutes, formatted: "" };
  }

  if (diffInHours < 24) {
    return { type: "hoursAgo", value: diffInHours, formatted: "" };
  }

  return { type: "date", value: 0, formatted: formatDate(input, locale) };
}

/**
 * Hook that returns smart date information and auto-updates every 10 seconds.
 * Returns the type of display needed (justNow, minutesAgo, hoursAgo, date)
 * along with the value for relative times or formatted string for dates.
 * Uses the app's current locale for date formatting.
 */
export function useSmartDate(date: string | undefined | null): SmartDateResult | null {
  const { i18n } = useLingui();
  const [, setTick] = useState(0);

  useEffect(() => {
    const interval = setInterval(() => {
      setTick((tick) => tick + 1);
    }, 10000);

    return () => clearInterval(interval);
  }, []);

  return getSmartDate(date, i18n.locale);
}

/**
 * Returns a locale-aware date formatting function based on the app's current locale.
 * Uses the browser's built-in Intl.DateTimeFormat API, so any locale is automatically supported.
 *
 * Always shows a 4-digit year by default. Callers can pass `omitCurrentYear: true` to drop the
 * year for dates in the current calendar year (other years still show the 4-digit year).
 *
 * Short format, assuming current year is 2026:
 * - English (en-US): "May 7, 2026" or "May 7, 2026, 8:19 PM"; with omitCurrentYear: "May 7" or "May 7, 8:19 PM"
 * - Danish (da-DK): "7. maj 2026" or "7. maj 2026 20.19"; with omitCurrentYear: "7. maj" or "7. maj 20.19"
 *
 * - Any other locale: Automatically formatted according to browser's locale data
 */
export function useFormatDate() {
  const { i18n } = useLingui();
  const locale = i18n.locale;

  return useCallback(
    (input: string | undefined | null, includeTime = false, omitCurrentYear = false, omitYear = false): string => {
      if (!input) {
        return "";
      }
      return formatDate(input, locale, includeTime, false, omitCurrentYear, omitYear);
    },
    [locale]
  );
}

/**
 * Returns a locale-aware date formatting function that uses the full month name.
 * Use this in prose/banner text where space is not constrained.
 *
 * Always shows a 4-digit year by default. Callers can pass `omitCurrentYear: true` to drop the
 * year for dates in the current calendar year (other years still show the 4-digit year).
 *
 * Formats, assuming current year is 2026:
 * - English (en-US): "January 31, 2026"; with omitCurrentYear: "January 31"
 * - Danish (da-DK): "31. januar 2026"; with omitCurrentYear: "31. januar"
 */
export function useFormatLongDate() {
  const { i18n } = useLingui();
  const locale = i18n.locale;

  return useCallback(
    (input: string | undefined | null, omitCurrentYear = false): string => {
      if (!input) {
        return "";
      }
      return formatDate(input, locale, false, true, omitCurrentYear);
    },
    [locale]
  );
}

/**
 * Returns a locale-aware calendar-day-relative date formatting function. Suitable for date-only
 * values (no time component) where you want "Today", "Yesterday", "In 5 days", "5 days ago".
 * Falls back to the long date format ("April 19, 2026") outside ±5 days.
 */
export function useFormatRelativeDate() {
  const { i18n } = useLingui();
  const locale = i18n.locale;

  return useCallback(
    (input: string | undefined | null): string => {
      if (!input) {
        return "";
      }
      const date = new Date(input);
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      const target = new Date(date);
      target.setHours(0, 0, 0, 0);
      const diffDays = Math.round((target.getTime() - today.getTime()) / 86400000);
      if (diffDays === 0) return t`Today`;
      if (diffDays === -1) return t`Yesterday`;
      if (diffDays === 1) return t`Tomorrow`;
      if (diffDays >= 2 && diffDays <= 5) {
        return plural(diffDays, { one: "In # day", other: "In # days" });
      }
      if (diffDays <= -2 && diffDays >= -5) {
        return plural(-diffDays, { one: "# day ago", other: "# days ago" });
      }
      return formatDate(input, locale, false, true);
    },
    [locale]
  );
}

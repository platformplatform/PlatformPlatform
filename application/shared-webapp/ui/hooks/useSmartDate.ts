import { useLingui } from "@lingui/react";
import { useCallback, useEffect, useState } from "react";

export type SmartDateType = "justNow" | "minutesAgo" | "hoursAgo" | "date";

export interface SmartDateResult {
  type: SmartDateType;
  value: number;
  formatted: string;
}

function formatDate(input: string, locale: string, includeTime = false): string {
  const date = new Date(input);
  const options: Intl.DateTimeFormatOptions = {
    year: "numeric",
    month: "short",
    day: "numeric",
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
 * Formats:
 * - English (en-US): "Jan 31, 2026" or "Jan 31, 2026, 2:30 PM"
 * - Danish (da-DK): "31. jan. 2026" or "31. jan. 2026 14.30"
 * - Any other locale: Automatically formatted according to browser's locale data
 */
export function useFormatDate() {
  const { i18n } = useLingui();
  const locale = i18n.locale;

  return useCallback(
    (input: string | undefined | null, includeTime = false): string => {
      if (!input) {
        return "";
      }
      return formatDate(input, locale, includeTime);
    },
    [locale]
  );
}

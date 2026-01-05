import { formatDate } from "@repo/utils/date/formatDate";
import { useEffect, useState } from "react";

export type SmartDateType = "justNow" | "minutesAgo" | "hoursAgo" | "date";

export interface SmartDateResult {
  type: SmartDateType;
  value: number;
  formatted: string;
}

function getSmartDate(input: string | undefined | null): SmartDateResult | null {
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

  return { type: "date", value: 0, formatted: formatDate(input) };
}

/**
 * Hook that returns smart date information and auto-updates every 10 seconds.
 * Returns the type of display needed (justNow, minutesAgo, hoursAgo, date)
 * along with the value for relative times or formatted string for dates.
 */
export function useSmartDate(date: string | undefined | null): SmartDateResult | null {
  const [, setTick] = useState(0);

  useEffect(() => {
    const interval = setInterval(() => {
      setTick((tick) => tick + 1);
    }, 10000);

    return () => clearInterval(interval);
  }, []);

  return getSmartDate(date);
}

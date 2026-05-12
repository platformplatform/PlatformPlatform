const SECOND_MS = 1000;
const MINUTE_MS = 60 * SECOND_MS;
const HOUR_MS = 60 * MINUTE_MS;
const DAY_MS = 24 * HOUR_MS;
const WEEK_MS = 7 * DAY_MS;
const MONTH_MS = 30 * DAY_MS;
const YEAR_MS = 365 * DAY_MS;

export function formatRelativeTime(input: string | null | undefined, locale: string): string {
  if (!input) {
    return "";
  }
  const date = new Date(input);
  const diffMs = date.getTime() - Date.now();
  const formatter = new Intl.RelativeTimeFormat(locale, { numeric: "auto" });
  const absMs = Math.abs(diffMs);
  if (absMs < MINUTE_MS) return formatter.format(Math.round(diffMs / SECOND_MS), "second");
  if (absMs < HOUR_MS) return formatter.format(Math.round(diffMs / MINUTE_MS), "minute");
  if (absMs < DAY_MS) return formatter.format(Math.round(diffMs / HOUR_MS), "hour");
  if (absMs < WEEK_MS) return formatter.format(Math.round(diffMs / DAY_MS), "day");
  if (absMs < MONTH_MS) return formatter.format(Math.round(diffMs / WEEK_MS), "week");
  if (absMs < YEAR_MS) return formatter.format(Math.round(diffMs / MONTH_MS), "month");
  return formatter.format(Math.round(diffMs / YEAR_MS), "year");
}

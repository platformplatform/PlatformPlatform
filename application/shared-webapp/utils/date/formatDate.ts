const MONTH_NAMES = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

/**
 * Format a date string to a consistent format across the application.
 * Format: "day Mon, year" (e.g., "15 Jan, 2024")
 * Or with time: "day Mon, year at time" (e.g., "15 Jan, 2024 at 14:30")
 */
export function formatDate(input: string | undefined | null, includeTime = false): string {
  if (!input) {
    return "";
  }
  const date = new Date(input);
  const day = date.getDate();
  const month = MONTH_NAMES[date.getMonth()];
  const year = date.getFullYear();

  if (includeTime) {
    const hours = date.getHours().toString().padStart(2, "0");
    const minutes = date.getMinutes().toString().padStart(2, "0");
    return `${day} ${month}, ${year} at ${hours}:${minutes}`;
  }

  return `${day} ${month}, ${year}`;
}

/**
 * Format a date to ISO date string (YYYY-MM-DD)
 */
export function toIsoDateString(date: Date): string {
  return date.toISOString().slice(0, 10);
}

/**
 * Get a date N days ago in ISO format
 */
export function getDateDaysAgo(days: number): string {
  const date = new Date();
  date.setDate(date.getDate() - days);
  return toIsoDateString(date);
}

/**
 * Get today's date in ISO format
 */
export function getTodayIsoDate(): string {
  return toIsoDateString(new Date());
}

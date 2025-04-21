/**
 * Format a date string to a consistent format across the application.
 * Format: "day month, year" (e.g., "15 Jan, 2024")
 */
export function formatDate(input: string | undefined | null): string {
  if (!input) {
    return "";
  }
  const date = new Date(input);
  return date.toLocaleDateString(undefined, {
    day: "numeric",
    month: "short",
    year: "numeric"
  });
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

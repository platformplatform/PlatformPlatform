import { plural, t } from "@lingui/core/macro";
import { useSmartDate } from "@repo/ui/hooks/useSmartDate";

interface SmartDateProps {
  date: string | undefined | null;
  className?: string;
}

/**
 * Displays a smart date that auto-updates every 10 seconds.
 * Shows relative time for recent dates, absolute date for older ones.
 */
export function SmartDate({ date, className }: Readonly<SmartDateProps>) {
  const result = useSmartDate(date);

  if (!result) {
    return null;
  }

  let text: string;
  switch (result.type) {
    case "justNow":
      text = t`Just now`;
      break;
    case "minutesAgo":
      text = plural(result.value, { one: "# minute ago", other: "# minutes ago" });
      break;
    case "hoursAgo":
      text = plural(result.value, { one: "# hour ago", other: "# hours ago" });
      break;
    case "date":
      text = result.formatted;
      break;
  }

  return <span className={className}>{text}</span>;
}

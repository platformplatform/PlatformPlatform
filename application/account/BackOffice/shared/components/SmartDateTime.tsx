import { plural, t } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { useFormatDate, useSmartDate } from "@repo/ui/hooks/useSmartDate";

interface SmartDateTimeProps {
  date: string | undefined | null;
  className?: string;
  /**
   * Append the clock time to older entries (e.g. "Yesterday, 14:02"). Default is `false` —
   * for billing/business surfaces a relative phrase is enough. Opt in only on security-sensitive
   * surfaces (login history, sessions, last-seen) where the exact time matters.
   */
  withTime?: boolean;
}

/**
 * Displays a relative timestamp that auto-updates every 10 seconds.
 *
 * Examples without time (default): "Just now", "12 minutes ago", "1 hour ago", "Yesterday",
 * "2 days ago", "Apr 22".
 * Examples with time (`withTime`): "Just now", "12 minutes ago", "1 hour ago, 14:02",
 * "Yesterday, 14:02", "2 days ago, 09:15", "Apr 22, 02:41".
 */
export function SmartDateTime({ date, className, withTime = false }: Readonly<SmartDateTimeProps>) {
  const result = useSmartDate(date);
  const formatDate = useFormatDate();
  const { i18n } = useLingui();

  if (!result || !date) {
    return null;
  }

  const formatTime = () =>
    new Intl.DateTimeFormat(i18n.locale, { hour: "2-digit", minute: "2-digit" }).format(new Date(date));

  let text: string;
  switch (result.type) {
    case "justNow":
      text = t`Just now`;
      break;
    case "minutesAgo":
      text = plural(result.value, { one: "# minute ago", other: "# minutes ago" });
      break;
    case "hoursAgo": {
      const relative = plural(result.value, { one: "# hour ago", other: "# hours ago" });
      text = withTime ? `${relative}, ${formatTime()}` : relative;
      break;
    }
    case "date": {
      const target = new Date(date);
      const todayStart = new Date();
      todayStart.setHours(0, 0, 0, 0);
      const targetStart = new Date(target);
      targetStart.setHours(0, 0, 0, 0);
      const diffDays = Math.round((todayStart.getTime() - targetStart.getTime()) / 86400000);
      let dayPart: string;
      if (diffDays === 1) {
        dayPart = t`Yesterday`;
      } else if (diffDays >= 2 && diffDays <= 5) {
        dayPart = plural(diffDays, { one: "# day ago", other: "# days ago" });
      } else {
        dayPart = formatDate(date);
      }
      text = withTime ? `${dayPart}, ${formatTime()}` : dayPart;
      break;
    }
  }

  return <span className={className}>{text}</span>;
}

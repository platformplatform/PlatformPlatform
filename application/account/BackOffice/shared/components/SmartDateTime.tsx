import { plural, t } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { useFormatDate, useSmartDate } from "@repo/ui/hooks/useSmartDate";

interface SmartDateTimeProps {
  date: string | undefined | null;
  className?: string;
}

/**
 * Displays a relative timestamp that auto-updates every 10 seconds and always includes the time
 * for older entries.
 *
 * Examples: "Just now", "12 minutes ago", "Yesterday, 14:02", "2 days ago, 09:15", "Apr 22, 02:41".
 *
 * Reuses the shared-webapp `useSmartDate` (relative resolution within the past 24h) and
 * `useFormatDate` (locale-aware short date) and adds calendar-day-relative phrasing with the
 * locale-formatted clock time appended.
 */
export function SmartDateTime({ date, className }: Readonly<SmartDateTimeProps>) {
  const result = useSmartDate(date);
  const formatDate = useFormatDate();
  const { i18n } = useLingui();

  if (!result || !date) {
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
    case "hoursAgo": {
      const time = new Intl.DateTimeFormat(i18n.locale, { hour: "2-digit", minute: "2-digit" }).format(new Date(date));
      const relative = plural(result.value, { one: "# hour ago", other: "# hours ago" });
      text = `${relative}, ${time}`;
      break;
    }
    case "date": {
      const target = new Date(date);
      const todayStart = new Date();
      todayStart.setHours(0, 0, 0, 0);
      const targetStart = new Date(target);
      targetStart.setHours(0, 0, 0, 0);
      const diffDays = Math.round((todayStart.getTime() - targetStart.getTime()) / 86400000);
      const time = new Intl.DateTimeFormat(i18n.locale, { hour: "2-digit", minute: "2-digit" }).format(target);
      let dayPart: string;
      if (diffDays === 1) {
        dayPart = t`Yesterday`;
      } else if (diffDays >= 2 && diffDays <= 5) {
        dayPart = plural(diffDays, { one: "# day ago", other: "# days ago" });
      } else {
        dayPart = formatDate(date);
      }
      text = `${dayPart}, ${time}`;
      break;
    }
  }

  return <span className={className}>{text}</span>;
}

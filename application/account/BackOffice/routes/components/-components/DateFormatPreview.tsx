import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useFormatDate, useFormatLongDate, useFormatRelativeDate, useSmartDate } from "@repo/ui/hooks/useSmartDate";
import { useMemo } from "react";

function SmartDateDisplay({ date, label }: { date: string; label: string }) {
  const smartDate = useSmartDate(date);
  const formatShortDate = useFormatDate();
  const formatLongDate = useFormatLongDate();
  const formatRelativeDate = useFormatRelativeDate();

  let relativeText = "";
  if (smartDate) {
    switch (smartDate.type) {
      case "justNow":
        relativeText = t`Just now`;
        break;
      case "minutesAgo":
        relativeText = t`${smartDate.value} minutes ago`;
        break;
      case "hoursAgo":
        relativeText = t`${smartDate.value} hours ago`;
        break;
      case "date":
        // Once we're past the hour-resolution part of useSmartDate, switch to day-relative
        // ("Yesterday", "5 days ago") so the relative cell stays human-readable.
        relativeText = formatRelativeDate(date);
        break;
    }
  }

  return (
    <div className="flex flex-col gap-1 rounded-md border p-3">
      <span className="text-xs font-medium text-muted-foreground">{label}</span>
      <div className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 text-sm">
        <span className="text-muted-foreground">
          <Trans>Relative</Trans>
        </span>
        <span>{relativeText}</span>
        <span className="text-muted-foreground">
          <Trans>Short date</Trans>
        </span>
        <span>{formatShortDate(date)}</span>
        <span className="text-muted-foreground">
          <Trans>Long date</Trans>
        </span>
        <span>{formatLongDate(date)}</span>
        <span className="text-muted-foreground">
          <Trans>Date and time</Trans>
        </span>
        <span>{formatShortDate(date, true)}</span>
      </div>
    </div>
  );
}

export function DateFormatPreview() {
  const now = useMemo(() => new Date(), []);
  const justNow = now.toISOString();
  const threeMinutesAgo = new Date(now.getTime() - 3 * 60 * 1000).toISOString();
  const twoHoursAgo = new Date(now.getTime() - 2 * 60 * 60 * 1000).toISOString();
  const yesterday = new Date(now.getTime() - 24 * 60 * 60 * 1000).toISOString();

  return (
    <div className="flex flex-col gap-2">
      <h4>
        <Trans>Date and time formatting</Trans>
      </h4>
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
        <SmartDateDisplay date={justNow} label={t`Just now`} />
        <SmartDateDisplay date={threeMinutesAgo} label={t`3 minutes ago`} />
        <SmartDateDisplay date={twoHoursAgo} label={t`2 hours ago`} />
        <SmartDateDisplay date={yesterday} label={t`Yesterday`} />
      </div>
    </div>
  );
}

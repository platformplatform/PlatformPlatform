import { t } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { ToggleGroup, ToggleGroupItem } from "@repo/ui/components/ToggleGroup";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { MaximizeIcon, MinimizeIcon } from "lucide-react";
import { useEffect, useState } from "react";

import { DashboardTrendPeriod } from "@/shared/lib/api/client";

interface DashboardHeaderProps {
  period: DashboardTrendPeriod;
  onPeriodChange: (period: DashboardTrendPeriod) => void;
}

export function DashboardHeader({ period, onPeriodChange }: Readonly<DashboardHeaderProps>) {
  const { i18n } = useLingui();
  const dateFormatter = new Intl.DateTimeFormat(i18n.locale, { weekday: "long", month: "long", day: "numeric" });
  const today = dateFormatter.format(new Date());

  // Browser fullscreen for kiosk mode — chrome (sidebar, tabs) hides until the user exits.
  const [isFullscreen, setIsFullscreen] = useState(false);

  useEffect(() => {
    const updateState = () => setIsFullscreen(document.fullscreenElement !== null);
    updateState();
    document.addEventListener("fullscreenchange", updateState);
    return () => document.removeEventListener("fullscreenchange", updateState);
  }, []);

  const toggleFullscreen = () => {
    if (document.fullscreenElement === null) {
      void document.documentElement.requestFullscreen();
    } else {
      void document.exitFullscreen();
    }
  };

  const fullscreenLabel = isFullscreen ? t`Exit kiosk mode` : t`Enter kiosk mode`;

  return (
    <div className="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1>
          <Trans>Dashboard</Trans>
        </h1>
        <p className="mt-1 text-muted-foreground">
          <Trans>Back Office overview · {today}</Trans>
        </p>
      </div>
      <div className="flex items-center gap-2">
        <ToggleGroup
          variant="outline"
          aria-label={t`Period`}
          value={[period]}
          onValueChange={(values) => {
            const next = values[0];
            if (next) {
              onPeriodChange(next as DashboardTrendPeriod);
            }
          }}
        >
          <ToggleGroupItem value={DashboardTrendPeriod.Last7Days} className="min-w-[3.5rem] justify-center">
            <Trans>7d</Trans>
          </ToggleGroupItem>
          <ToggleGroupItem value={DashboardTrendPeriod.Last30Days} className="min-w-[3.5rem] justify-center">
            <Trans>30d</Trans>
          </ToggleGroupItem>
          <ToggleGroupItem value={DashboardTrendPeriod.Last90Days} className="min-w-[3.5rem] justify-center">
            <Trans>90d</Trans>
          </ToggleGroupItem>
        </ToggleGroup>
        <Tooltip>
          <TooltipTrigger
            render={
              <Button variant="outline" size="icon-sm" onClick={toggleFullscreen} aria-label={fullscreenLabel}>
                {isFullscreen ? <MinimizeIcon className="size-4" /> : <MaximizeIcon className="size-4" />}
              </Button>
            }
          />
          <TooltipContent>{fullscreenLabel}</TooltipContent>
        </Tooltip>
      </div>
    </div>
  );
}

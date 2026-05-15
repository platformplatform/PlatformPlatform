import { Trans } from "@lingui/react/macro";
import { CircleCheckIcon, CircleSlashIcon, PenLineIcon, UsersIcon } from "lucide-react";

interface OverrideStatsProps {
  total: number;
  enabled: number;
  disabled: number;
  override: number;
  showOverride: boolean;
}

// Compact summary chips shown above the table. Counts come from the response and reflect the
// population after search/plans/roles filtering but before state/has-override filtering — so the
// numbers describe the addressable audience for the flag rather than the current view.
export function OverrideStats({ total, enabled, disabled, override, showOverride }: Readonly<OverrideStatsProps>) {
  return (
    <div className="flex flex-wrap items-center gap-2 text-sm">
      <StatChip
        icon={<UsersIcon className="size-3.5" aria-hidden={true} />}
        value={total}
        label={<Trans>Total</Trans>}
        className="border-border text-muted-foreground"
      />
      <StatChip
        icon={<CircleCheckIcon className="size-3.5" aria-hidden={true} />}
        value={enabled}
        label={<Trans>Enabled</Trans>}
        className="border-success/30 text-success"
      />
      <StatChip
        icon={<CircleSlashIcon className="size-3.5" aria-hidden={true} />}
        value={disabled}
        label={<Trans>Disabled</Trans>}
        className="border-muted-foreground/30 text-muted-foreground"
      />
      {showOverride && (
        <StatChip
          icon={<PenLineIcon className="size-3.5" aria-hidden={true} />}
          value={override}
          label={<Trans>With override</Trans>}
          className="border-warning/30 text-warning"
        />
      )}
    </div>
  );
}

function StatChip({
  icon,
  value,
  label,
  className
}: Readonly<{
  icon: React.ReactNode;
  value: number;
  label: React.ReactNode;
  className: string;
}>) {
  return (
    <span className={`inline-flex items-center gap-1.5 rounded-md border px-2 py-1 ${className}`}>
      {icon}
      <span className="font-medium tabular-nums">{value}</span>
      <span>{label}</span>
    </span>
  );
}

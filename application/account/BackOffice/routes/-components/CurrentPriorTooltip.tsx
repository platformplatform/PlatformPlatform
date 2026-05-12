import { useLingui } from "@lingui/react";

type TooltipPointPayload = {
  date?: string;
  priorDate?: string;
  current?: number;
  prior?: number;
};

type CurrentPriorTooltipProps = {
  active?: boolean;
  payload?: ReadonlyArray<{ payload?: TooltipPointPayload }>;
  formatValue: (value: number) => string;
  accentColor: string;
};

export function CurrentPriorTooltip({ active, payload, formatValue, accentColor }: Readonly<CurrentPriorTooltipProps>) {
  const { i18n } = useLingui();
  if (!active || !payload || payload.length === 0) return null;
  const data = payload[0]?.payload;
  if (!data) return null;

  const current = data.current ?? 0;
  const prior = data.prior ?? 0;
  const deltaPercent = prior === 0 ? null : ((current - prior) / prior) * 100;

  const dateFormatter = new Intl.DateTimeFormat(i18n.locale, { month: "short", day: "numeric" });
  const currentLabel = data.date ? dateFormatter.format(new Date(data.date)) : "—";
  const priorLabel = data.priorDate ? dateFormatter.format(new Date(data.priorDate)) : "—";

  const deltaClassName =
    deltaPercent === null
      ? "text-muted-foreground"
      : deltaPercent > 0
        ? "text-success"
        : deltaPercent < 0
          ? "text-destructive"
          : "text-muted-foreground";
  const deltaText = deltaPercent === null ? "—" : `${deltaPercent >= 0 ? "+" : ""}${deltaPercent.toFixed(2)}%`;

  return (
    <div className="rounded-lg border border-border bg-popover px-3 py-2 text-xs text-popover-foreground shadow-sm">
      <div className={`mb-1.5 font-semibold ${deltaClassName}`}>{deltaText}</div>
      <div className="flex items-center gap-2">
        <span aria-hidden={true} className="size-2 rounded-full" style={{ background: accentColor }} />
        <span className="text-muted-foreground">{currentLabel}</span>
        <span className="ml-auto font-medium tabular-nums">{formatValue(current)}</span>
      </div>
      <div className="mt-0.5 flex items-center gap-2">
        <span aria-hidden={true} className="size-2 rounded-full bg-muted-foreground/60" />
        <span className="text-muted-foreground">{priorLabel}</span>
        <span className="ml-auto font-medium tabular-nums">{formatValue(prior)}</span>
      </div>
    </div>
  );
}

import { useLingui } from "@lingui/react";

type DeltaPercentProps = {
  value: number | null;
  fractionDigits?: number;
};

/**
 * Renders a percentage delta with consistent color semantics across the dashboard:
 * positive → green (text-success), negative → red (text-destructive), zero or null → muted.
 * Locale-aware number formatting via Intl. Caller decides whether to wrap with extra text
 * (e.g. "vs prior period") around the rendered span.
 */
export function DeltaPercent({ value, fractionDigits = 1 }: Readonly<DeltaPercentProps>) {
  const { i18n } = useLingui();

  if (value === null || Number.isNaN(value)) {
    return <span className="text-muted-foreground">—</span>;
  }

  const className = deltaClassName(value);
  const formatter = new Intl.NumberFormat(i18n.locale, {
    minimumFractionDigits: fractionDigits,
    maximumFractionDigits: fractionDigits,
    signDisplay: "exceptZero"
  });

  return <span className={className}>{formatter.format(value)}%</span>;
}

export function deltaClassName(value: number | null): string {
  if (value === null || Number.isNaN(value) || value === 0) return "text-muted-foreground";
  return value > 0 ? "text-success" : "text-destructive";
}

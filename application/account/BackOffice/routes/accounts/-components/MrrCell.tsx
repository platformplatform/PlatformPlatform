import { formatCurrency } from "@repo/utils/currency/formatCurrency";

import type { components } from "@/shared/lib/api/client";

import { PlannedSubscriptionChange } from "@/shared/lib/api/client";

type PlannedSubscriptionChangeValue = components["schemas"]["PlannedSubscriptionChange"] | null;

interface MrrCellProps {
  monthlyRecurringRevenue: number | null;
  scheduledPriceAmount: number | null;
  currency: string | null;
  plannedChange: PlannedSubscriptionChangeValue;
  align?: "start" | "end";
}

function formatMonthlyRevenue(amount: number | null, currency: string | null): string {
  if (amount === null || currency === null) return "-";
  return formatCurrency(amount, currency);
}

export function MrrCell({
  monthlyRecurringRevenue,
  scheduledPriceAmount,
  currency,
  plannedChange,
  align = "start"
}: Readonly<MrrCellProps>) {
  const currentAmount = formatMonthlyRevenue(monthlyRecurringRevenue, currency);
  const isCanceling = plannedChange === PlannedSubscriptionChange.Cancellation;
  const isDowngrading = plannedChange === PlannedSubscriptionChange.ScheduledPlanChange;
  const newAmount =
    isCanceling && currency !== null
      ? formatCurrency(0, currency)
      : isDowngrading && scheduledPriceAmount !== null && currency !== null
        ? formatCurrency(scheduledPriceAmount, currency)
        : null;

  if (newAmount === null) {
    return <span>{currentAmount}</span>;
  }

  return (
    <div className={`flex flex-col leading-tight ${align === "end" ? "items-end" : ""}`}>
      <span className="text-xs text-muted-foreground line-through">{currentAmount}</span>
      <span>{newAmount}</span>
    </div>
  );
}

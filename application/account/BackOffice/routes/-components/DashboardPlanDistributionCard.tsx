import { Trans } from "@lingui/react/macro";
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from "@repo/ui/components/Chart";
import { Skeleton } from "@repo/ui/components/Skeleton";

import { api, SubscriptionPlan } from "@/shared/lib/api/client";
import { getSubscriptionPlanLabel } from "@/shared/lib/api/labels";

import { DashboardCardShell } from "./DashboardCardShell";

const PLAN_COLORS: Record<SubscriptionPlan, string> = {
  Basis: "var(--chart-5)",
  Standard: "var(--chart-3)",
  Premium: "var(--chart-1)"
};

const PLAN_ORDER: SubscriptionPlan[] = [SubscriptionPlan.Premium, SubscriptionPlan.Standard, SubscriptionPlan.Basis];

export function DashboardPlanDistributionCard() {
  const { data, isLoading } = api.useQuery("get", "/api/back-office/dashboard/plan-distribution");

  const total = data?.totalTenants ?? 0;
  const distribution = (data?.distribution ?? [])
    .slice()
    .sort((a, b) => PLAN_ORDER.indexOf(a.plan) - PLAN_ORDER.indexOf(b.plan));

  return (
    <DashboardCardShell
      title={<Trans>Plan distribution</Trans>}
      subtitle={data ? <Trans>{total} accounts</Trans> : undefined}
    >
      {isLoading ? (
        <Skeleton className="h-[14rem] w-full" />
      ) : (
        <div className="flex flex-row items-center gap-4">
          <div className="relative h-[14rem] w-[14rem] shrink-0">
            <ResponsiveContainer width={224} height={224}>
              <PieChart accessibilityLayer={true}>
                <Tooltip
                  contentStyle={{
                    backgroundColor: "var(--popover)",
                    borderColor: "var(--border)",
                    borderRadius: "0.5rem",
                    color: "var(--popover-foreground)"
                  }}
                />
                <Pie
                  data={distribution}
                  dataKey="count"
                  nameKey="plan"
                  innerRadius={56}
                  outerRadius={88}
                  paddingAngle={2}
                  strokeWidth={0}
                >
                  {distribution.map((entry) => (
                    <Cell key={entry.plan} fill={PLAN_COLORS[entry.plan]} />
                  ))}
                </Pie>
              </PieChart>
            </ResponsiveContainer>
            <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center">
              <span className="text-2xl font-semibold tabular-nums">{total}</span>
              <span className="text-xs text-muted-foreground">
                <Trans>accounts</Trans>
              </span>
            </div>
          </div>
          <ul className="flex flex-1 flex-col gap-2 text-sm">
            {distribution.map((entry) => (
              <li key={entry.plan} className="flex items-center justify-between gap-3">
                <span className="flex items-center gap-2">
                  <span className="size-2.5 rounded-full" style={{ backgroundColor: PLAN_COLORS[entry.plan] }} />
                  <span>{getSubscriptionPlanLabel(entry.plan)}</span>
                </span>
                <span className="flex items-center gap-3 tabular-nums">
                  <span className="font-semibold">{entry.count}</span>
                  <span className="text-muted-foreground">{entry.percentage}%</span>
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </DashboardCardShell>
  );
}

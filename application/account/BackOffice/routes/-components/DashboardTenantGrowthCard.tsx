import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";

import type { DashboardTrendPeriod } from "@/shared/lib/api/client";

import { api, DashboardTrendMetric } from "@/shared/lib/api/client";

import { DashboardCardShell } from "./DashboardCardShell";

interface DashboardTenantGrowthCardProps {
  period: DashboardTrendPeriod;
}

export function DashboardTenantGrowthCard({ period }: Readonly<DashboardTenantGrowthCardProps>) {
  const { i18n } = useLingui();
  const { data, isLoading } = api.useQuery("get", "/api/back-office/dashboard/trends", {
    params: { query: { Metric: DashboardTrendMetric.NewTenants, Period: period } }
  });

  const points = data?.points ?? [];
  const total = points.reduce((acc, p) => acc + p.value, 0);
  const dateFormatter = new Intl.DateTimeFormat(i18n.locale, { month: "short", day: "numeric" });

  return (
    <DashboardCardShell
      title={<Trans>Tenant growth</Trans>}
      subtitle={data ? <Trans>{total} new signups</Trans> : undefined}
    >
      {isLoading ? (
        <Skeleton className="h-[14rem] w-full" />
      ) : (
        <ResponsiveContainer width="100%" height={224}>
          <BarChart data={points} margin={{ left: 8, right: 8, top: 12, bottom: 0 }}>
            <CartesianGrid vertical={false} stroke="var(--border)" strokeOpacity={0.5} />
            <XAxis
              dataKey="date"
              tickLine={false}
              axisLine={false}
              tickMargin={8}
              minTickGap={32}
              tickFormatter={(value: string) => dateFormatter.format(new Date(value))}
              stroke="var(--muted-foreground)"
            />
            <YAxis
              tickLine={false}
              axisLine={false}
              tickMargin={8}
              allowDecimals={false}
              width={56}
              stroke="var(--muted-foreground)"
            />
            <Tooltip
              cursor={{ fill: "var(--muted)", opacity: 0.3 }}
              labelFormatter={(value) => dateFormatter.format(new Date(value as string))}
              contentStyle={{
                backgroundColor: "var(--popover)",
                borderColor: "var(--border)",
                borderRadius: "0.5rem",
                color: "var(--popover-foreground)"
              }}
            />
            <Bar dataKey="value" fill="var(--chart-2)" radius={[4, 4, 0, 0]} />
          </BarChart>
        </ResponsiveContainer>
      )}
    </DashboardCardShell>
  );
}

import type { ReactNode } from "react";

import { Card, CardContent } from "@repo/ui/components/Card";

interface DashboardCardShellProps {
  title: ReactNode;
  subtitle?: ReactNode;
  action?: ReactNode;
  children: ReactNode;
}

export function DashboardCardShell({ title, subtitle, action, children }: Readonly<DashboardCardShellProps>) {
  return (
    <Card className="rounded-lg shadow-none">
      <CardContent className="flex flex-col gap-4 p-4">
        <div className="flex items-start justify-between gap-3">
          <div className="flex flex-col gap-1">
            <h4>{title}</h4>
            {subtitle && <p className="text-xs text-muted-foreground">{subtitle}</p>}
          </div>
          {action}
        </div>
        {children}
      </CardContent>
    </Card>
  );
}

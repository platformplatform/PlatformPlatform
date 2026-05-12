import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";

import { SmartDateTime } from "@/shared/components/SmartDateTime";
import { api, LoginEventOutcome } from "@/shared/lib/api/client";
import { getLoginMethodLabel } from "@/shared/lib/api/labels";

interface UserLoginHistorySectionProps {
  userId: string;
}

export function UserLoginHistorySection({ userId }: Readonly<UserLoginHistorySectionProps>) {
  const { data, isLoading } = api.useQuery("get", "/api/back-office/users/{id}/login-history", {
    params: { path: { id: userId } }
  });

  return (
    <section className="flex flex-col gap-3">
      <div className="text-sm text-muted-foreground">
        <Trans>
          Every sign-in attempt over the last 30 days, successful or failed, across email and external providers.
        </Trans>
      </div>
      {isLoading ? (
        <Skeleton className="h-24 w-full" />
      ) : !data || data.entries.length === 0 ? (
        <Empty className="border bg-card">
          <EmptyHeader>
            <EmptyTitle>
              <Trans>No login history</Trans>
            </EmptyTitle>
            <EmptyDescription>
              <Trans>No sign-in attempts in the last 30 days.</Trans>
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <Table rowSize="compact" aria-label={t`Login history`}>
          <TableHeader>
            <TableRow>
              <TableHead>
                <Trans>When</Trans>
              </TableHead>
              <TableHead>
                <Trans>Method</Trans>
              </TableHead>
              <TableHead>
                <Trans>Outcome</Trans>
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {data.entries.map((entry, index) => (
              <TableRow key={`${entry.kind}-${entry.occurredAt}-${index}`}>
                <TableCell>
                  <SmartDateTime date={entry.occurredAt} withTime={true} />
                </TableCell>
                <TableCell>{getLoginMethodLabel(entry.method)}</TableCell>
                <TableCell>
                  <OutcomeBadge outcome={entry.outcome} failureReason={entry.failureReason} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </section>
  );
}

function OutcomeBadge({
  outcome,
  failureReason
}: Readonly<{ outcome: LoginEventOutcome; failureReason: string | null }>) {
  if (outcome === LoginEventOutcome.Succeeded) {
    return (
      <Badge variant="outline" className="border-emerald-500/30 text-emerald-600">
        <Trans>Succeeded</Trans>
      </Badge>
    );
  }
  if (failureReason) {
    return (
      <Badge variant="outline" className="border-amber-500/30 text-amber-700 dark:text-amber-300">
        {failureReason}
      </Badge>
    );
  }
  if (outcome === LoginEventOutcome.Pending) {
    return (
      <Badge variant="outline" className="border-muted text-muted-foreground">
        <Trans>Pending</Trans>
      </Badge>
    );
  }
  return (
    <Badge variant="outline" className="border-destructive/30 text-destructive">
      <Trans>Failed</Trans>
    </Badge>
  );
}

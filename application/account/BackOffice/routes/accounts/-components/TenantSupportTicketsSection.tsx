import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { useNavigate } from "@tanstack/react-router";

import { CategoryPill } from "@/routes/support/-components/CategoryPill";
import { CsatScoreLabel } from "@/routes/support/-components/StaffCsatSummary";
import { StatusPill } from "@/routes/support/-components/StatusPill";
import { SmartDateTime } from "@/shared/components/SmartDateTime";
import { api } from "@/shared/lib/api/client";

interface TenantSupportTicketsSectionProps {
  tenantId: string;
}

export function TenantSupportTicketsSection({ tenantId }: Readonly<TenantSupportTicketsSectionProps>) {
  const navigate = useNavigate();
  // Mirrors the chip query so TanStack Query serves both from the same cache entry.
  const { data, isLoading } = api.useQuery("get", "/api/back-office/support-tickets", {
    params: { query: { TenantId: tenantId, PageSize: 100 } }
  });

  return (
    <section className="flex flex-col gap-3">
      <div className="text-sm text-muted-foreground">
        <Trans>Every support ticket filed by users of this account.</Trans>
      </div>
      {isLoading ? (
        <Skeleton className="h-24 w-full" />
      ) : !data || data.tickets.length === 0 ? (
        <Empty className="border bg-card">
          <EmptyHeader>
            <EmptyTitle>
              <Trans>No support tickets</Trans>
            </EmptyTitle>
            <EmptyDescription>
              <Trans>This account has no support tickets.</Trans>
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <Table rowSize="compact" aria-label={t`Support tickets`}>
          <TableHeader>
            <TableRow>
              <TableHead>
                <Trans>Subject</Trans>
              </TableHead>
              <TableHead>
                <Trans>Status</Trans>
              </TableHead>
              <TableHead className="text-right">
                <Trans>Last activity</Trans>
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {data.tickets.map((ticket) => (
              <TableRow
                key={ticket.id}
                className="cursor-pointer"
                onClick={() => navigate({ to: "/support/tickets/$ticketId", params: { ticketId: ticket.id } })}
              >
                <TableCell>
                  <div className="flex min-w-0 flex-col gap-1">
                    <span className="truncate font-medium text-foreground">{ticket.subject}</span>
                    <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
                      <CategoryPill category={ticket.category} />
                      <span className="font-mono text-xs text-muted-foreground">#{ticket.shortDisplayId}</span>
                    </div>
                  </div>
                </TableCell>
                <TableCell>
                  <div className="flex flex-col items-start gap-1">
                    <StatusPill status={ticket.status} />
                    {ticket.csatScore && <CsatScoreLabel score={ticket.csatScore} />}
                  </div>
                </TableCell>
                <TableCell className="text-right text-xs text-muted-foreground">
                  <SmartDateTime date={ticket.lastActivityAt} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </section>
  );
}

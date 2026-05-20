import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { useNavigate } from "@tanstack/react-router";
import { MailIcon, MessageSquareIcon, PaperclipIcon } from "lucide-react";

import { CategoryPill } from "@/routes/support/-components/CategoryPill";
import { getInitials } from "@/routes/support/-components/displayName";
import { CsatScoreLabel } from "@/routes/support/-components/StaffCsatSummary";
import { StatusPill } from "@/routes/support/-components/StatusPill";
import { SmartDateTime } from "@/shared/components/SmartDateTime";
import { api, SupportTicketStatus } from "@/shared/lib/api/client";

interface AccountOpenSupportTicketsCardProps {
  tenantId: string;
}

// Renders only when the tenant has at least one open ticket. Reuses the same query the support tab
// uses so TanStack Query serves both consumers from the same cache entry. Columns mirror the
// back-office inbox table minus the Account column, since we are already in the account context.
export function AccountOpenSupportTicketsCard({ tenantId }: Readonly<AccountOpenSupportTicketsCardProps>) {
  const navigate = useNavigate();
  const { data } = api.useQuery("get", "/api/back-office/support-tickets", {
    params: { query: { TenantId: tenantId, PageSize: 100 } }
  });

  const openTickets =
    data?.tickets.filter(
      (ticket) => ticket.status !== SupportTicketStatus.Resolved && ticket.status !== SupportTicketStatus.Closed
    ) ?? [];

  if (openTickets.length === 0) return null;

  return (
    <section>
      <h4 className="mb-3">
        <Trans>Open support tickets</Trans>
      </h4>
      <Table rowSize="spacious" aria-label={t`Open support tickets`}>
        <TableHeader>
          <TableRow>
            <TableHead>
              <Trans>Subject</Trans>
            </TableHead>
            <TableHead className="hidden md:table-cell">
              <Trans>Reporter</Trans>
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
          {openTickets.map((ticket) => {
            const reporterName = ticket.reporterName ?? ticket.reporterEmail.split("@")[0];
            return (
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
                      {ticket.messageCount > 0 && (
                        <span className="inline-flex items-center gap-1 text-xs text-muted-foreground">
                          <MessageSquareIcon className="size-3" aria-hidden={true} />
                          {ticket.messageCount}
                        </span>
                      )}
                      {ticket.attachmentCount > 0 && (
                        <span className="inline-flex items-center gap-1 text-xs text-muted-foreground">
                          <PaperclipIcon className="size-3" aria-hidden={true} />
                          {ticket.attachmentCount}
                        </span>
                      )}
                    </div>
                  </div>
                </TableCell>
                <TableCell className="hidden md:table-cell">
                  <div className="flex min-w-0 items-center gap-2">
                    <Avatar size="default" className="size-8">
                      <AvatarImage src={ticket.reporterAvatarUrl ?? undefined} alt="" />
                      <AvatarFallback>{getInitials(reporterName)}</AvatarFallback>
                    </Avatar>
                    <div className="flex min-w-0 flex-col">
                      <span className="truncate text-sm font-medium">{reporterName}</span>
                      <span className="flex min-w-0 items-center gap-1 text-xs text-muted-foreground">
                        <MailIcon className="size-3 shrink-0" aria-hidden={true} />
                        <span className="truncate">{ticket.reporterEmail}</span>
                      </span>
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
            );
          })}
        </TableBody>
      </Table>
    </section>
  );
}

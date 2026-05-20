import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { useNavigate } from "@tanstack/react-router";
import { MailIcon, MessageSquareIcon, PaperclipIcon } from "lucide-react";

import type { Schemas } from "@/shared/lib/api/client";

import { SmartDateTime } from "@/shared/components/SmartDateTime";
import { getSubscriptionPlanLabel } from "@/shared/lib/api/labels";

import { CategoryPill } from "./CategoryPill";
import { getInitials } from "./displayName";
import { CsatScoreLabel } from "./StaffCsatSummary";
import { StatusPill } from "./StatusPill";

type Ticket = Schemas["AllTicketsSummary"];

export function InboxTableRow({ ticket }: Readonly<{ ticket: Ticket }>) {
  const reporterName = ticket.reporterName ?? ticket.reporterEmail.split("@")[0];
  const navigate = useNavigate();
  // Single click selects the row and opens the preview side pane (handled by the parent Table's
  // selectionMode). Double click is the power-user shortcut to deep-link into the full detail page,
  // matching the side pane's "Open ticket" affordance.
  const handleDoubleClick = () => {
    navigate({ to: "/support/tickets/$ticketId", params: { ticketId: ticket.id } });
  };
  return (
    <TableRow rowKey={ticket.id} onDoubleClick={handleDoubleClick}>
      <TableCell>
        <Avatar size="default" className="size-8">
          <AvatarFallback className={ticket.assignee ? "bg-primary/10 text-primary" : undefined}>
            {ticket.assignee ? getInitials(ticket.assignee.displayName) : "?"}
          </AvatarFallback>
        </Avatar>
      </TableCell>
      <TableCell>
        <div className="flex min-w-0 flex-col gap-1">
          <span
            className={`truncate ${ticket.isUnreadForStaff ? "font-semibold text-foreground" : "font-medium text-foreground"}`}
          >
            {ticket.subject}
          </span>
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
      <TableCell className="hidden lg:table-cell">
        <div className="flex min-w-0 items-center gap-2">
          <TenantLogo logoUrl={ticket.tenantLogoUrl} tenantName={ticket.tenantName} size="md" className="size-8" />
          <div className="flex min-w-0 flex-col items-start gap-1">
            <span className="truncate text-sm font-medium">{ticket.tenantName}</span>
            <Badge variant="secondary">{getSubscriptionPlanLabel(ticket.tenantPlan)}</Badge>
          </div>
        </div>
      </TableCell>
      <TableCell>
        <div className="flex flex-col items-start gap-1">
          <StatusPill status={ticket.status} />
          {ticket.csatScore && <CsatScoreLabel score={ticket.csatScore} />}
        </div>
      </TableCell>
      <TableCell className="hidden text-right text-xs text-muted-foreground xl:table-cell">
        <SmartDateTime date={ticket.createdAt} />
      </TableCell>
      <TableCell className="text-right text-xs text-muted-foreground">
        <SmartDateTime date={ticket.lastActivityAt} />
      </TableCell>
    </TableRow>
  );
}

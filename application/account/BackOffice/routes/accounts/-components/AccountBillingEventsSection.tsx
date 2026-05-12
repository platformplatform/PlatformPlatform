import type { ReactNode } from "react";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { ArrowRightIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { AccountBillingEventRow } from "./AccountBillingEventRow";

type BillingEventSummary = components["schemas"]["BillingEventSummary"];
type RenderDate = (value: string | null | undefined) => ReactNode;

interface Props {
  billingEvents: BillingEventSummary[];
  isLoading: boolean;
  isCompact: boolean;
  totalEvents: number;
  onViewAll?: () => void;
  renderDate: RenderDate;
}

export function AccountBillingEventsSection({
  billingEvents,
  isLoading,
  isCompact,
  totalEvents,
  onViewAll,
  renderDate
}: Readonly<Props>) {
  return (
    <div className="flex min-h-0 flex-1 flex-col">
      {isCompact ? (
        <div className="mb-3 flex items-baseline justify-between gap-3">
          <h4 className="whitespace-nowrap">
            <Trans>Billing events</Trans>
          </h4>
          {onViewAll && totalEvents > 0 && (
            <Button
              variant="ghost"
              size="xs"
              onClick={onViewAll}
              className="ml-auto text-sm whitespace-nowrap text-muted-foreground hover:text-foreground max-sm:w-fit"
            >
              <Trans>View all {totalEvents} events</Trans>
              <ArrowRightIcon className="size-3.5" aria-hidden={true} />
            </Button>
          )}
        </div>
      ) : (
        <div className="mb-3 text-sm text-muted-foreground">
          <Trans>
            Plan changes, renewals, cancellations, and payment outcomes — the subscription lifecycle and its MRR impact
            over time.
          </Trans>
        </div>
      )}
      {isLoading && billingEvents.length === 0 ? (
        <div className="flex flex-col gap-2 rounded-lg border border-border bg-card p-2">
          {Array.from({ length: isCompact ? 2 : 5 }).map((_, index) => (
            <Skeleton key={`event-skeleton-${index}`} className="h-12 w-full" />
          ))}
        </div>
      ) : billingEvents.length === 0 ? (
        <Empty className="h-[8.375rem] flex-none border bg-card p-4 md:p-4">
          <EmptyHeader>
            <EmptyTitle>
              <Trans>No billing events</Trans>
            </EmptyTitle>
            <EmptyDescription>
              <Trans>Subscription, payment, and billing transitions will appear here.</Trans>
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <Table rowSize="compact" aria-label={t`Billing events`} stickyHeader={true}>
          <TableHeader>
            <TableRow>
              <TableHead>
                <Trans>Occurred</Trans>
              </TableHead>
              <TableHead>
                <Trans>Event</Trans>
              </TableHead>
              <TableHead className="hidden md:table-cell">
                <Trans>Plan</Trans>
              </TableHead>
              {isCompact ? (
                <TableHead className="text-right">
                  <Trans>MRR impact</Trans>
                </TableHead>
              ) : (
                <>
                  <TableHead className="text-right">
                    <Trans>MRR impact</Trans>
                  </TableHead>
                  <TableHead className="hidden text-right md:table-cell">
                    <Trans>MRR after</Trans>
                  </TableHead>
                </>
              )}
            </TableRow>
          </TableHeader>
          <TableBody>
            {billingEvents.map((event) => (
              <AccountBillingEventRow key={event.id} event={event} renderDate={renderDate} isCompact={isCompact} />
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  );
}

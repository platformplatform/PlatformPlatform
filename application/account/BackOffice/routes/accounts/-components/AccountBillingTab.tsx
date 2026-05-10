import type { ReactNode } from "react";

import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { keepPreviousData } from "@tanstack/react-query";
import { useCallback, useState } from "react";

import { api } from "@/shared/lib/api/client";

import { AccountBillingEventsSection } from "./AccountBillingEventsSection";
import { AccountBillingHistorySection } from "./AccountBillingHistorySection";

type AccountBillingTabVariant = "compact-both" | "history-full" | "events-full";

interface AccountBillingTabProps {
  tenantId: string;
  /**
   * `compact-both` — Overview tab: show last 2 events and last 2 invoices (no pagination).
   * `history-full` — Billing tab: full pageable list of invoices only.
   * `events-full` — Billing events tab: full list of events only, with MRR before/after columns.
   */
  variant: AccountBillingTabVariant;
  /** Click handler for the "View all # invoices" link rendered in compact mode. */
  onViewAllInvoices?: () => void;
  /** Click handler for the "View all # events" link rendered in compact mode. */
  onViewAllEvents?: () => void;
}

export function AccountBillingTab({
  tenantId,
  variant,
  onViewAllInvoices,
  onViewAllEvents
}: Readonly<AccountBillingTabProps>) {
  const formatDate = useFormatDate();
  const [pageOffset, setPageOffset] = useState(0);

  const isCompact = variant === "compact-both";
  const showHistory = variant === "compact-both" || variant === "history-full";
  const showEvents = variant === "compact-both" || variant === "events-full";

  // Compact (Overview) shows date only. Full views (Invoices, Billing events) include the clock
  // time so support can correlate Stripe webhooks with billing-event ordering. The mobile span
  // hides the year so the date column stays narrow on phones.
  const renderRowDate = useCallback(
    (input: string | null | undefined): ReactNode => (
      <>
        <span className="md:hidden">{formatDate(input, !isCompact, false, true)}</span>
        <span className="hidden md:inline">{formatDate(input, !isCompact)}</span>
      </>
    ),
    [formatDate, isCompact]
  );

  const paymentsPageSize = isCompact ? 2 : 25;
  const eventsPageSize = isCompact ? 2 : 50;

  const paymentsQuery = api.useQuery(
    "get",
    "/api/back-office/tenants/{id}/payment-history",
    {
      params: {
        path: { id: tenantId },
        query: { PageOffset: pageOffset || undefined, PageSize: paymentsPageSize }
      }
    },
    { placeholderData: keepPreviousData, enabled: showHistory }
  );

  const eventsQuery = api.useQuery(
    "get",
    "/api/back-office/billing-events",
    {
      params: { query: { TenantId: tenantId, PageSize: eventsPageSize } }
    },
    { placeholderData: keepPreviousData, enabled: showEvents }
  );

  const transactions = paymentsQuery.data?.transactions ?? [];
  const totalPages = paymentsQuery.data?.totalPages ?? 0;
  const currentPage = (paymentsQuery.data?.currentPageOffset ?? 0) + 1;
  const billingEvents = eventsQuery.data?.billingEvents ?? [];
  const totalEvents = eventsQuery.data?.totalCount ?? 0;
  const totalTransactions = paymentsQuery.data?.totalCount ?? 0;

  return (
    <section className="flex h-full flex-col gap-6">
      {showHistory && (
        <AccountBillingHistorySection
          transactions={transactions}
          isLoading={paymentsQuery.isLoading}
          isCompact={isCompact}
          totalTransactions={totalTransactions}
          totalPages={totalPages}
          currentPage={currentPage}
          onViewAll={onViewAllInvoices}
          onPageChange={setPageOffset}
          renderDate={renderRowDate}
        />
      )}
      {showEvents && (
        <AccountBillingEventsSection
          billingEvents={billingEvents}
          isLoading={eventsQuery.isLoading}
          isCompact={isCompact}
          totalEvents={totalEvents}
          onViewAll={onViewAllEvents}
          renderDate={renderRowDate}
        />
      )}
    </section>
  );
}

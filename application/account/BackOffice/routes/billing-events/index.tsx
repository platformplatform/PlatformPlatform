import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { requireSubscriptionEnabled } from "@repo/infrastructure/auth/routeGuards";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Button } from "@repo/ui/components/Button";
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@repo/ui/components/Empty";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { keepPreviousData } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { ZapIcon } from "lucide-react";
import { z } from "zod";

import type { BillingEventType } from "@/shared/lib/api/client";

import { BackOfficeSideMenu } from "@/shared/components/BackOfficeSideMenu";
import { api, SortableBillingEventProperties, SortOrder } from "@/shared/lib/api/client";
import {
  MRR_IMPACT_EVENT_TYPES,
  OTHER_EVENT_TYPES,
  SUBSCRIPTION_STATE_EVENT_TYPES
} from "@/shared/lib/billingEventCategories";

import { BillingEventsTable } from "./-components/BillingEventsTable";
import { BillingEventsToolbar, type BillingEventsView } from "./-components/BillingEventsToolbar";

// Drives the URL-controlled view pill. The backend filter accepts an open-ended EventTypes[] array;
// the pill maps the chosen view onto a fixed event-type set so operators don't have to think about
// which low-level types make up each business concept.
const EVENT_TYPES_FOR_VIEW: Record<BillingEventsView, BillingEventType[] | undefined> = {
  all: undefined,
  mrr: [...MRR_IMPACT_EVENT_TYPES],
  state: [...SUBSCRIPTION_STATE_EVENT_TYPES],
  other: [...OTHER_EVENT_TYPES]
};

const billingEventsSearchSchema = z.object({
  search: z.string().optional(),
  view: z.enum(["all", "mrr", "state", "other"]).optional(),
  orderBy: z.nativeEnum(SortableBillingEventProperties).optional(),
  sortOrder: z.nativeEnum(SortOrder).optional(),
  pageOffset: z.number().int().nonnegative().optional()
});

export const Route = createFileRoute("/billing-events/")({
  staticData: { trackingTitle: "Billing events" },
  validateSearch: billingEventsSearchSchema,
  beforeLoad: () => requireSubscriptionEnabled(),
  component: BillingEventsListPage
});

function BillingEventsListPage() {
  const { search, view, orderBy, sortOrder, pageOffset } = Route.useSearch();
  const navigate = useNavigate();
  const activeView: BillingEventsView = view ?? "all";

  const { data, isLoading } = api.useQuery(
    "get",
    "/api/back-office/billing-events",
    {
      params: {
        query: {
          Search: search,
          EventTypes: EVENT_TYPES_FOR_VIEW[activeView],
          OrderBy: orderBy,
          SortOrder: sortOrder,
          PageOffset: pageOffset
        }
      }
    },
    { placeholderData: keepPreviousData }
  );

  const billingEvents = data?.billingEvents ?? [];
  const hasFilters = Boolean(search) || activeView !== "all";
  const showEmpty = !isLoading && billingEvents.length === 0;

  return (
    <SidebarProvider>
      <BackOfficeSideMenu />
      <SidebarInset>
        <AppLayout
          variant="center"
          maxWidth="64rem"
          browserTitle={t`Billing events`}
          title={t`Billing events`}
          subtitle={t`Authoritative log of subscription, payment, and billing transitions across all accounts.`}
        >
          <BillingEventsToolbar search={search} view={activeView} />

          {showEmpty ? (
            <Empty>
              <EmptyHeader>
                <EmptyMedia variant="icon">
                  <ZapIcon />
                </EmptyMedia>
                <EmptyTitle>
                  {hasFilters ? (
                    <Trans>No billing events match your filters</Trans>
                  ) : (
                    <Trans>No billing events yet</Trans>
                  )}
                </EmptyTitle>
                <EmptyDescription>
                  {hasFilters ? (
                    <Trans>Try clearing the search or filters to see more results.</Trans>
                  ) : (
                    <Trans>
                      Subscription, payment, and billing transitions will appear here as Stripe webhooks are processed.
                    </Trans>
                  )}
                </EmptyDescription>
              </EmptyHeader>
              {hasFilters && (
                <EmptyContent>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() =>
                      navigate({
                        to: "/billing-events",
                        search: () => ({})
                      })
                    }
                  >
                    <Trans>Clear filters</Trans>
                  </Button>
                </EmptyContent>
              )}
            </Empty>
          ) : (
            <div className="flex min-h-0 flex-1 flex-col">
              <BillingEventsTable
                billingEvents={billingEvents}
                isLoading={isLoading}
                totalPages={data?.totalPages ?? 0}
                currentPageOffset={data?.currentPageOffset ?? 0}
                orderBy={orderBy}
                sortOrder={sortOrder}
              />
            </div>
          )}
        </AppLayout>
      </SidebarInset>
    </SidebarProvider>
  );
}

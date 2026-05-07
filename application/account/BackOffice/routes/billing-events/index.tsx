import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Button } from "@repo/ui/components/Button";
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@repo/ui/components/Empty";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { keepPreviousData } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { ZapIcon } from "lucide-react";
import { z } from "zod";

import { BackOfficeSideMenu } from "@/shared/components/BackOfficeSideMenu";
import { api, BillingEventType, SortableBillingEventProperties, SortOrder } from "@/shared/lib/api/client";

import { BillingEventsTable } from "./-components/BillingEventsTable";
import { BillingEventsToolbar } from "./-components/BillingEventsToolbar";

const billingEventsSearchSchema = z.object({
  search: z.string().optional(),
  eventTypes: z.array(z.nativeEnum(BillingEventType)).max(25).optional(),
  orderBy: z.nativeEnum(SortableBillingEventProperties).optional(),
  sortOrder: z.nativeEnum(SortOrder).optional(),
  pageOffset: z.number().int().nonnegative().optional()
});

export const Route = createFileRoute("/billing-events/")({
  staticData: { trackingTitle: "Billing events" },
  validateSearch: billingEventsSearchSchema,
  component: BillingEventsListPage
});

function BillingEventsListPage() {
  const { search, eventTypes, orderBy, sortOrder, pageOffset } = Route.useSearch();
  const navigate = useNavigate();

  const { data, isLoading } = api.useQuery(
    "get",
    "/api/back-office/billing-events",
    {
      params: {
        query: {
          Search: search,
          EventTypes: eventTypes,
          OrderBy: orderBy,
          SortOrder: sortOrder,
          PageOffset: pageOffset
        }
      }
    },
    { placeholderData: keepPreviousData }
  );

  const billingEvents = data?.billingEvents ?? [];
  const hasFilters = Boolean(search) || (eventTypes?.length ?? 0) > 0;
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
          <BillingEventsToolbar search={search} eventTypes={eventTypes ?? []} />

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

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Button } from "@repo/ui/components/Button";
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@repo/ui/components/Empty";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { keepPreviousData } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { Building2Icon } from "lucide-react";
import { useCallback, useState } from "react";
import { z } from "zod";

import type { components } from "@/shared/lib/api/client";

import { BackOfficeSideMenu } from "@/shared/components/BackOfficeSideMenu";
import {
  api,
  SortableTenantProperties,
  SortOrder,
  SubscriptionPlan,
  TenantStatusFilter
} from "@/shared/lib/api/client";

import { AccountSidePane } from "./-components/AccountSidePane";
import { AccountsTable } from "./-components/AccountsTable";
import { AccountsToolbar } from "./-components/AccountsToolbar";

type TenantSummary = components["schemas"]["TenantSummary"];

const accountsSearchSchema = z.object({
  search: z.string().optional(),
  plans: z.array(z.nativeEnum(SubscriptionPlan)).max(10).optional(),
  statuses: z.array(z.nativeEnum(TenantStatusFilter)).max(10).optional(),
  unsynced: z.boolean().optional(),
  driftDetected: z.boolean().optional(),
  orderBy: z.nativeEnum(SortableTenantProperties).optional(),
  sortOrder: z.nativeEnum(SortOrder).optional(),
  pageOffset: z.number().int().nonnegative().optional()
});

export const Route = createFileRoute("/accounts/")({
  staticData: { trackingTitle: "Accounts" },
  validateSearch: accountsSearchSchema,
  component: AccountsListPage
});

function AccountsListPage() {
  const { search, plans, statuses, unsynced, driftDetected, orderBy, sortOrder, pageOffset } = Route.useSearch();
  const navigate = useNavigate();
  const [previewTenant, setPreviewTenant] = useState<TenantSummary | null>(null);

  const { data, isLoading } = api.useQuery(
    "get",
    "/api/back-office/tenants",
    {
      params: {
        query: {
          Search: search,
          Plans: plans,
          Statuses: statuses,
          Unsynced: unsynced,
          DriftDetected: driftDetected,
          OrderBy: orderBy,
          SortOrder: sortOrder,
          PageOffset: pageOffset
        }
      }
    },
    { placeholderData: keepPreviousData }
  );

  const handleSelectTenant = useCallback((tenant: TenantSummary | null) => {
    setPreviewTenant(tenant);
  }, []);

  const handleClosePane = useCallback(() => setPreviewTenant(null), []);

  const tenants = data?.tenants ?? [];
  const hasFilters =
    Boolean(search) ||
    (plans?.length ?? 0) > 0 ||
    (statuses?.length ?? 0) > 0 ||
    Boolean(unsynced) ||
    Boolean(driftDetected);
  const showEmpty = !isLoading && tenants.length === 0;

  return (
    <SidebarProvider>
      <BackOfficeSideMenu />
      <SidebarInset>
        <AppLayout
          variant="center"
          maxWidth="64rem"
          browserTitle={t`Accounts`}
          title={t`Accounts`}
          subtitle={t`Search, filter, and review accounts.`}
          sidePane={
            previewTenant ? (
              <AccountSidePane tenant={previewTenant} isOpen={previewTenant !== null} onClose={handleClosePane} />
            ) : undefined
          }
        >
          <AccountsToolbar
            search={search}
            plans={plans ?? []}
            statuses={statuses ?? []}
            unsynced={unsynced ?? false}
            driftDetected={driftDetected ?? false}
          />

          {showEmpty ? (
            <Empty>
              <EmptyHeader>
                <EmptyMedia variant="icon">
                  <Building2Icon />
                </EmptyMedia>
                <EmptyTitle>
                  {hasFilters ? <Trans>No accounts match your filters</Trans> : <Trans>No accounts yet</Trans>}
                </EmptyTitle>
                <EmptyDescription>
                  {hasFilters ? (
                    <Trans>Try clearing the search or filters to see more results.</Trans>
                  ) : (
                    <Trans>Accounts will appear here as they are created.</Trans>
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
                        to: "/accounts",
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
              <AccountsTable
                tenants={tenants}
                isLoading={isLoading}
                totalPages={data?.totalPages ?? 0}
                currentPageOffset={data?.currentPageOffset ?? 0}
                selectedTenantId={previewTenant?.id}
                onSelectTenant={handleSelectTenant}
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

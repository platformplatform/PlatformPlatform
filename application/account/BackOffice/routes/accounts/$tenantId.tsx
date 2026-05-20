import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@repo/ui/components/Tabs";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { ActivityIcon, FlagIcon, LayoutGridIcon, LifeBuoyIcon, ReceiptIcon, UsersIcon } from "lucide-react";
import { useCallback } from "react";
import { z } from "zod";

import { BackOfficeSideMenu } from "@/shared/components/BackOfficeSideMenu";
import { api } from "@/shared/lib/api/client";

import { AccountBillingTab } from "./-components/AccountBillingTab";
import { AccountCurrentPlanCard } from "./-components/AccountCurrentPlanCard";
import { AccountDetailHeader } from "./-components/AccountDetailHeader";
import { AccountFeatureFlagsTab } from "./-components/AccountFeatureFlagsTab";
import { AccountHealthTiles } from "./-components/AccountHealthTiles";
import { AccountOpenSupportTicketsCard } from "./-components/AccountOpenSupportTicketsCard";
import { AccountOverviewTab } from "./-components/AccountOverviewTab";
import { AccountUsersTab } from "./-components/AccountUsersTab";
import { TenantSupportTicketsSection } from "./-components/TenantSupportTicketsSection";

type AccountDetailTab = "overview" | "users" | "invoices" | "billing-events" | "feature-flags" | "support-tickets";

const isSubscriptionEnabled = import.meta.runtime_env.PUBLIC_SUBSCRIPTION_ENABLED === "true";
const isSupportSystemEnabled = import.meta.runtime_env.PUBLIC_SUPPORT_SYSTEM_ENABLED === "true";

const accountDetailSearchSchema = z.object({
  tab: z.enum(["overview", "users", "invoices", "billing-events", "feature-flags", "support-tickets"]).optional()
});

export const Route = createFileRoute("/accounts/$tenantId")({
  staticData: { trackingTitle: "Account detail" },
  validateSearch: accountDetailSearchSchema,
  component: AccountDetailPage
});

function AccountDetailPage() {
  const { tenantId } = Route.useParams();
  const { tab } = Route.useSearch();
  const navigate = useNavigate({ from: Route.fullPath });
  const isBillingTab = tab === "invoices" || tab === "billing-events";
  const activeTab = !isSubscriptionEnabled && isBillingTab ? "overview" : (tab ?? "overview");

  const setActiveTab = useCallback(
    (value: string) => {
      const next = value as AccountDetailTab;
      navigate({
        search: { tab: next === "overview" ? undefined : next },
        replace: true
      });
    },
    [navigate]
  );

  const tenantQuery = api.useQuery("get", "/api/back-office/tenants/{id}", {
    params: { path: { id: tenantId } }
  });

  const tenant = tenantQuery.data;

  return (
    <SidebarProvider>
      <BackOfficeSideMenu />
      <SidebarInset>
        <AppLayout variant="center" maxWidth="64rem" browserTitle={tenant?.name ?? t`Account`}>
          <div className="flex flex-col gap-6">
            <AccountDetailHeader tenant={tenant} tenantId={tenantId} isLoading={tenantQuery.isLoading} />
            <AccountHealthTiles tenant={tenant} tenantId={tenantId} isLoading={tenantQuery.isLoading} />
            <Tabs value={activeTab} onValueChange={setActiveTab}>
              <TabsList>
                <TabsTrigger value="overview">
                  <LayoutGridIcon className="size-4" aria-hidden={true} />
                  <Trans>Overview</Trans>
                </TabsTrigger>
                <TabsTrigger value="users">
                  <UsersIcon className="size-4" aria-hidden={true} />
                  <Trans>Users</Trans>
                </TabsTrigger>
                {isSubscriptionEnabled && (
                  <TabsTrigger value="invoices">
                    <ReceiptIcon className="size-4" aria-hidden={true} />
                    <Trans>Invoices</Trans>
                  </TabsTrigger>
                )}
                {isSubscriptionEnabled && (
                  <TabsTrigger value="billing-events">
                    <ActivityIcon className="size-4" aria-hidden={true} />
                    <Trans>Billing events</Trans>
                  </TabsTrigger>
                )}
                <TabsTrigger value="feature-flags">
                  <FlagIcon className="size-4" aria-hidden={true} />
                  <Trans>Feature flags</Trans>
                </TabsTrigger>
                {isSupportSystemEnabled && (
                  <TabsTrigger value="support-tickets">
                    <LifeBuoyIcon className="size-4" aria-hidden={true} />
                    <Trans>Support tickets</Trans>
                  </TabsTrigger>
                )}
              </TabsList>
              <TabsContent value="overview" className="flex flex-col gap-6">
                <AccountOverviewTab tenant={tenant} tenantId={tenantId} isLoading={tenantQuery.isLoading} />
                {isSupportSystemEnabled && <AccountOpenSupportTicketsCard tenantId={tenantId} />}
                {isSubscriptionEnabled && (
                  <div className="grid grid-cols-1 gap-6 lg:grid-cols-5">
                    <div className="flex flex-col lg:col-span-2">
                      <AccountCurrentPlanCard tenant={tenant} isLoading={tenantQuery.isLoading} />
                    </div>
                    <div className="flex flex-col lg:col-span-3">
                      <AccountBillingTab
                        tenantId={tenantId}
                        variant="compact-both"
                        onViewAllInvoices={() => setActiveTab("invoices")}
                        onViewAllEvents={() => setActiveTab("billing-events")}
                      />
                    </div>
                  </div>
                )}
              </TabsContent>
              <TabsContent value="users">
                <AccountUsersTab tenantId={tenantId} />
              </TabsContent>
              {isSubscriptionEnabled && (
                <TabsContent value="invoices">
                  <AccountBillingTab tenantId={tenantId} variant="history-full" />
                </TabsContent>
              )}
              {isSubscriptionEnabled && (
                <TabsContent value="billing-events">
                  <AccountBillingTab tenantId={tenantId} variant="events-full" />
                </TabsContent>
              )}
              <TabsContent value="feature-flags">
                <AccountFeatureFlagsTab tenantId={tenantId} />
              </TabsContent>
              {isSupportSystemEnabled && (
                <TabsContent value="support-tickets">
                  <TenantSupportTicketsSection tenantId={tenantId} />
                </TabsContent>
              )}
            </Tabs>
          </div>
        </AppLayout>
      </SidebarInset>
    </SidebarProvider>
  );
}

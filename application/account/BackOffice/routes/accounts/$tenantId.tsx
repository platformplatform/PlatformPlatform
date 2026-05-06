import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@repo/ui/components/Tabs";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { LayoutGridIcon, ReceiptIcon, UsersIcon } from "lucide-react";
import { useCallback } from "react";
import { z } from "zod";

import { BackOfficeSideMenu } from "@/shared/components/BackOfficeSideMenu";
import { api } from "@/shared/lib/api/client";

import { AccountBillingTab } from "./-components/AccountBillingTab";
import { AccountCurrentPlanCard } from "./-components/AccountCurrentPlanCard";
import { AccountDetailHeader } from "./-components/AccountDetailHeader";
import { AccountKpiCards } from "./-components/AccountKpiCards";
import { AccountOverviewTab } from "./-components/AccountOverviewTab";
import { AccountUsersTab } from "./-components/AccountUsersTab";

type AccountDetailTab = "overview" | "users" | "billing";

const accountDetailSearchSchema = z.object({
  tab: z.enum(["overview", "users", "billing"]).optional()
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
  const activeTab = tab ?? "overview";

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
  const userCountsQuery = api.useQuery("get", "/api/back-office/tenants/{id}/user-counts", {
    params: { path: { id: tenantId } }
  });

  const tenant = tenantQuery.data;
  const totalUsers = userCountsQuery.data?.totalUsers;

  return (
    <SidebarProvider>
      <BackOfficeSideMenu />
      <SidebarInset>
        <AppLayout variant="center" maxWidth="64rem" browserTitle={tenant?.name ?? t`Account`}>
          <div className="flex flex-col gap-6">
            <AccountDetailHeader tenant={tenant} tenantId={tenantId} isLoading={tenantQuery.isLoading} />
            <AccountKpiCards tenant={tenant} tenantId={tenantId} isLoading={tenantQuery.isLoading} />
            <Tabs value={activeTab} onValueChange={setActiveTab}>
              <TabsList>
                <TabsTrigger value="overview">
                  <LayoutGridIcon className="size-4" />
                  <Trans>Overview</Trans>
                </TabsTrigger>
                <TabsTrigger value="users">
                  <UsersIcon className="size-4" />
                  {totalUsers === undefined ? <Trans>Users</Trans> : <Trans>Users ({totalUsers})</Trans>}
                </TabsTrigger>
                <TabsTrigger value="billing">
                  <ReceiptIcon className="size-4" />
                  <Trans>Billing</Trans>
                </TabsTrigger>
              </TabsList>
              <TabsContent value="overview" className="flex flex-col gap-6">
                <AccountOverviewTab tenant={tenant} tenantId={tenantId} isLoading={tenantQuery.isLoading} />
                <div className="grid grid-cols-1 gap-6 lg:grid-cols-5">
                  <div className="lg:col-span-2">
                    <AccountCurrentPlanCard tenant={tenant} isLoading={tenantQuery.isLoading} />
                  </div>
                  <div className="lg:col-span-3">
                    <AccountBillingTab
                      tenantId={tenantId}
                      variant="compact"
                      onViewAll={() => setActiveTab("billing")}
                    />
                  </div>
                </div>
              </TabsContent>
              <TabsContent value="users">
                <AccountUsersTab tenantId={tenantId} />
              </TabsContent>
              <TabsContent value="billing">
                <AccountBillingTab tenantId={tenantId} variant="full" />
              </TabsContent>
            </Tabs>
          </div>
        </AppLayout>
      </SidebarInset>
    </SidebarProvider>
  );
}

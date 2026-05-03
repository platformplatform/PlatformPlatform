import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@repo/ui/components/Tabs";
import { createFileRoute } from "@tanstack/react-router";
import { z } from "zod";

import { BackOfficeSideMenu } from "@/shared/components/BackOfficeSideMenu";
import { api } from "@/shared/lib/api/client";

import { AccountBillingTab } from "./-components/AccountBillingTab";
import { AccountDetailHeader } from "./-components/AccountDetailHeader";
import { AccountOverviewTab } from "./-components/AccountOverviewTab";
import { AccountUsersTab } from "./-components/AccountUsersTab";

const detailSearchSchema = z.object({
  tab: z.enum(["overview", "users", "billing"]).optional()
});

export const Route = createFileRoute("/accounts/$tenantId")({
  staticData: { trackingTitle: "Account detail" },
  validateSearch: detailSearchSchema,
  component: AccountDetailPage
});

function AccountDetailPage() {
  const { tenantId } = Route.useParams();
  const { tab } = Route.useSearch();
  const navigate = Route.useNavigate();

  const tenantQuery = api.useQuery("get", "/api/back-office/tenants/{id}", {
    params: { path: { id: tenantId } }
  });

  const userCountsQuery = api.useQuery("get", "/api/back-office/tenants/{id}/user-counts", {
    params: { path: { id: tenantId } }
  });

  const activeTab = tab ?? "overview";
  const tenant = tenantQuery.data;

  return (
    <SidebarProvider>
      <BackOfficeSideMenu />
      <SidebarInset>
        <AppLayout variant="center" maxWidth="64rem" browserTitle={tenant?.name ?? t`Account`}>
          <div className="flex flex-col gap-6">
            <AccountDetailHeader
              tenant={tenant}
              isLoading={tenantQuery.isLoading}
              userCounts={userCountsQuery.data}
              isLoadingUserCounts={userCountsQuery.isLoading}
            />

            <Tabs
              value={activeTab}
              onValueChange={(value) =>
                navigate({
                  to: "/accounts/$tenantId",
                  params: { tenantId },
                  search: { tab: value === "overview" ? undefined : (value as "users" | "billing") }
                })
              }
            >
              <TabsList aria-label={t`Account sections`}>
                <TabsTrigger value="overview">
                  <Trans>Overview</Trans>
                </TabsTrigger>
                <TabsTrigger value="users">
                  <Trans>Users</Trans>
                </TabsTrigger>
                <TabsTrigger value="billing">
                  <Trans>Billing & invoices</Trans>
                </TabsTrigger>
              </TabsList>

              <TabsContent value="overview" className="mt-4">
                <AccountOverviewTab tenant={tenant} tenantId={tenantId} isLoading={tenantQuery.isLoading} />
              </TabsContent>
              <TabsContent value="users" className="mt-4">
                <AccountUsersTab tenantId={tenantId} />
              </TabsContent>
              <TabsContent value="billing" className="mt-4">
                <AccountBillingTab tenant={tenant} tenantId={tenantId} />
              </TabsContent>
            </Tabs>
          </div>
        </AppLayout>
      </SidebarInset>
    </SidebarProvider>
  );
}

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@repo/ui/components/Tabs";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { Building2Icon, FlagIcon, KeyIcon, LifeBuoyIcon, MonitorIcon } from "lucide-react";
import { useCallback } from "react";
import { z } from "zod";

import { BackOfficeSideMenu } from "@/shared/components/BackOfficeSideMenu";
import { api } from "@/shared/lib/api/client";

import { UserActivityTiles } from "./-components/UserActivityTiles";
import { UserDetailHeader } from "./-components/UserDetailHeader";
import { getUserDisplayName } from "./-components/userDisplay";
import { UserFeatureFlagsSection } from "./-components/UserFeatureFlagsSection";
import { UserLoginHistorySection } from "./-components/UserLoginHistorySection";
import { UserSessionsSection } from "./-components/UserSessionsSection";
import { UserSupportTicketsSection } from "./-components/UserSupportTicketsSection";
import { UserTenantsSection } from "./-components/UserTenantsSection";

const isSupportSystemEnabled = import.meta.runtime_env.PUBLIC_SUPPORT_SYSTEM_ENABLED === "true";

type UserDetailTab = "overview" | "sessions" | "logins" | "feature-flags" | "support-tickets";

const userDetailSearchSchema = z.object({
  tab: z.enum(["overview", "sessions", "logins", "feature-flags", "support-tickets"]).optional()
});

export const Route = createFileRoute("/users/$userId")({
  staticData: { trackingTitle: "User detail" },
  validateSearch: userDetailSearchSchema,
  component: UserDetailPage
});

function UserDetailPage() {
  const { userId } = Route.useParams();
  const { tab } = Route.useSearch();
  const navigate = useNavigate({ from: Route.fullPath });
  const activeTab = tab ?? "overview";

  const setActiveTab = useCallback(
    (value: string) => {
      const next = value as UserDetailTab;
      navigate({
        search: { tab: next === "overview" ? undefined : next },
        replace: true
      });
    },
    [navigate]
  );

  const userQuery = api.useQuery("get", "/api/back-office/users/{id}", {
    params: { path: { id: userId } }
  });

  const user = userQuery.data;

  const browserTitle = user ? getUserDisplayName(user.firstName, user.lastName, user.email) : t`User detail`;

  return (
    <SidebarProvider>
      <BackOfficeSideMenu />
      <SidebarInset>
        <AppLayout variant="center" maxWidth="64rem" browserTitle={browserTitle}>
          <div className="flex flex-col gap-6">
            <UserDetailHeader user={user} userId={userId} isLoading={userQuery.isLoading} />
            <UserActivityTiles user={user} userId={userId} isLoading={userQuery.isLoading} />
            <Tabs value={activeTab} onValueChange={setActiveTab}>
              <TabsList>
                <TabsTrigger value="overview">
                  <Building2Icon className="size-4" />
                  <Trans>Accounts</Trans>
                </TabsTrigger>
                <TabsTrigger value="logins">
                  <KeyIcon className="size-4" />
                  <Trans>Logins</Trans>
                </TabsTrigger>
                <TabsTrigger value="sessions">
                  <MonitorIcon className="size-4" />
                  <Trans>Sessions</Trans>
                </TabsTrigger>
                <TabsTrigger value="feature-flags">
                  <FlagIcon className="size-4" />
                  <Trans>Feature flags</Trans>
                </TabsTrigger>
                {isSupportSystemEnabled && (
                  <TabsTrigger value="support-tickets">
                    <LifeBuoyIcon className="size-4" aria-hidden={true} />
                    <Trans>Support tickets</Trans>
                  </TabsTrigger>
                )}
              </TabsList>
              <TabsContent value="overview">
                <UserTenantsSection user={user} />
              </TabsContent>
              <TabsContent value="logins">
                <UserLoginHistorySection userId={userId} />
              </TabsContent>
              <TabsContent value="sessions">
                <UserSessionsSection userId={userId} />
              </TabsContent>
              <TabsContent value="feature-flags">
                <UserFeatureFlagsSection userId={userId} />
              </TabsContent>
              {isSupportSystemEnabled && (
                <TabsContent value="support-tickets">
                  <UserSupportTicketsSection userId={userId} />
                </TabsContent>
              )}
            </Tabs>
          </div>
        </AppLayout>
      </SidebarInset>
    </SidebarProvider>
  );
}

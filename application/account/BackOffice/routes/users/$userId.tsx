import { t } from "@lingui/core/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { createFileRoute } from "@tanstack/react-router";

import { BackOfficeSideMenu } from "@/shared/components/BackOfficeSideMenu";
import { api } from "@/shared/lib/api/client";

import { UserDetailHeader } from "./-components/UserDetailHeader";
import { UserDetailSections } from "./-components/UserDetailSections";
import { getUserDisplayName } from "./-components/userDisplay";

export const Route = createFileRoute("/users/$userId")({
  staticData: { trackingTitle: "User detail" },
  component: UserDetailPage
});

function UserDetailPage() {
  const { userId } = Route.useParams();

  const { data: user, isLoading } = api.useQuery("get", "/api/back-office/users/{id}", {
    params: { path: { id: userId } }
  });

  const browserTitle = user ? getUserDisplayName(user.firstName, user.lastName, user.email) : t`User detail`;

  return (
    <SidebarProvider>
      <BackOfficeSideMenu />
      <SidebarInset>
        <AppLayout variant="center" maxWidth="64rem" browserTitle={browserTitle}>
          <UserDetailHeader user={user} isLoading={isLoading} />
          <UserDetailSections userId={userId} user={user} isLoading={isLoading} />
        </AppLayout>
      </SidebarInset>
    </SidebarProvider>
  );
}

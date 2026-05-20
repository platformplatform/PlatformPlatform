import { requireAuthentication, requireSupportSystemEnabled } from "@repo/infrastructure/auth/routeGuards";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { createFileRoute, Outlet } from "@tanstack/react-router";

import SuspendedPage from "@/federated-modules/subscription/SuspendedPage";
import { AccountSideMenu } from "@/shared/components/AccountSideMenu";
import { api, TenantState } from "@/shared/lib/api/client";

export const Route = createFileRoute("/support")({
  beforeLoad: () => {
    requireSupportSystemEnabled();
    requireAuthentication();
  },
  component: SupportLayout
});

function SupportLayout() {
  const { data: tenant } = api.useQuery("get", "/api/account/tenants/current");

  if (tenant?.state === TenantState.Suspended) {
    return <SuspendedPage />;
  }

  return (
    <SidebarProvider>
      <AccountSideMenu />
      <SidebarInset>
        <Outlet />
      </SidebarInset>
    </SidebarProvider>
  );
}

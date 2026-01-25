import { requireAuthentication } from "@repo/infrastructure/auth/routeGuards";
import { createFileRoute, Outlet, useLocation } from "@tanstack/react-router";
import FederatedNotFoundPage from "@/federated-modules/errorPages/FederatedNotFoundPage";
import { PastDueBanner } from "@/shared/components/PastDueBanner";
import { SuspendedPage } from "@/shared/components/SuspendedPage";
import { api, TenantState } from "@/shared/lib/api/client";

export const Route = createFileRoute("/admin")({
  beforeLoad: () => requireAuthentication(),
  component: AdminLayout,
  notFoundComponent: FederatedNotFoundPage
});

function AdminLayout() {
  const { data: tenant } = api.useQuery("get", "/api/account/tenants/current");
  const location = useLocation();
  const isSubscriptionPage = location.pathname.startsWith("/admin/subscription");

  if (tenant?.state === TenantState.Suspended && !isSubscriptionPage) {
    return <SuspendedPage />;
  }

  return (
    <>
      <PastDueBanner />
      <Outlet />
    </>
  );
}

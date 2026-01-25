import { requireAuthentication } from "@repo/infrastructure/auth/routeGuards";
import { createFileRoute, Outlet, useLocation } from "@tanstack/react-router";
import Banners from "@/federated-modules/banners/Banners";
import FederatedNotFoundPage from "@/federated-modules/errorPages/FederatedNotFoundPage";
import SuspendedPage from "@/federated-modules/subscription/SuspendedPage";
import { api, TenantState } from "@/shared/lib/api/client";

export const Route = createFileRoute("/account")({
  beforeLoad: () => requireAuthentication(),
  component: AccountLayout,
  notFoundComponent: FederatedNotFoundPage
});

function AccountLayout() {
  const { data: tenant } = api.useQuery("get", "/api/account/tenants/current");
  const location = useLocation();
  const isSubscriptionPage = location.pathname.startsWith("/account/subscription");

  if (tenant?.state === TenantState.Suspended && !isSubscriptionPage) {
    return <SuspendedPage />;
  }

  return (
    <>
      <Banners />
      <Outlet />
    </>
  );
}

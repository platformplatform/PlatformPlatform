import { requireAuthentication } from "@repo/infrastructure/auth/routeGuards";
import { createFileRoute, Outlet, useLocation } from "@tanstack/react-router";
import SuspendedPage from "@/federated-modules/subscription/SuspendedPage";
import { AccountSideMenu } from "@/shared/components/AccountSideMenu";
import { api, TenantState } from "@/shared/lib/api/client";

export const Route = createFileRoute("/account")({
  beforeLoad: () => requireAuthentication(),
  component: AccountLayout
});

function AccountLayout() {
  const { data: tenant } = api.useQuery("get", "/api/account/tenants/current");
  const location = useLocation();
  const isBillingPage = location.pathname.startsWith("/account/billing");

  if (tenant?.state === TenantState.Suspended && !isBillingPage) {
    return <SuspendedPage />;
  }

  return (
    <>
      <AccountSideMenu />
      <Outlet />
    </>
  );
}

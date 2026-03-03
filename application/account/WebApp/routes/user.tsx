import { requireAuthentication } from "@repo/infrastructure/auth/routeGuards";
import { useTenant } from "@repo/infrastructure/sync/hooks";
import { createFileRoute, Outlet, useLocation } from "@tanstack/react-router";
import SuspendedPage from "@/federated-modules/subscription/SuspendedPage";
import { AccountSideMenu } from "@/shared/components/AccountSideMenu";
import { TenantState } from "@/shared/lib/api/client";

export const Route = createFileRoute("/user")({
  beforeLoad: () => requireAuthentication(),
  component: UserLayout
});

function UserLayout() {
  const { tenantId } = import.meta.user_info_env;
  const { data: tenant } = useTenant(tenantId ?? "");
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

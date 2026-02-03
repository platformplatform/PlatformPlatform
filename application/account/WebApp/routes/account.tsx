import { requireAuthentication } from "@repo/infrastructure/auth/routeGuards";
import { createFileRoute, Outlet } from "@tanstack/react-router";
import Banners from "@/federated-modules/banners/Banners";
import FederatedNotFoundPage from "@/federated-modules/errorPages/FederatedNotFoundPage";
import { AccountSideMenu } from "@/shared/components/AccountSideMenu";

export const Route = createFileRoute("/account")({
  beforeLoad: () => requireAuthentication(),
  component: AccountLayout,
  notFoundComponent: FederatedNotFoundPage
});

function AccountLayout() {
  return (
    <>
      <Banners />
      <AccountSideMenu />
      <Outlet />
    </>
  );
}

import { requireAuthentication } from "@repo/infrastructure/auth/routeGuards";
import { createFileRoute, Outlet } from "@tanstack/react-router";
import { AccountSideMenu } from "@/shared/components/AccountSideMenu";

export const Route = createFileRoute("/account")({
  beforeLoad: () => requireAuthentication(),
  component: AccountLayout
});

function AccountLayout() {
  return (
    <>
      <AccountSideMenu />
      <Outlet />
    </>
  );
}

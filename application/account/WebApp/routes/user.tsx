import { requireAuthentication } from "@repo/infrastructure/auth/routeGuards";
import { createFileRoute, Outlet } from "@tanstack/react-router";
import { AccountSideMenu } from "@/shared/components/AccountSideMenu";

export const Route = createFileRoute("/user")({
  beforeLoad: () => requireAuthentication(),
  component: UserLayout
});

function UserLayout() {
  return (
    <>
      <AccountSideMenu />
      <Outlet />
    </>
  );
}

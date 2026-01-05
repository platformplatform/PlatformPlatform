import { requireAuthentication } from "@repo/infrastructure/auth/routeGuards";
import { createFileRoute, Outlet } from "@tanstack/react-router";

export const Route = createFileRoute("/admin")({
  beforeLoad: () => requireAuthentication(),
  component: AdminLayout
});

function AdminLayout() {
  return <Outlet />;
}

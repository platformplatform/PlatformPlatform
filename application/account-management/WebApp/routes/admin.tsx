import { requireAuthentication } from "@repo/infrastructure/auth/routeGuards";
import { createFileRoute, Outlet } from "@tanstack/react-router";
import FederatedNotFoundPage from "@/federated-modules/errorPages/FederatedNotFoundPage";

export const Route = createFileRoute("/admin")({
  beforeLoad: () => requireAuthentication(),
  component: AdminLayout,
  notFoundComponent: FederatedNotFoundPage
});

function AdminLayout() {
  return <Outlet />;
}

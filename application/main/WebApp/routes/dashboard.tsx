import { requireAuthentication } from "@repo/infrastructure/auth/routeGuards";
import { createFileRoute, Outlet } from "@tanstack/react-router";
import { lazy, Suspense } from "react";

const Banners = lazy(() => import("account/Banners"));
const FederatedNotFoundPage = lazy(() => import("account/FederatedNotFoundPage"));

export const Route = createFileRoute("/dashboard")({
  beforeLoad: () => requireAuthentication(),
  component: DashboardLayout,
  notFoundComponent: FederatedNotFoundPage
});

function DashboardLayout() {
  return (
    <Suspense fallback={null}>
      <Banners />
      <Outlet />
    </Suspense>
  );
}

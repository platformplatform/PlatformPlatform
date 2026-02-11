import { requireAuthentication } from "@repo/infrastructure/auth/routeGuards";
import { createFileRoute, Outlet, useLocation } from "@tanstack/react-router";
import { lazy, Suspense } from "react";

const NotFoundPage = lazy(() => import("account/NotFoundPage"));
const TenantStateGuard = lazy(() => import("account/TenantStateGuard"));

export const Route = createFileRoute("/dashboard")({
  beforeLoad: () => requireAuthentication(),
  component: DashboardLayout,
  notFoundComponent: NotFoundPage
});

function DashboardLayout() {
  const location = useLocation();

  return (
    <Suspense fallback={null}>
      <TenantStateGuard pathname={location.pathname}>
        <Outlet />
      </TenantStateGuard>
    </Suspense>
  );
}

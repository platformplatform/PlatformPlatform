import { requireAuthentication } from "@repo/infrastructure/auth/routeGuards";
import { createFileRoute, Outlet } from "@tanstack/react-router";
import { lazy, Suspense } from "react";

const NotFoundPage = lazy(() => import("account/NotFoundPage"));

export const Route = createFileRoute("/dashboard")({
  beforeLoad: () => requireAuthentication(),
  component: DashboardLayout,
  notFoundComponent: NotFoundPage
});

function DashboardLayout() {
  return (
    <Suspense fallback={null}>
      <Outlet />
    </Suspense>
  );
}

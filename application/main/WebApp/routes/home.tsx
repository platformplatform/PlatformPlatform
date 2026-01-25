import { requireAuthentication } from "@repo/infrastructure/auth/routeGuards";
import { createFileRoute, Outlet, useLocation } from "@tanstack/react-router";
import { lazy, Suspense } from "react";

const Banners = lazy(() => import("account/Banners"));
const FederatedNotFoundPage = lazy(() => import("account/FederatedNotFoundPage"));
const TenantStateGuard = lazy(() => import("account/TenantStateGuard"));

export const Route = createFileRoute("/home")({
  beforeLoad: () => requireAuthentication(),
  component: HomeLayout,
  notFoundComponent: FederatedNotFoundPage
});

function HomeLayout() {
  const location = useLocation();

  return (
    <Suspense fallback={null}>
      <Banners />
      <TenantStateGuard pathname={location.pathname}>
        <Outlet />
      </TenantStateGuard>
    </Suspense>
  );
}

import { hasPermission, requireAuthentication } from "@repo/infrastructure/auth/routeGuards";
import { createFileRoute, Outlet } from "@tanstack/react-router";
import { lazy, Suspense } from "react";

const FederatedAccessDeniedPage = lazy(() => import("account-management/FederatedAccessDeniedPage"));
const FederatedNotFoundPage = lazy(() => import("account-management/FederatedNotFoundPage"));

export const Route = createFileRoute("/back-office")({
  beforeLoad: () => requireAuthentication(),
  component: BackOfficeLayout,
  notFoundComponent: FederatedNotFoundPage
});

function BackOfficeLayout() {
  if (!hasPermission({ requiresInternalUser: true })) {
    return (
      <Suspense fallback={null}>
        <FederatedAccessDeniedPage />
      </Suspense>
    );
  }
  return <Outlet />;
}

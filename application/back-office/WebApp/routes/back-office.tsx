import { hasPermission, requireAuthentication } from "@repo/infrastructure/auth/routeGuards";
import { createFileRoute, Outlet } from "@tanstack/react-router";
import { AccessDeniedPage, NotFoundPage } from "@/shared/components/errorPages";
import { BackOfficeSideMenu } from "@/shared/components/sideMenu";

export const Route = createFileRoute("/back-office")({
  beforeLoad: () => requireAuthentication(),
  component: BackOfficeLayout,
  notFoundComponent: NotFoundPage
});

function BackOfficeLayout() {
  if (!hasPermission({ requiresInternalUser: true })) {
    return <AccessDeniedPage />;
  }
  return (
    <>
      <BackOfficeSideMenu />
      <Outlet />
    </>
  );
}

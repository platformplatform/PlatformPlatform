import { hasPermission, requireAuthentication } from "@repo/infrastructure/auth/routeGuards";
import { createFileRoute, Outlet } from "@tanstack/react-router";
import { BackOfficeSideMenu } from "@/shared/components/BackOfficeSideMenu";
import { AccessDeniedPage } from "@/shared/components/errorPages/AccessDeniedPage";

export const Route = createFileRoute("/back-office")({
  beforeLoad: () => requireAuthentication(),
  component: BackOfficeLayout
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

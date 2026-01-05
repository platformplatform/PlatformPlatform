import { loginPath } from "@repo/infrastructure/auth/constants";
import { createFileRoute, Outlet } from "@tanstack/react-router";

export const Route = createFileRoute("/back-office")({
  beforeLoad: () => {
    const isAuthenticated = import.meta.user_info_env.isAuthenticated;
    if (!isAuthenticated) {
      const returnPath = encodeURIComponent(window.location.pathname);
      window.location.href = `${loginPath}?returnPath=${returnPath}`;
      return;
    }
  },
  component: BackOfficeLayout
});

function BackOfficeLayout() {
  return <Outlet />;
}

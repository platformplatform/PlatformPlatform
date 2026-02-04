import { createFileRoute, useLocation, useNavigate } from "@tanstack/react-router";
import { lazy, Suspense } from "react";

const AccountApp = lazy(() => import("account/AccountApp"));
const NotFoundPage = lazy(() => import("account/NotFoundPage"));

const ACCOUNT_PREFIXES = ["/login", "/signup", "/account", "/profile", "/legal", "/error"];

export const Route = createFileRoute("/$")({
  component: CatchAll
});

function CatchAll() {
  const location = useLocation();
  const navigate = useNavigate();

  const isAccountRoute = ACCOUNT_PREFIXES.some((prefix) => location.pathname.startsWith(prefix));

  const handleNavigateToMain = (path: string) => {
    navigate({ to: path });
  };

  if (isAccountRoute) {
    const searchString = location.searchStr || "";
    return (
      <Suspense fallback={null}>
        <AccountApp initialPath={location.pathname + searchString} onNavigateToMain={handleNavigateToMain} />
      </Suspense>
    );
  }

  return (
    <Suspense fallback={null}>
      <NotFoundPage />
    </Suspense>
  );
}

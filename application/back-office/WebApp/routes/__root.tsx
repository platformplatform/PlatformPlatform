import { PageTracker } from "@repo/infrastructure/applicationInsights/PageTracker";
import { AuthenticationProvider } from "@repo/infrastructure/auth/AuthenticationProvider";
import { AuthSyncModal } from "@repo/infrastructure/auth/AuthSyncModal";
import { useErrorTrigger } from "@repo/infrastructure/development/useErrorTrigger";
import { ReactAriaRouterProvider } from "@repo/infrastructure/router/ReactAriaRouterProvider";
import { useInitializeLocale } from "@repo/infrastructure/translations/useInitializeLocale";
import { AddToHomescreen } from "@repo/ui/components/AddToHomescreen";
import { ThemeModeProvider } from "@repo/ui/theme/mode/ThemeMode";
import { QueryClientProvider } from "@tanstack/react-query";
import { createRootRoute, Outlet, useNavigate } from "@tanstack/react-router";
import { lazy } from "react";
import { queryClient } from "@/shared/lib/api/client";

const FederatedAuthSyncModal = lazy(() => import("account-management/AuthSyncModal"));
const FederatedErrorPage = lazy(() => import("account-management/FederatedErrorPage"));
const FederatedNotFoundPage = lazy(() => import("account-management/FederatedNotFoundPage"));

export const Route = createRootRoute({
  component: Root,
  errorComponent: FederatedErrorPage,
  notFoundComponent: FederatedNotFoundPage
});

function Root() {
  const navigate = useNavigate();
  useInitializeLocale();
  useErrorTrigger();

  return (
    <QueryClientProvider client={queryClient}>
      <ThemeModeProvider>
        <ReactAriaRouterProvider>
          <AuthenticationProvider navigate={(options) => navigate(options)}>
            <AddToHomescreen />
            <PageTracker />
            <Outlet />
            <AuthSyncModal modalComponent={FederatedAuthSyncModal} />
          </AuthenticationProvider>
        </ReactAriaRouterProvider>
      </ThemeModeProvider>
    </QueryClientProvider>
  );
}

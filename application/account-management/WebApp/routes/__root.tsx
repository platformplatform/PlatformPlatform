import { PageTracker } from "@repo/infrastructure/applicationInsights/PageTracker";
import { AuthenticationProvider } from "@repo/infrastructure/auth/AuthenticationProvider";
import { AuthSyncModal } from "@repo/infrastructure/auth/AuthSyncModal";
import { useErrorTrigger } from "@repo/infrastructure/development/useErrorTrigger";
import { useInitializeLocale } from "@repo/infrastructure/translations/useInitializeLocale";
import { AddToHomescreen } from "@repo/ui/components/AddToHomescreen";
import { ThemeModeProvider } from "@repo/ui/theme/mode/ThemeMode";
import { QueryClientProvider } from "@tanstack/react-query";
import { createRootRoute, Outlet, useNavigate } from "@tanstack/react-router";
import AuthSyncModalComponent from "@/federated-modules/common/AuthSyncModal";
import FederatedErrorPage from "@/federated-modules/errorPages/FederatedErrorPage";
import FederatedNotFoundPage from "@/federated-modules/errorPages/FederatedNotFoundPage";
import { queryClient } from "@/shared/lib/api/client";

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
        <AuthenticationProvider navigate={(options) => navigate(options)}>
          <AddToHomescreen />
          <PageTracker />
          <Outlet />
          <AuthSyncModal modalComponent={AuthSyncModalComponent} />
        </AuthenticationProvider>
      </ThemeModeProvider>
    </QueryClientProvider>
  );
}

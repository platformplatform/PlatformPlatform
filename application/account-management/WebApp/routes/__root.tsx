import { queryClient } from "@/shared/lib/api/client";
import { PageTracker } from "@repo/infrastructure/applicationInsights/PageTracker";
import { AuthenticationProvider } from "@repo/infrastructure/auth/AuthenticationProvider";
import { ErrorPage } from "@repo/infrastructure/errorComponents/ErrorPage";
import { NotFound } from "@repo/infrastructure/errorComponents/NotFoundPage";
import { ReactAriaRouterProvider } from "@repo/infrastructure/router/ReactAriaRouterProvider";
import { useInitializeLocale } from "@repo/infrastructure/translations/useInitializeLocale";
import { AddToHomescreen } from "@repo/ui/components/AddToHomescreen";
import { ThemeModeProvider } from "@repo/ui/theme/mode/ThemeMode";
import { QueryClientProvider } from "@tanstack/react-query";
import { Outlet, createRootRoute, useNavigate } from "@tanstack/react-router";

export const Route = createRootRoute({
  component: Root,
  errorComponent: ErrorPage,
  notFoundComponent: NotFound
});

function Root() {
  const navigate = useNavigate();
  useInitializeLocale();

  return (
    <QueryClientProvider client={queryClient}>
      <ThemeModeProvider>
        <ReactAriaRouterProvider>
          <AuthenticationProvider navigate={(options) => navigate(options)}>
            <AddToHomescreen />
            <PageTracker />
            <Outlet />
          </AuthenticationProvider>
        </ReactAriaRouterProvider>
      </ThemeModeProvider>
    </QueryClientProvider>
  );
}

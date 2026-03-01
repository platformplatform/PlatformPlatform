import { PageTracker } from "@repo/infrastructure/applicationInsights/PageTracker";
import { AuthenticationProvider } from "@repo/infrastructure/auth/AuthenticationProvider";
import { useErrorTrigger } from "@repo/infrastructure/development/useErrorTrigger";
import { useInitializeLocale } from "@repo/infrastructure/translations/useInitializeLocale";
import { ThemeModeProvider } from "@repo/ui/theme/mode/ThemeMode";
import { QueryClientProvider } from "@tanstack/react-query";
import { createRootRoute, Outlet, useNavigate } from "@tanstack/react-router";
import { ErrorPage } from "@/shared/components/errorPages/ErrorPage";
import { NotFoundPage } from "@/shared/components/errorPages/NotFoundPage";
import { queryClient } from "@/shared/lib/api/client";

export const Route = createRootRoute({
  component: Root,
  errorComponent: ErrorPage,
  notFoundComponent: NotFoundPage
});

function Root() {
  const navigate = useNavigate();
  useInitializeLocale();
  useErrorTrigger();

  return (
    <QueryClientProvider client={queryClient}>
      <ThemeModeProvider>
        <AuthenticationProvider navigate={(options) => navigate(options)}>
          <PageTracker />
          <Outlet />
        </AuthenticationProvider>
      </ThemeModeProvider>
    </QueryClientProvider>
  );
}

import { createRootRoute, Outlet, useNavigate } from "@tanstack/react-router";
import { ErrorPage } from "@repo/infrastructure/errorComponents/ErrorPage";
import { NotFound } from "@repo/infrastructure/errorComponents/NotFoundPage";
import { AuthenticationProvider } from "@repo/infrastructure/auth/AuthenticationProvider";
import { ReactAriaRouterProvider } from "@repo/infrastructure/router/ReactAriaRouterProvider";
import { ThemeModeProvider } from "@repo/ui/theme/mode/ThemeMode";
import { useInitializeLocale } from "@repo/infrastructure/translations/useInitializeLocale";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

export const Route = createRootRoute({
  component: Root,
  errorComponent: ErrorPage,
  notFoundComponent: NotFound
});

const queryClient = new QueryClient();

function Root() {
  const navigate = useNavigate();
  useInitializeLocale();

  return (
    <QueryClientProvider client={queryClient}>
      <ThemeModeProvider>
        <ReactAriaRouterProvider>
          <AuthenticationProvider navigate={(options) => navigate(options)}>
            <Outlet />
          </AuthenticationProvider>
        </ReactAriaRouterProvider>
      </ThemeModeProvider>
    </QueryClientProvider>
  );
}

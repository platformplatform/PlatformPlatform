import { createRootRoute, Outlet } from "@tanstack/react-router";
import { ErrorPage } from "@repo/infrastructure/errorComponents/ErrorPage";
import { NotFound } from "@repo/infrastructure/errorComponents/NotFoundPage";
import { AuthenticationProvider } from "@repo/infrastructure/auth/AuthenticationProvider";
import { ReactAriaRouterProvider } from "@repo/infrastructure/router/ReactAriaRouterProvider";
import { ThemeModeProvider } from "@repo/ui/theme/mode/ThemeMode";

export const Route = createRootRoute({
  component: Root,
  errorComponent: ErrorPage,
  notFoundComponent: NotFound
});

function Root() {
  return (
    <ThemeModeProvider>
      <ReactAriaRouterProvider>
        <AuthenticationProvider>
          <Outlet />
        </AuthenticationProvider>
      </ReactAriaRouterProvider>
    </ThemeModeProvider>
  );
}

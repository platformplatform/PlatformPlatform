import { createRootRoute, Outlet, useNavigate } from "@tanstack/react-router";
import { ErrorPage } from "@repo/infrastructure/errorComponents/ErrorPage";
import { NotFound } from "@repo/infrastructure/errorComponents/NotFoundPage";
import { AuthenticationProvider } from "@repo/infrastructure/auth/AuthenticationProvider";
import { ReactAriaRouterProvider } from "@repo/infrastructure/router/ReactAriaRouterProvider";
import { ThemeModeProvider } from "@repo/infrastructure/themeMode/useThemeMode";

export const Route = createRootRoute({
  component: Root,
  errorComponent: ErrorPage,
  notFoundComponent: NotFound
});

function Root() {
  const navigate = useNavigate();
  return (
    <ThemeModeProvider>
      <ReactAriaRouterProvider>
        <AuthenticationProvider navigate={(options) => navigate(options)} afterSignIn="/" afterSignOut="/">
          <Outlet />
        </AuthenticationProvider>
      </ReactAriaRouterProvider>
    </ThemeModeProvider>
  );
}

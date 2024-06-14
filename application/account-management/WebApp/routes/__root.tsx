import { Outlet, createRootRoute, useNavigate } from "@tanstack/react-router";
import { ErrorPage } from "./-components/ErrorPage";
import { NotFound } from "./-components/NotFoundPage";
import { ReactAriaRouterProvider } from "@/lib/router/ReactAriaRouterProvider";
import { AuthenticationProvider } from "@/lib/auth/AuthenticationProvider";

export const Route = createRootRoute({
  component: Root,
  errorComponent: ErrorPage,
  notFoundComponent: NotFound,
});

function Root() {
  const navigate = useNavigate();
  return (
    <ReactAriaRouterProvider>
      <AuthenticationProvider navigate={options => navigate(options)} afterSignIn="/admin/users" afterSignOut="/">
        <Outlet />
      </AuthenticationProvider>
    </ReactAriaRouterProvider>
  );
}

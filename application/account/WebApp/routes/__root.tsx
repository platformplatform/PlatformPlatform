import { createRootRoute, Outlet } from "@tanstack/react-router";
import FederatedErrorPage from "@/federated-modules/errorPages/FederatedErrorPage";
import FederatedNotFoundPage from "@/federated-modules/errorPages/FederatedNotFoundPage";

export const Route = createRootRoute({
  component: Root,
  errorComponent: FederatedErrorPage,
  notFoundComponent: FederatedNotFoundPage
});

function Root() {
  return <Outlet />;
}

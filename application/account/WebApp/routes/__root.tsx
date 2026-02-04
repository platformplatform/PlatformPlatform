import { OnboardingGuard } from "@repo/infrastructure/onboarding/OnboardingGuard";
import { createRootRoute, Outlet } from "@tanstack/react-router";
import ErrorPage from "@/federated-modules/errorPages/ErrorPage";
import NotFoundPage from "@/federated-modules/errorPages/NotFoundPage";

export const Route = createRootRoute({
  component: Root,
  errorComponent: ErrorPage,
  notFoundComponent: NotFoundPage
});

function Root() {
  return (
    <>
      <Outlet />
      <OnboardingGuard />
    </>
  );
}

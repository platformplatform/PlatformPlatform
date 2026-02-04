import { PageTracker } from "@repo/infrastructure/applicationInsights/PageTracker";
import { AuthenticationProvider } from "@repo/infrastructure/auth/AuthenticationProvider";
import { AuthSyncModal } from "@repo/infrastructure/auth/AuthSyncModal";
import { useErrorTrigger } from "@repo/infrastructure/development/useErrorTrigger";
import { useInitializeLocale } from "@repo/infrastructure/translations/useInitializeLocale";
import { AddToHomescreen } from "@repo/ui/components/AddToHomescreen";
import { BannerPortal } from "@repo/ui/components/BannerPortal";
import { ThemeModeProvider } from "@repo/ui/theme/mode/ThemeMode";
import { QueryClientProvider } from "@tanstack/react-query";
import { createRootRoute, Outlet, useNavigate } from "@tanstack/react-router";
import { lazy, useEffect } from "react";
import { queryClient } from "@/shared/lib/api/client";

const FederatedAuthSyncModal = lazy(() => import("account/AuthSyncModal"));
const FederatedBanners = lazy(() => import("account/Banners"));
const FederatedErrorPage = lazy(() => import("account/FederatedErrorPage"));
const FederatedNotFoundPage = lazy(() => import("account/FederatedNotFoundPage"));

export const Route = createRootRoute({
  component: Root,
  errorComponent: FederatedErrorPage,
  notFoundComponent: FederatedNotFoundPage
});

function Root() {
  const navigate = useNavigate();
  useInitializeLocale();
  useErrorTrigger();

  useEffect(() => {
    import("account/AccountApp");
  }, []);

  return (
    <QueryClientProvider client={queryClient}>
      <ThemeModeProvider>
        <AuthenticationProvider navigate={(options) => navigate(options)}>
          <BannerPortal>
            <FederatedBanners />
          </BannerPortal>
          <AddToHomescreen />
          <PageTracker />
          <Outlet />
          <AuthSyncModal modalComponent={FederatedAuthSyncModal} />
        </AuthenticationProvider>
      </ThemeModeProvider>
    </QueryClientProvider>
  );
}

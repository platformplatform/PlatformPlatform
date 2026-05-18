import { PageTracker } from "@repo/infrastructure/applicationInsights/PageTracker";
import { AuthenticationProvider } from "@repo/infrastructure/auth/AuthenticationProvider";
import { AuthSyncModal } from "@repo/infrastructure/auth/AuthSyncModal";
import { productName, showAddToHomescreen, themeColor } from "@repo/infrastructure/branding";
import { useErrorTrigger } from "@repo/infrastructure/development/useErrorTrigger";
import { OnboardingGuard } from "@repo/infrastructure/onboarding/OnboardingGuard";
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
const ErrorPage = lazy(() => import("account/ErrorPage"));
const NotFoundPage = lazy(() => import("account/NotFoundPage"));

export const Route = createRootRoute({
  component: Root,
  errorComponent: ErrorPage,
  notFoundComponent: NotFoundPage
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
      <ThemeModeProvider themeColor={themeColor}>
        <AuthenticationProvider navigate={(options) => navigate(options)}>
          <BannerPortal>
            <FederatedBanners />
          </BannerPortal>
          {showAddToHomescreen && <AddToHomescreen productName={productName} />}
          <PageTracker />
          <Outlet />
          <AuthSyncModal modalComponent={FederatedAuthSyncModal} />
          <OnboardingGuard />
        </AuthenticationProvider>
      </ThemeModeProvider>
    </QueryClientProvider>
  );
}

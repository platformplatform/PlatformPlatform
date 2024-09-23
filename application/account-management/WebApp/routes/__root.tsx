import { createRootRoute, Outlet, useNavigate } from "@tanstack/react-router";
import { ErrorPage } from "@repo/infrastructure/errorComponents/ErrorPage";
import { NotFound } from "@repo/infrastructure/errorComponents/NotFoundPage";
import { AuthenticationContext, AuthenticationProvider } from "@repo/infrastructure/auth/AuthenticationProvider";
import { ReactAriaRouterProvider } from "@repo/infrastructure/router/ReactAriaRouterProvider";
import { ThemeModeProvider } from "@repo/ui/theme/mode/ThemeMode";
import { useContext, useEffect, useState } from "react";
import UserProfileModal from "@/shared/components/userModals/UserProfileModal";

export const Route = createRootRoute({
  component: Root,
  errorComponent: ErrorPage,
  notFoundComponent: NotFound
});

function Root() {
  const navigate = useNavigate();
  const { userInfo } = useContext(AuthenticationContext);
  const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);

  useEffect(() => {
    if (userInfo?.isAuthenticated && (!userInfo.firstName || !userInfo.lastName)) {
      setIsProfileModalOpen(true);
    }
  }, [userInfo]);

  return (
    <ThemeModeProvider>
      <ReactAriaRouterProvider>
        <AuthenticationProvider navigate={(options) => navigate(options)}>
          <Outlet />
          {userInfo?.isAuthenticated && (
            <UserProfileModal
              isOpen={isProfileModalOpen}
              onOpenChange={setIsProfileModalOpen}
              userId={userInfo.userId ?? ""}
            />
          )}
        </AuthenticationProvider>
      </ReactAriaRouterProvider>
    </ThemeModeProvider>
  );
}

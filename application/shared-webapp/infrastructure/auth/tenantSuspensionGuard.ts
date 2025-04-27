import { useLocation } from "@tanstack/react-router";
import { loggedInPath } from "./constants";
import { useIsAuthenticated, useUserInfo } from "./hooks";

/**
 * Hook that handles tenant suspension redirects.
 * Automatically redirects based on tenant state and current path.
 */
export function useTenantSuspensionGuard(): void {
  const userInfo = useUserInfo();
  const isAuthenticated = useIsAuthenticated();
  const location = useLocation();
  const isSuspendedPage = location.pathname === "/suspended";

  if (isAuthenticated) {
    const isTenantSuspended = userInfo?.tenantState === "Suspended";

    if (isTenantSuspended && !isSuspendedPage) {
      window.location.href = "/suspended"; // If tenant is suspended and not on suspended page, redirect to suspended page
    }

    if (!isTenantSuspended && isSuspendedPage) {
      window.location.href = loggedInPath; // If tenant is not suspended but on suspended page, redirect to logged-in path
    }
  }

  if (!isAuthenticated && isSuspendedPage) {
    window.location.href = loggedInPath; // Handle unauthenticated users trying to access the suspended page
  }
}

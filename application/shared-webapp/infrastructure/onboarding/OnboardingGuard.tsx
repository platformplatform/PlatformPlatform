import { Navigate, useRouterState } from "@tanstack/react-router";

const welcomePath = "/welcome";

/**
 * Guard that automatically redirects to welcome page when onboarding is incomplete.
 * - For Owners: Redirects when either tenantName or firstName is missing
 * - For non-Owners: Redirects when firstName is missing
 * Place this as a sibling to your main app content, similar to AuthSyncModal.
 */
export function OnboardingGuard() {
  const routerState = useRouterState();
  const { isAuthenticated, firstName, role, tenantName } = import.meta.user_info_env;

  const pathname = routerState.location.pathname;
  const isOnWelcomePage = pathname.startsWith(welcomePath);
  const isOwner = role === "Owner";

  const hasCompletedAccountSetup = !isOwner || !!tenantName;
  const hasCompletedProfileSetup = !!firstName;
  const isOnboardingComplete = hasCompletedAccountSetup && hasCompletedProfileSetup;

  const shouldRedirect = isAuthenticated && !isOnboardingComplete && !isOnWelcomePage;

  if (shouldRedirect) {
    // Use existing returnPath from URL, or current pathname as the return destination
    const existingReturnPath = new URLSearchParams(routerState.location.search).get("returnPath");
    const returnPath = existingReturnPath || pathname;

    return <Navigate to={welcomePath} search={{ returnPath }} />;
  }

  return null;
}

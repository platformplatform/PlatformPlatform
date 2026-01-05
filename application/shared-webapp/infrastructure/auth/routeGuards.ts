import { loginPath } from "./constants";

export type UserRole = "Owner" | "Admin" | "Member";

export interface RoutePermissions {
  requiresInternalUser?: boolean;
  allowedRoles?: UserRole[];
}

/**
 * Redirect to login if user is not authenticated.
 * Use this in beforeLoad to prevent unauthorized access before the route renders.
 *
 * @example
 * export const Route = createFileRoute("/admin")({
 *   beforeLoad: () => requireAuthentication(),
 *   component: AdminLayout
 * });
 */
export function requireAuthentication(): void {
  const { isAuthenticated } = import.meta.user_info_env;
  if (!isAuthenticated) {
    const returnPath = encodeURIComponent(window.location.pathname);
    window.location.href = `${loginPath}?returnPath=${returnPath}`;
  }
}

/**
 * Check if the current user has the required permissions.
 * Use this in components to conditionally render content or show access denied.
 *
 * @param permissions - The required permissions
 * @returns true if user has all required permissions, false otherwise
 *
 * @example
 * function ProtectedPage() {
 *   if (!hasPermission({ allowedRoles: ["Owner", "Admin"] })) {
 *     return <AccessDeniedContent />;
 *   }
 *   return <ActualContent />;
 * }
 */
export function hasPermission(permissions: RoutePermissions): boolean {
  const { isInternalUser, role } = import.meta.user_info_env;

  if (permissions.requiresInternalUser && !isInternalUser) {
    return false;
  }

  if (permissions.allowedRoles && permissions.allowedRoles.length > 0) {
    if (!role || !permissions.allowedRoles.includes(role)) {
      return false;
    }
  }

  return true;
}

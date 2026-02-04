import { loginPath } from "./constants";

export type UserRole = "Owner" | "Admin" | "Member";

export interface RoutePermissions {
  requiresInternalUser?: boolean;
  allowedRoles?: UserRole[];
}

/**
 * Custom error for access denied scenarios.
 * Thrown by requirePermission and caught by error boundaries.
 */
export class AccessDeniedError extends Error {
  constructor(message = "Access denied") {
    super(message);
    this.name = "AccessDeniedError";
  }
}

/**
 * Check if an error is an AccessDeniedError.
 */
export function isAccessDeniedError(error: unknown): error is AccessDeniedError {
  return error instanceof AccessDeniedError;
}

/**
 * Custom error for not found scenarios.
 * Thrown by catch-all routes and caught by error boundaries to render full-page 404.
 */
export class NotFoundError extends Error {
  constructor(message = "Page not found") {
    super(message);
    this.name = "NotFoundError";
  }
}

/**
 * Check if an error is a NotFoundError.
 */
export function isNotFoundError(error: unknown): error is NotFoundError {
  return error instanceof NotFoundError;
}

/**
 * Redirect to login if user is not authenticated.
 * Use this in beforeLoad to prevent unauthorized access before the route renders.
 *
 * @example
 * export const Route = createFileRoute("/account")({
 *   beforeLoad: () => requireAuthentication(),
 *   component: AccountLayout
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
 * Require specific permissions. Throws AccessDeniedError if permission is denied.
 * Use this in beforeLoad to prevent unauthorized access before the route renders.
 *
 * @param permissions - The required permissions
 * @throws AccessDeniedError if user lacks required permissions
 *
 * @example
 * export const Route = createFileRoute("/account/users/recycle-bin")({
 *   beforeLoad: () => requirePermission({ allowedRoles: ["Owner", "Admin"] }),
 *   component: RecycleBinPage
 * });
 */
export function requirePermission(permissions: RoutePermissions): void {
  if (!hasPermission(permissions)) {
    throw new AccessDeniedError();
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

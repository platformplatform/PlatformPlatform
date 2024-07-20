import type { ReactNode } from "react";
import { useUserInfo } from "./hooks";
import type { UserRole } from "./actions";

export interface LoggedInProps {
  children: ReactNode;
  requiredRoles?: UserRole[];
}

/**
 * Show component if user is logged in and has the required role.
 */
export function LoggedIn({ children, requiredRoles }: LoggedInProps) {
  const userInfo = useUserInfo();
  if (userInfo == null) return null;
  if (requiredRoles != null && (userInfo.userRole == null || requiredRoles.includes(userInfo.userRole) === false))
    return null;
  return children;
}

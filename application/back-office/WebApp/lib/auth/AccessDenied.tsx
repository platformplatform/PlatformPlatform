import type { ReactNode } from "react";
import { useUserInfo } from "./hooks";
import type { UserRole } from "./actions";

export interface AccessDeniedProps {
  children: ReactNode;
  requiredRoles: UserRole[];
}

/**
 * Display component if user is logged in but does not have the required role.
 */
export function AccessDenied({ children, requiredRoles }: AccessDeniedProps) {
  const userInfo = useUserInfo();
  if (userInfo == null)
    return null;
  if (userInfo.userRole && requiredRoles.includes(userInfo.userRole))
    return null;
  return children;
}

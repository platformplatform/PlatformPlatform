import type { ReactNode } from "react";
import { useUserInfo } from "./hooks";

export interface AccessDeniedProps {
  children: ReactNode;
  requiredRoles: UserInfoEnv["userRole"][];
}

/**
 * Display component if a user is logged in but does not have the required role.
 */
export function AccessDenied({ children, requiredRoles }: AccessDeniedProps) {
  return requiredRoles.includes(useUserInfo()?.userRole) ? null : children;
}

import type { ReactNode } from "react";
import { useUserInfo } from "./hooks";

export interface AccessDeniedProps {
  children: ReactNode;
  requiredRoles: UserInfoEnv["role"][];
}

/**
 * Display component if a user is logged in but does not have the required role.
 */
export function AccessDenied({ children, requiredRoles }: AccessDeniedProps) {
  const userInfo = useUserInfo();
  if (userInfo == null) return null;
  if (userInfo.role && requiredRoles.includes(userInfo.role)) return null;
  return children;
}

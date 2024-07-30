import type { ReactNode } from "react";
import { useUserInfo } from "./hooks";

export interface LoggedInProps {
  children: ReactNode;
  requiredRoles?: UserInfoEnv["role"][];
}

/**
 * Show component if user is logged in and has the required role.
 */
export function LoggedIn({ children, requiredRoles }: LoggedInProps) {
  const userInfo = useUserInfo();
  if (userInfo == null) return null;
  if (requiredRoles != null && (userInfo.role == null || !requiredRoles.includes(userInfo.role))) return null;
  return children;
}

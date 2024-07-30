import type { ReactNode } from "react";
import { useUserInfo } from "./hooks";

export interface LoggedInProps {
  children: ReactNode;
  requiredRoles?: UserInfoEnv["userRole"][];
}

/**
 * Show component if a user is logged in and has the required role.
 */
export function LoggedIn({ children, requiredRoles }: LoggedInProps) {
  return requiredRoles?.includes(useUserInfo()?.userRole) ? children : null;
}

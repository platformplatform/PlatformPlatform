import type { ReactNode } from "react";
import { useUserInfo } from "./hooks";
import type { UserInfo } from "./AuthenticationProvider";

export interface LoggedInProps {
  requiredRoles?: UserInfo["role"][];
  children: ReactNode;
}

/**
 * Show component if the user is logged in and has the required role.
 */
export function LoggedIn({ requiredRoles, children }: LoggedInProps) {
  return requiredRoles?.includes(useUserInfo()?.role) ? children : null;
}

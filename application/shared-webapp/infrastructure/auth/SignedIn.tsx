import type { ReactNode } from "react";
import { useUserInfo } from "./hooks";
import type { UserInfo } from "./AuthenticationProvider";

export interface SignedInProps {
  requiredRoles?: UserInfo["userRole"][];
  children: ReactNode;
}

/**
 * Show component if a user is logged in and has the required role.
 */
export function LoggedIn({ requiredRoles, children }: SignedInProps) {
  return requiredRoles?.includes(useUserInfo()?.userRole) ? children : null;
}

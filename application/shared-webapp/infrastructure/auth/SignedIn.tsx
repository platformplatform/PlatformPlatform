import type { ReactNode } from "react";
import { useUserInfo } from "./hooks";
import type { UserInfo } from "./AuthenticationProvider";

export interface SignedInProps {
  children: ReactNode;
  requiredRoles?: UserInfo["role"][];
}

/**
 * Show component if user is logged in and has the required role.
 */
export function LoggedIn({ children, requiredRoles }: SignedInProps) {
  const userInfo = useUserInfo();
  if (userInfo == null) return null;
  if (requiredRoles != null && (userInfo.role == null || !requiredRoles.includes(userInfo.role))) return null;
  return children;
}

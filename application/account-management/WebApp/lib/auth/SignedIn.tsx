import type { ReactNode } from "react";
import { useUserInfo } from "./hooks";
import type { UserRole } from "./actions";

export interface SignedInProps {
  children: ReactNode;
  requiredRoles?: UserRole[];
}

/**
 * Show component if user is signed in and has the required role.
 */
export function SignedIn({ children, requiredRoles }: SignedInProps) {
  const userInfo = useUserInfo();
  if (userInfo == null)
    return null;
  if (requiredRoles != null && (userInfo.userRole == null || requiredRoles.includes(userInfo.userRole) === false))
    return null;
  return children;
}

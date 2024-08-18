import type { ReactNode } from "react";
import { useUserInfo } from "./hooks";

type AccessDeniedProps = {
  requiredRoles: UserInfoEnv["userRole"][];
  children: ReactNode;
};

/**
 * Display component if the user is logged in but does not have the required role.
 */
export function AccessDenied({ requiredRoles, children }: AccessDeniedProps) {
  return requiredRoles.includes(useUserInfo()?.userRole) ? null : children;
}

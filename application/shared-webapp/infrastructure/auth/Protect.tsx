import type { ReactNode } from "react";
import { useUserInfo } from "./hooks";

type ProtectProps = {
  fallback?: ReactNode;
  requiredRoles: UserInfoEnv["role"][];
  children: ReactNode;
};

export function Protect({ requiredRoles, fallback, children }: ProtectProps) {
  const userInfo = useUserInfo();
  if (userInfo?.role && requiredRoles.includes(userInfo.role)) {
    return children;
  }

  return fallback;
}

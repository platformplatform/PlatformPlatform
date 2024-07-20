import type { ReactNode } from "react";
import { useIsAuthenticated } from "./hooks";

export interface LoggedOutProps {
  children: ReactNode;
}

/**
 * Show component if user is logged out.
 */
export function LoggedOut({ children }: LoggedOutProps) {
  const isAuthenticated = useIsAuthenticated();
  if (isAuthenticated) return null;
  return children;
}

import type { ReactNode } from "react";
import { useIsAuthenticated } from "./hooks";

export interface LoggedOutProps {
  children: ReactNode;
}

/**
 * Show component if no user is logged in.
 */
export function LoggedOut({ children }: LoggedOutProps) {
  return useIsAuthenticated() ? null : children;
}

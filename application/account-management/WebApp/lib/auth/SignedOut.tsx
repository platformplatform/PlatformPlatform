import type { ReactNode } from "react";
import { useIsAuthenticated } from "./hooks";

export interface SignedOutProps {
  children: ReactNode;
}

/**
 * Show component if user is signed out.
 */
export function SignedOut({ children }: SignedOutProps) {
  const isAuthenticated = useIsAuthenticated();
  if (isAuthenticated)
    return null;
  return children;
}

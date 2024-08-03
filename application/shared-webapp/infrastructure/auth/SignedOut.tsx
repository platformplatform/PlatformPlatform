import type { ReactNode } from "react";
import { useIsAuthenticated } from "./hooks";

export interface SignedOutProps {
  fallback?: ReactNode;
  children: ReactNode;
}

/**
 * Show component if user is logged out.
 */
export function SignedOut({ children, fallback }: SignedOutProps) {
  const isAuthenticated = useIsAuthenticated();
  if (isAuthenticated) return fallback;
  return children;
}

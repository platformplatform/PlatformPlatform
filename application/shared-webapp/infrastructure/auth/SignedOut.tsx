import type { ReactNode } from "react";
import { useIsAuthenticated } from "./hooks";

export interface SignedOutProps {
  children: ReactNode;
}

/**
 * Show component if no user is logged in.
 */
export function SignedOut({ children }: SignedOutProps) {
  return useIsAuthenticated() ? null : children;
}

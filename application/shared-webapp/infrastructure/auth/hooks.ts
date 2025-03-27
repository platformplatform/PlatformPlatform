import { useContext } from "react";
import { AuthenticationContext } from "./AuthenticationProvider";

/**
 * Get the authentication context.
 */
export function useAuthentication() {
  const context = useContext(AuthenticationContext);
  if (!context) {
    throw new Error("useAuthentication must be used within an AuthenticationProvider.");
  }

  return context;
}

/**
 * Get the current user info.
 */
export function useUserInfo() {
  return useAuthentication().userInfo;
}

/**
 * Return true if the current user is logged in.
 */
export function useIsAuthenticated() {
  const userInfo = useUserInfo();
  return userInfo?.isAuthenticated ?? false;
}

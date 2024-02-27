import { useContext } from "react";
import { AuthenticationContext } from "./AuthenticationProvider";

/**
 * Get the authentication context.
 */
export function useAuthentication() {
  const context = useContext(AuthenticationContext);
  if (!context)
    throw new Error("useAuthentication must be used within an AuthenticationProvider");

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
  return useUserInfo() != null;
}

/**
 * Return the current user role. If the user is not logged in, return null.
 */
export function useUserRole() {
  return useUserInfo()?.userRole ?? null;
}

/**
 * Return the sign in action.
 * [FormAction]
 */
export function useSignInAction() {
  return useAuthentication().signInAction;
}

/**
 * Return the sign out action.
 * [FormAction]
 */
export function useSignOutAction() {
  return useAuthentication().signOutAction;
}

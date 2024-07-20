import { useContext } from "react";
import { AuthenticationContext } from "./AuthenticationProvider";
export type { AuthenticationState } from "./actions";

/**
 * Get the authentication context.
 */
export function useAuthentication() {
  const context = useContext(AuthenticationContext);
  if (!context) throw new Error("useAuthentication must be used within an AuthenticationProvider");

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
 * Return the log in action.
 * [FormAction]
 */
export function useLogInAction() {
  return useAuthentication().logInAction;
}

/**
 * Return the log out action.
 * [FormAction]
 */
export function useLogOutAction() {
  return useAuthentication().logOutAction;
}

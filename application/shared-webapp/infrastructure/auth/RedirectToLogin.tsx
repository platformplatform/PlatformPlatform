import { Navigate } from "@tanstack/react-router";
import { createLoginUrlWithReturnPath } from "./util";
import { loginPath } from "./constants";

type RedirectToLoginProps = {
  /**
   * The URL to redirect to after signing in.
   *
   * Note: Defaults to the current URL.
   */
  returnUrl?: string;
};

export function RedirectToLogin({ returnUrl }: RedirectToLoginProps) {
  return <Navigate to={createLoginUrlWithReturnPath(loginPath, returnUrl)} />;
}

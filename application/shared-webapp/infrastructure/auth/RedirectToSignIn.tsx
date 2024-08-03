import { Navigate } from "@tanstack/react-router";
import { createUrlWithReturnUrl } from "./util";
import { signInPath } from "./constants";

type RedirectToSignInProps = {
  /**
   * The URL to redirect to after signing in.
   *
   * Note: Defaults to the current URL.
   */
  returnUrl?: string;
};

export function RedirectToSignIn({ returnUrl }: Readonly<RedirectToSignInProps>) {
  return <Navigate to={createUrlWithReturnUrl(signInPath, returnUrl)} />;
}

/**
 * Create a login path that will be used to log in a user and return to the original requested url.
 * For security reasons, only the path is used to redirect the user back to the requested location.
 *
 * @param loginPath The path to redirect to.
 * @param returnUrl The return path to set in the query parameter (defaults to the current url)
 */
export function createLoginUrlWithReturnPath(loginPath: string, returnUrl?: string) {
  const redirectUrl = new URL(loginPath, returnUrl ?? window.location.href);
  redirectUrl.searchParams.set("returnPath", window.location.pathname + window.location.search);
  return redirectUrl.href;
}

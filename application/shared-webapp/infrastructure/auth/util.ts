/**
 * Create a URL with a return URL query parameter.
 *
 * @param pathname The path to redirect to.
 * @param returnUrl The return URL to set in the query parameter. (default: current URL)
 */
export function createUrlWithReturnUrl(pathname: string, returnUrl?: string) {
  const urlWithRedirectUrl = new URL(pathname, returnUrl ?? window.location.href);
  urlWithRedirectUrl.searchParams.set("returnUrl", window.location.href);

  return urlWithRedirectUrl.href;
}

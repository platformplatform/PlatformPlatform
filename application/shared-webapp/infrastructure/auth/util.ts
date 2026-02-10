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

/**
 * Validates that a return path is a safe relative path to prevent open redirect attacks.
 * Mirrors the backend ReturnPathHelper.IsValidRelativePath() logic.
 *
 * Rejects protocol-relative URLs (//evil.com), backslash paths, absolute URLs with
 * schemes (://), and directory traversal (..) after URL decoding.
 */
export function isValidReturnPath(path: string): boolean {
  if (!path.startsWith("/")) {
    return false;
  }

  let decoded: string;
  try {
    decoded = decodeURIComponent(path);
  } catch {
    return false;
  }

  if (decoded.startsWith("//")) {
    return false;
  }

  if (decoded.includes("\\")) {
    return false;
  }

  if (decoded.includes("://")) {
    return false;
  }

  if (decoded.includes("..")) {
    return false;
  }

  return true;
}

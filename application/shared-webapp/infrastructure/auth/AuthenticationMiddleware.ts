import type { Middleware } from "openapi-fetch";
import { createUrlWithReturnUrl } from "./util";
import { signInPath } from "./constants";

type AuthenticationMiddlewareOptions = {
  customSignInPath?: string;
};
export function createAuthenticationMiddleware(options?: AuthenticationMiddlewareOptions): Middleware {
  return {
    onResponse(context) {
      if (context.response.status === 401) {
        window.location.href = createUrlWithReturnUrl(options?.customSignInPath ?? signInPath);
      }
    }
  };
}

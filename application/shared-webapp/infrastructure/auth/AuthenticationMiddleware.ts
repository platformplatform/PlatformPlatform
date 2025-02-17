import type { Middleware } from "openapi-fetch";
import { loginPath } from "./constants";
import { createLoginUrlWithReturnPath } from "./util";

type AuthenticationMiddlewareOptions = {
  customLoginPath?: string;
};

export function createAuthenticationMiddleware(options?: AuthenticationMiddlewareOptions): Middleware {
  return {
    onResponse(context) {
      if (context.response.status === 401) {
        window.location.href = createLoginUrlWithReturnPath(options?.customLoginPath ?? loginPath);
      }
    }
  };
}

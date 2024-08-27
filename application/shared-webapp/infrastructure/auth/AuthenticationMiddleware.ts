import type { Middleware } from "openapi-fetch";
import { createLoginUrlWithReturnPath } from "./util";
import { loginPath } from "./constants";

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

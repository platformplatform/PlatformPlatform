import type { Middleware } from "openapi-fetch";

type AuthenticationMiddlewareOptions = {
  loginPath: string;
};

export function createAuthenticationMiddleware({ loginPath }: AuthenticationMiddlewareOptions): Middleware {
  return {
    onResponse(options) {
      if (options.response.status === 401) {
        const redirectUrl = new URL(loginPath, window.location.href);
        redirectUrl.searchParams.set("returnUrl", window.location.pathname + window.location.search);
        window.location.replace(redirectUrl.href);
      }
    }
  };
}

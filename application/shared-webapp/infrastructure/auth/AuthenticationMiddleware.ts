import type { Middleware } from "openapi-fetch";
import { loginPath } from "./constants";
import { createLoginUrlWithReturnPath } from "./util";

type AuthenticationMiddlewareOptions = {
  customLoginPath?: string;
};

// IMPORTANT: Must be kept in sync with UnauthorizedReason enum in shared-kernel/SharedKernel/Authentication/UnauthorizedReason.cs
const UnauthorizedReason = {
  Revoked: "Revoked",
  ReplayAttackDetected: "ReplayAttackDetected",
  SessionNotFound: "SessionNotFound"
} as const;

// Error codes used in /error page query parameter
export const ErrorCode = {
  ReplayAttack: "replay-attack",
  SessionRevoked: "session-revoked",
  SessionNotFound: "session-not-found"
} as const;

const unauthorizedReasonHeaderKey = "x-unauthorized-reason";

function getErrorCodeFromUnauthorizedReason(reason: string | null): string | null {
  switch (reason) {
    case UnauthorizedReason.ReplayAttackDetected:
      return ErrorCode.ReplayAttack;
    case UnauthorizedReason.Revoked:
      return ErrorCode.SessionRevoked;
    case UnauthorizedReason.SessionNotFound:
      return ErrorCode.SessionNotFound;
    default:
      return null;
  }
}

export function createAuthenticationMiddleware(options?: AuthenticationMiddlewareOptions): Middleware {
  return {
    onResponse(context) {
      if (context.response.status === 401) {
        const loginUrl = createLoginUrlWithReturnPath(options?.customLoginPath ?? loginPath);
        const unauthorizedReason = context.response.headers.get(unauthorizedReasonHeaderKey);
        const errorCode = getErrorCodeFromUnauthorizedReason(unauthorizedReason);

        if (errorCode) {
          const errorUrl = new URL(loginUrl);
          errorUrl.pathname = "/error";
          errorUrl.searchParams.set("error", errorCode);
          globalThis.location.href = errorUrl.href;
        } else {
          globalThis.location.href = loginUrl;
        }
      }
    }
  };
}

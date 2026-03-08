import type { ReactNode } from "react";

import { Trans } from "@lingui/react/macro";
import { ErrorCode } from "@repo/infrastructure/auth/AuthenticationMiddleware";
import { AlertCircle, Building2, LogOut, ShieldAlert, UserX } from "lucide-react";

export type ErrorAction = "login" | "signup" | "contact";

export type ErrorDisplay = {
  icon: ReactNode;
  iconBackground: string;
  title: ReactNode;
  message: ReactNode;
  action: ErrorAction;
  secondaryAction?: ErrorAction;
};

export const errorLabelMap: Record<string, string> = {
  [ErrorCode.ReplayAttack]: "Security alert",
  [ErrorCode.SessionRevoked]: "Session ended",
  [ErrorCode.SessionNotFound]: "Session expired",
  [ErrorCode.SessionExpired]: "Session expired",
  [ErrorCode.UserNotFound]: "Account not found",
  [ErrorCode.AccountAlreadyExists]: "Account already exists",
  [ErrorCode.IdentityMismatch]: "Identity mismatch",
  [ErrorCode.AuthenticationFailed]: "Authentication failed",
  [ErrorCode.InvalidRequest]: "Invalid request",
  [ErrorCode.AccessDenied]: "Access denied",
  [ErrorCode.TenantDeleted]: "Account deleted"
};

export function getErrorDisplay(error: string): ErrorDisplay {
  switch (error) {
    case ErrorCode.ReplayAttack:
      return {
        icon: <ShieldAlert className="size-10 text-destructive" />,
        iconBackground: "bg-destructive/10",
        title: <Trans>Security alert</Trans>,
        message: (
          <>
            <Trans>
              We detected suspicious activity on your account. Someone may have attempted to take over your session.
            </Trans>
            <br />
            <Trans>For your protection, you have been logged out. Please log in again to continue.</Trans>
          </>
        ),
        action: "login"
      };

    case ErrorCode.SessionRevoked:
      return {
        icon: <LogOut className="size-10 text-muted-foreground" />,
        iconBackground: "bg-muted",
        title: <Trans>Session ended</Trans>,
        message: (
          <>
            <Trans>Your session was ended from another device.</Trans>
            <br />
            <Trans>Please log in again to continue.</Trans>
          </>
        ),
        action: "login"
      };

    case ErrorCode.SessionNotFound:
    case ErrorCode.SessionExpired:
      return {
        icon: <LogOut className="size-10 text-muted-foreground" />,
        iconBackground: "bg-muted",
        title: <Trans>Session expired</Trans>,
        message: (
          <>
            <Trans>Your session has expired.</Trans>
            <br />
            <Trans>Please log in again to continue.</Trans>
          </>
        ),
        action: "login"
      };

    case ErrorCode.UserNotFound:
      return {
        icon: <UserX className="size-10 text-muted-foreground" />,
        iconBackground: "bg-muted",
        title: <Trans>Account not found</Trans>,
        message: <Trans>No account found for this email address. Please sign up to create an account.</Trans>,
        action: "signup",
        secondaryAction: "login"
      };

    case ErrorCode.AccountAlreadyExists:
      return {
        icon: <UserX className="size-10 text-muted-foreground" />,
        iconBackground: "bg-muted",
        title: <Trans>Account already exists</Trans>,
        message: <Trans>An account with this email already exists. Please log in instead.</Trans>,
        action: "login",
        secondaryAction: "signup"
      };

    case ErrorCode.IdentityMismatch:
      return {
        icon: <ShieldAlert className="size-10 text-destructive" />,
        iconBackground: "bg-destructive/10",
        title: <Trans>Identity mismatch</Trans>,
        message: (
          <>
            <Trans>This account is linked to a different Google identity.</Trans>
            <br />
            <Trans>This can happen when email ownership has changed. Contact your account administrator.</Trans>
          </>
        ),
        action: "contact"
      };

    case ErrorCode.AuthenticationFailed:
      return {
        icon: <AlertCircle className="size-10 text-destructive" />,
        iconBackground: "bg-destructive/10",
        title: <Trans>Authentication failed</Trans>,
        message: <Trans>We detected a security issue with your login attempt. Please try again.</Trans>,
        action: "login"
      };

    case ErrorCode.InvalidRequest:
      return {
        icon: <AlertCircle className="size-10 text-destructive" />,
        iconBackground: "bg-destructive/10",
        title: <Trans>Invalid request</Trans>,
        message: <Trans>The authentication request was invalid. Please try again.</Trans>,
        action: "login"
      };

    case ErrorCode.AccessDenied:
      return {
        icon: <AlertCircle className="size-10 text-muted-foreground" />,
        iconBackground: "bg-muted",
        title: <Trans>Access denied</Trans>,
        message: <Trans>Authentication was cancelled or denied. Please try again if you want to continue.</Trans>,
        action: "login"
      };

    case ErrorCode.TenantDeleted:
      return {
        icon: <Building2 className="size-10 text-destructive" />,
        iconBackground: "bg-destructive/10",
        title: <Trans>Account deleted</Trans>,
        message: (
          <>
            <Trans>Your account has been deleted.</Trans>
            <br />
            <Trans>Contact the account owner immediately if you believe this is incorrect.</Trans>
          </>
        ),
        action: "login"
      };

    default:
      return {
        icon: <AlertCircle className="size-10 text-destructive" />,
        iconBackground: "bg-destructive/10",
        title: <Trans>Something went wrong</Trans>,
        message: (
          <Trans>An unexpected error occurred. Please try again or contact support if the problem persists.</Trans>
        ),
        action: "login"
      };
  }
}

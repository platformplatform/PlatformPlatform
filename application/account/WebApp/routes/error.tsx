import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { ErrorCode } from "@repo/infrastructure/auth/AuthenticationMiddleware";
import { AuthenticationContext } from "@repo/infrastructure/auth/AuthenticationProvider";
import { loginPath, signUpPath } from "@repo/infrastructure/auth/constants";
import { useIsAuthenticated, useUserInfo } from "@repo/infrastructure/auth/hooks";
import { isValidReturnPath } from "@repo/infrastructure/auth/util";
import { Button } from "@repo/ui/components/Button";
import { Link } from "@repo/ui/components/Link";
import { createFileRoute, Navigate, useNavigate } from "@tanstack/react-router";
import { AlertCircle, Building2, LogIn, LogOut, ShieldAlert, UserPlus, UserX } from "lucide-react";
import { type ReactNode, useContext, useState } from "react";
import LocaleSwitcher from "@/federated-modules/common/LocaleSwitcher";
import SupportButton from "@/federated-modules/common/SupportButton";
import ThemeModeSelector from "@/federated-modules/common/ThemeModeSelector";
import logoMark from "@/shared/images/logo-mark.svg";
import logoWrap from "@/shared/images/logo-wrap.svg";

export const Route = createFileRoute("/error")({
  validateSearch: (search) => {
    const params = search as { error?: string; returnPath?: string; id?: string };
    return {
      error: params.error,
      returnPath: params.returnPath && isValidReturnPath(params.returnPath) ? params.returnPath : undefined,
      id: params.id && /^[a-zA-Z0-9-]+$/.test(params.id) ? params.id : undefined
    };
  },
  component: ErrorPage
});

type ErrorAction = "login" | "signup" | "contact";

function getErrorDisplay(error: string): {
  icon: ReactNode;
  iconBackground: string;
  title: ReactNode;
  message: ReactNode;
  action: ErrorAction;
  secondaryAction?: ErrorAction;
} {
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

function useAuthInfoSafe() {
  const context = useContext(AuthenticationContext);
  const hasContext = context.userInfo !== null;
  const isAuthenticated = useIsAuthenticated();
  const userInfo = useUserInfo();

  return {
    isAuthenticated: hasContext && isAuthenticated,
    userInfo: hasContext ? userInfo : null
  };
}

function ErrorNavigation() {
  const { isAuthenticated, userInfo } = useAuthInfoSafe();
  const [isLoggingOut, setIsLoggingOut] = useState(false);

  const handleLogout = async () => {
    setIsLoggingOut(true);
    try {
      const response = await fetch("/api/account/authentication/logout", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "x-xsrf-token": import.meta.antiforgeryToken
        }
      });
      if (response.ok) {
        globalThis.location.href = loginPath;
      }
    } catch {
      globalThis.location.href = loginPath;
    }
  };

  return (
    <nav className="mx-auto flex w-full max-w-7xl items-center justify-between gap-4 px-6 pt-8 pb-4">
      <Link href="/" variant="logo" underline={false}>
        <img className="hidden h-10 w-[17.5rem] sm:block" src={logoWrap} alt={t`PlatformPlatform logo`} />
        <img className="size-10 sm:hidden" src={logoMark} alt={t`PlatformPlatform logo`} />
      </Link>

      <div className="flex items-center gap-6">
        <span className="flex gap-2">
          <ThemeModeSelector />
          <SupportButton />
          <LocaleSwitcher />
        </span>
        {isAuthenticated && userInfo && (
          <Button variant="outline" onClick={handleLogout} disabled={isLoggingOut} aria-label={t`Log out`}>
            <LogOut size={16} />
            <span className="hidden sm:inline">
              <Trans>Log out</Trans>
            </span>
          </Button>
        )}
      </div>
    </nav>
  );
}

type ActionButtonProps = {
  action: ErrorAction;
  variant: "default" | "outline";
  onLogIn: () => void;
  onSignUp: () => void;
};

function ActionButton({ action, variant, onLogIn, onSignUp }: ActionButtonProps) {
  switch (action) {
    case "signup":
      return (
        <Button variant={variant} onClick={onSignUp} aria-label={t`Sign up`}>
          <UserPlus size={16} />
          <Trans>Sign up</Trans>
        </Button>
      );
    case "contact":
      return (
        <Button variant={variant} onClick={onLogIn} aria-label={t`Log in`}>
          <LogIn size={16} />
          <Trans>Back to login</Trans>
        </Button>
      );
    default:
      return (
        <Button variant={variant} onClick={onLogIn} aria-label={t`Log in`}>
          <LogIn size={16} />
          <Trans>Log in</Trans>
        </Button>
      );
  }
}

function ErrorPage() {
  const { error, returnPath, id } = Route.useSearch();
  const navigate = useNavigate();

  if (!error) {
    return <Navigate to="/login" search={{ returnPath }} />;
  }

  const errorDisplay = getErrorDisplay(error);

  const handleLogIn = () => {
    navigate({ to: "/login", search: { returnPath } });
  };

  const handleSignUp = () => {
    navigate({ to: signUpPath });
  };

  return (
    <main style={{ minHeight: "100vh" }} className="flex w-full flex-col bg-background">
      <ErrorNavigation />

      <div style={{ flex: 1 }} className="flex flex-col items-center justify-center gap-8 px-6 pt-12 pb-32 text-center">
        <div className="flex max-w-lg flex-col items-center gap-6">
          <div className={`flex size-20 items-center justify-center rounded-full ${errorDisplay.iconBackground}`}>
            {errorDisplay.icon}
          </div>

          <div className="flex flex-col gap-3">
            <h1>{errorDisplay.title}</h1>
            <p className="text-lg text-muted-foreground">{errorDisplay.message}</p>
          </div>

          <div className="flex flex-wrap justify-center gap-3 pt-2">
            <ActionButton
              action={errorDisplay.action}
              variant="default"
              onLogIn={handleLogIn}
              onSignUp={handleSignUp}
            />
            {errorDisplay.secondaryAction && (
              <ActionButton
                action={errorDisplay.secondaryAction}
                variant="outline"
                onLogIn={handleLogIn}
                onSignUp={handleSignUp}
              />
            )}
          </div>

          {id && (
            <p className="text-muted-foreground text-sm">
              <Trans>Reference ID: {id}</Trans>
            </p>
          )}
        </div>
      </div>
    </main>
  );
}

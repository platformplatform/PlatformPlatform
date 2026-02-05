import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { ErrorCode } from "@repo/infrastructure/auth/AuthenticationMiddleware";
import { Button } from "@repo/ui/components/Button";
import { Link } from "@repo/ui/components/Link";
import { createFileRoute, Navigate, useNavigate } from "@tanstack/react-router";
import { AlertCircle, LogIn, LogOut, ShieldAlert } from "lucide-react";
import type { ReactNode } from "react";
import LocaleSwitcher from "@/federated-modules/common/LocaleSwitcher";
import SupportButton from "@/federated-modules/common/SupportButton";
import ThemeModeSelector from "@/federated-modules/common/ThemeModeSelector";
import logoMark from "@/shared/images/logo-mark.svg";
import logoWrap from "@/shared/images/logo-wrap.svg";

export const Route = createFileRoute("/error")({
  validateSearch: (search) => {
    const params = search as { error?: string; returnPath?: string };
    return {
      error: params.error,
      returnPath: params.returnPath?.startsWith("/") ? params.returnPath : undefined
    };
  },
  component: ErrorPage
});

function getErrorDisplay(error: string): {
  icon: ReactNode;
  iconBackground: string;
  title: ReactNode;
  message: ReactNode;
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
        )
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
        )
      };

    case ErrorCode.SessionNotFound:
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
        )
      };

    default:
      return {
        icon: <AlertCircle className="size-10 text-destructive" />,
        iconBackground: "bg-destructive/10",
        title: <Trans>Something went wrong</Trans>,
        message: (
          <Trans>An unexpected error occurred. Please try again or contact support if the problem persists.</Trans>
        )
      };
  }
}

function ErrorNavigation() {
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
      </div>
    </nav>
  );
}

function ErrorPage() {
  const { error, returnPath } = Route.useSearch();
  const navigate = useNavigate();

  if (!error) {
    return <Navigate to="/login" search={{ returnPath }} />;
  }

  const errorDisplay = getErrorDisplay(error);

  const handleLogIn = () => {
    navigate({ to: "/login", search: { returnPath } });
  };

  return (
    <main className="flex min-h-screen w-full flex-col bg-background">
      <ErrorNavigation />

      <div className="flex flex-1 flex-col items-center justify-center gap-8 px-6 pt-12 pb-32 text-center">
        <div className="flex max-w-lg flex-col items-center gap-6">
          <div className={`flex size-20 items-center justify-center rounded-full ${errorDisplay.iconBackground}`}>
            {errorDisplay.icon}
          </div>

          <div className="flex flex-col gap-3">
            <h1>{errorDisplay.title}</h1>
            <p className="text-lg text-muted-foreground">{errorDisplay.message}</p>
          </div>

          <div className="flex justify-center gap-3 pt-2">
            <Button variant="default" onClick={handleLogIn} aria-label={t`Log in`}>
              <LogIn size={16} />
              <Trans>Log in</Trans>
            </Button>
          </div>
        </div>
      </div>
    </main>
  );
}

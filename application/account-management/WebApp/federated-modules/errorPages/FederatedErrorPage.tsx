import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { applicationInsights } from "@repo/infrastructure/applicationInsights/ApplicationInsightsProvider";
import { AuthenticationContext } from "@repo/infrastructure/auth/AuthenticationProvider";
import { loginPath } from "@repo/infrastructure/auth/constants";
import { useIsAuthenticated, useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { Link } from "@repo/ui/components/Link";
import type { ErrorComponentProps } from "@tanstack/react-router";
import { AlertTriangle, Home, LogOut, RefreshCw } from "lucide-react";
import { useContext, useEffect, useState } from "react";
import logoMark from "@/shared/images/logo-mark.svg";
import logoWrap from "@/shared/images/logo-wrap.svg";
import LocaleSwitcher from "../common/LocaleSwitcher";
import SupportButton from "../common/SupportButton";
import ThemeModeSelector from "../common/ThemeModeSelector";
import "@repo/ui/tailwind.css";

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
      const response = await fetch("/api/account-management/authentication/logout", {
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

export default function FederatedErrorPage({ error }: Readonly<ErrorComponentProps>) {
  const [showDetails, setShowDetails] = useState(false);

  useEffect(() => {
    const exception = error instanceof Error ? error : new Error(String(error));

    applicationInsights.trackException({
      exception,
      properties: {
        component: "ErrorPage",
        url: globalThis.location.href,
        pathname: globalThis.location.pathname,
        errorName: exception.name,
        errorMessage: exception.message
      }
    });

    console.error(error);
  }, [error]);

  return (
    <main id="account-management" style={{ minHeight: "100vh" }} className="flex w-full flex-col bg-background">
      <ErrorNavigation />

      <div style={{ flex: 1 }} className="flex flex-col items-center justify-center gap-8 px-6 pt-12 pb-32 text-center">
        <div className="flex max-w-lg flex-col items-center gap-6">
          <div className="flex size-20 items-center justify-center rounded-full bg-destructive/10">
            <AlertTriangle className="size-10 text-destructive" />
          </div>

          <div className="flex flex-col gap-3">
            <h1>
              <Trans>Something went wrong</Trans>
            </h1>
            <p className="text-lg text-muted-foreground">
              <Trans>An unexpected error occurred while processing your request.</Trans>
              <br />
              <Trans>Please try again or return to the home page.</Trans>
            </p>
          </div>

          <div className="flex flex-wrap justify-center gap-3 pt-2">
            <Button
              onClick={() => {
                globalThis.location.reload();
              }}
            >
              <RefreshCw size={16} />
              <Trans>Try again</Trans>
            </Button>
            <Button
              variant="secondary"
              onClick={() => {
                globalThis.location.href = "/";
              }}
            >
              <Home size={16} />
              <Trans>Go to home</Trans>
            </Button>
          </div>

          {error?.message && (
            <div className="mt-4 w-full">
              <Button
                variant="ghost"
                onClick={() => setShowDetails(!showDetails)}
                className="text-muted-foreground text-sm"
              >
                {showDetails ? <Trans>Hide details</Trans> : <Trans>Show details</Trans>}
              </Button>
              {showDetails && (
                <div className="mt-3 rounded-lg border border-border bg-muted/50 p-4 text-left">
                  <p className="break-all font-mono text-muted-foreground text-sm">{error.message}</p>
                  {error.stack && (
                    <pre className="mt-2 max-h-40 overflow-auto font-mono text-muted-foreground text-xs">
                      {error.stack}
                    </pre>
                  )}
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </main>
  );
}

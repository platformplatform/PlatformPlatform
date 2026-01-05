import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AuthenticationContext } from "@repo/infrastructure/auth/AuthenticationProvider";
import { loginPath } from "@repo/infrastructure/auth/constants";
import { useIsAuthenticated, useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { Image } from "@repo/ui/components/Image";
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
        window.location.href = loginPath;
      }
    } catch {
      window.location.href = loginPath;
    }
  };

  return (
    <nav className="mx-auto flex w-full max-w-7xl items-center justify-between gap-4 px-6 pt-8 pb-4">
      <a href="/" className="flex items-center">
        <Image
          className="hidden h-10 sm:block"
          src={logoWrap}
          alt="PlatformPlatform Logo"
          width={280}
          height={40}
          priority={true}
        />
        <Image
          className="h-10 sm:hidden"
          src={logoMark}
          alt="PlatformPlatform Logo"
          width={40}
          height={40}
          priority={true}
        />
      </a>

      <div className="flex items-center gap-6">
        <span className="flex gap-2">
          <ThemeModeSelector />
          <SupportButton />
          <LocaleSwitcher />
        </span>
        {isAuthenticated && userInfo && (
          <Button variant="outline" onPress={handleLogout} isDisabled={isLoggingOut} aria-label={t`Log out`}>
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

export default function FederatedErrorPage({ error, reset }: Readonly<ErrorComponentProps>) {
  const [showDetails, setShowDetails] = useState(false);

  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <main id="account-management" style={{ minHeight: "100vh" }} className="flex w-full flex-col bg-background">
      <ErrorNavigation />

      <div style={{ flex: 1 }} className="flex flex-col items-center justify-center gap-8 px-6 pt-12 pb-32 text-center">
        <div className="flex max-w-lg flex-col items-center gap-6">
          <div className="flex h-20 w-20 items-center justify-center rounded-full bg-destructive/10">
            <AlertTriangle className="h-10 w-10 text-destructive" />
          </div>

          <div className="flex flex-col gap-3">
            <h1 className="font-bold text-3xl text-foreground">
              <Trans>Something went wrong</Trans>
            </h1>
            <p className="text-lg text-muted-foreground">
              <Trans>
                We encountered an unexpected error while processing your request. Please try again or return to the home
                page.
              </Trans>
            </p>
          </div>

          <div className="flex flex-wrap justify-center gap-3 pt-2">
            <Button variant="primary" onPress={reset}>
              <RefreshCw size={16} />
              <Trans>Try again</Trans>
            </Button>
            <Link
              href="/"
              variant="button"
              underline={false}
              className="h-10 rounded-lg border border-border px-4 text-foreground hover:bg-hover-background"
            >
              <Home size={16} />
              <Trans>Go to home</Trans>
            </Link>
          </div>

          {error?.message && (
            <div className="mt-4 w-full">
              <Button
                variant="ghost"
                onPress={() => setShowDetails(!showDetails)}
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

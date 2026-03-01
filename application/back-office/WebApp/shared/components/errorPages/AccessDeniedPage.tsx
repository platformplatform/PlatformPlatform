import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AuthenticationContext } from "@repo/infrastructure/auth/AuthenticationProvider";
import { loginPath } from "@repo/infrastructure/auth/constants";
import { useIsAuthenticated, useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { Link } from "@repo/ui/components/Link";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { Home, LogOut, ShieldX } from "lucide-react";
import { useContext, useState } from "react";
import logoMark from "@/shared/images/logo-mark.svg";
import logoWrap from "@/shared/images/logo-wrap.svg";

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

function AccessDeniedNavigation() {
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
      <Link href="/back-office" variant="logo" underline={false}>
        <img className="hidden h-10 w-[17.5rem] sm:block" src={logoWrap} alt={t`PlatformPlatform logo`} />
        <img className="size-10 sm:hidden" src={logoMark} alt={t`PlatformPlatform logo`} />
      </Link>

      <div className="flex items-center gap-6">
        {isAuthenticated && userInfo && (
          <Tooltip>
            <TooltipTrigger
              render={
                <Button variant="outline" onClick={handleLogout} disabled={isLoggingOut} aria-label={t`Log out`}>
                  <LogOut size={16} />
                  <span className="hidden sm:inline">
                    <Trans>Log out</Trans>
                  </span>
                </Button>
              }
            />
            <TooltipContent className="sm:hidden">
              <Trans>Log out</Trans>
            </TooltipContent>
          </Tooltip>
        )}
      </div>
    </nav>
  );
}

export function AccessDeniedPage() {
  return (
    <main id="back-office" style={{ minHeight: "100vh" }} className="flex w-full flex-col bg-background">
      <AccessDeniedNavigation />

      <div style={{ flex: 1 }} className="flex flex-col items-center justify-center gap-8 px-6 pt-12 pb-32 text-center">
        <div className="flex w-full max-w-lg flex-col items-center gap-6">
          <div className="flex size-20 items-center justify-center rounded-full bg-destructive/10">
            <ShieldX className="size-10 text-destructive" />
          </div>

          <div className="flex flex-col gap-3">
            <h1>
              <Trans>Access denied</Trans>
            </h1>
            <p className="text-lg text-muted-foreground">
              <Trans>You do not have permission to access this page.</Trans>
              <br />
              <Trans>Contact your administrator if you believe this is a mistake.</Trans>
            </p>
          </div>

          <div className="flex justify-center gap-3 pt-2">
            <Button
              variant="default"
              onClick={() => {
                globalThis.location.href = "/";
              }}
            >
              <Home size={16} />
              <Trans>Go to home</Trans>
            </Button>
          </div>
        </div>
      </div>
    </main>
  );
}

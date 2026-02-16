import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AuthenticationContext } from "@repo/infrastructure/auth/AuthenticationProvider";
import { loginPath } from "@repo/infrastructure/auth/constants";
import { useIsAuthenticated, useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { Link } from "@repo/ui/components/Link";
import { AlertTriangleIcon, LogOut } from "lucide-react";
import { useContext, useState } from "react";
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

function SuspendedNavigation() {
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

export default function SuspendedPage() {
  const userInfo = useUserInfo();
  const isOwner = userInfo?.role === "Owner";

  return (
    <main id="account" style={{ minHeight: "100vh" }} className="flex w-full flex-col bg-background">
      <SuspendedNavigation />

      <div style={{ flex: 1 }} className="flex flex-col items-center justify-center gap-8 px-6 pt-12 pb-32 text-center">
        <div className="flex w-full max-w-lg flex-col items-center gap-6">
          <div className="flex size-20 items-center justify-center rounded-full bg-destructive/10">
            <AlertTriangleIcon className="size-10 text-destructive" />
          </div>

          <div className="flex flex-col gap-3">
            <h1>
              <Trans>Account suspended</Trans>
            </h1>
            <p className="text-lg text-muted-foreground">
              {isOwner ? (
                <Trans>
                  Your account has been suspended. Please visit the subscription page to resolve any issues and restore
                  access.
                </Trans>
              ) : (
                <Trans>Your account has been suspended. Please contact the account owner to restore access.</Trans>
              )}
            </p>
          </div>

          {isOwner && (
            <div className="flex justify-center gap-3 pt-2">
              <Button
                onClick={() => {
                  globalThis.location.href = "/account/subscription";
                }}
              >
                <Trans>Manage subscription</Trans>
              </Button>
            </div>
          )}
        </div>
      </div>
    </main>
  );
}

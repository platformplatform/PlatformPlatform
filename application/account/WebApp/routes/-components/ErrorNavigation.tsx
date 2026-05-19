import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AuthenticationContext } from "@repo/infrastructure/auth/AuthenticationProvider";
import { loginPath } from "@repo/infrastructure/auth/constants";
import { useIsAuthenticated, useUserInfo } from "@repo/infrastructure/auth/hooks";
import { productName } from "@repo/infrastructure/branding";
import { Button } from "@repo/ui/components/Button";
import { Link } from "@repo/ui/components/Link";
import { Logo } from "@repo/ui/components/Logo";
import { LogIn, LogOut, UserPlus } from "lucide-react";
import { useContext, useState } from "react";

import LocaleSwitcher from "@/federated-modules/common/LocaleSwitcher";
import SupportButton from "@/federated-modules/common/SupportButton";
import ThemeModeSelector from "@/federated-modules/common/ThemeModeSelector";

import type { ErrorAction } from "./errorDisplay";

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

export function ErrorNavigation() {
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
      <Link href="/" variant="logo" underline={false} className="shrink-0">
        <Logo variant="wordmark" className="hidden h-10 w-auto sm:block" alt={t`${productName} logo`} />
        <Logo variant="mark" className="size-10 sm:hidden" alt={t`${productName} logo`} />
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

export function ActionButton({ action, variant, onLogIn, onSignUp }: ActionButtonProps) {
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

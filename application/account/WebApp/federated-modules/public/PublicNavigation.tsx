import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { loginPath, signUpPath } from "@repo/infrastructure/auth/constants";
import { useIsAuthenticated } from "@repo/infrastructure/auth/hooks";
import { Link } from "@repo/ui/components/Link";
import { Suspense } from "react";
import LocaleSwitcher from "@/federated-modules/common/LocaleSwitcher";
import ThemeModeSelector from "@/federated-modules/common/ThemeModeSelector";
import UserMenu from "@/federated-modules/userMenu/UserMenu";
import logoMark from "@/shared/images/logo-mark.svg";
import logoWrap from "@/shared/images/logo-wrap.svg";

export default function PublicNavigation() {
  const isAuthenticated = useIsAuthenticated();

  return (
    <nav className="mx-auto flex w-full max-w-7xl items-center justify-between gap-4 px-6 pt-8 pb-4">
      <Link href="/" variant="logo" underline={false}>
        <img className="hidden h-10 w-[17.5rem] sm:block" src={logoWrap} alt={t`PlatformPlatform logo`} />
        <img className="size-10 sm:hidden" src={logoMark} alt={t`PlatformPlatform logo`} />
      </Link>

      {isAuthenticated ? (
        <Suspense fallback={<div className="h-10" />}>
          <UserMenu />
        </Suspense>
      ) : (
        <div className="flex items-center gap-6">
          <Suspense fallback={<div className="flex gap-2" />}>
            <span className="flex gap-2">
              <LocaleSwitcher />
              <ThemeModeSelector aria-label={t`Change theme`} />
            </span>
          </Suspense>
          <span className="flex gap-2">
            <Link href={loginPath} variant="button-secondary" underline={false} className="h-10 px-4">
              <Trans>Log in</Trans>
            </Link>
            <Link href={signUpPath} variant="button-primary" underline={false} className="h-10 px-4">
              <Trans>Sign up</Trans>
            </Link>
          </span>
        </div>
      )}
    </nav>
  );
}

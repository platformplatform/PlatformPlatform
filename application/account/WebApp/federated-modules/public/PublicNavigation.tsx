import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { loginPath, signUpPath } from "@repo/infrastructure/auth/constants";
import { useIsAuthenticated } from "@repo/infrastructure/auth/hooks";
import { productName } from "@repo/infrastructure/branding";
import { Link } from "@repo/ui/components/Link";
import { Logo } from "@repo/ui/components/Logo";
import { Suspense } from "react";

import LocaleSwitcher from "@/federated-modules/common/LocaleSwitcher";
import ThemeModeSelector from "@/federated-modules/common/ThemeModeSelector";
import UserMenu from "@/federated-modules/userMenu/UserMenu";

export default function PublicNavigation() {
  const isAuthenticated = useIsAuthenticated();

  return (
    <nav className="mx-auto flex w-full max-w-[66rem] items-center justify-between gap-4 px-4 pt-8 pb-4">
      <Link href="/" variant="logo" underline={false} className="shrink-0">
        <Logo variant="wordmark" className="hidden h-10 w-auto sm:block" alt={t`${productName} logo`} />
        <Logo variant="mark" className="size-10 sm:hidden" alt={t`${productName} logo`} />
      </Link>

      {isAuthenticated ? (
        <Suspense fallback={<div className="h-10" />}>
          <UserMenu />
        </Suspense>
      ) : (
        <div className="flex items-center gap-6">
          <Suspense fallback={<div className="flex" />}>
            <span className="flex">
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

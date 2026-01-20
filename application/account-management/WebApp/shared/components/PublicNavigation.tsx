import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { loginPath, signUpPath } from "@repo/infrastructure/auth/constants";
import { useIsAuthenticated } from "@repo/infrastructure/auth/hooks";
import { Link } from "@repo/ui/components/Link";
import LocaleSwitcher from "@/federated-modules/common/LocaleSwitcher";
import SupportButton from "@/federated-modules/common/SupportButton";
import ThemeModeSelector from "@/federated-modules/common/ThemeModeSelector";
import AvatarButton from "@/federated-modules/topMenu/AvatarButton";
import logoMark from "@/shared/images/logo-mark.svg";
import logoWrap from "@/shared/images/logo-wrap.svg";

export function PublicNavigation() {
  const isAuthenticated = useIsAuthenticated();

  return (
    <nav className="mx-auto flex w-full max-w-7xl items-center justify-between gap-4 px-6 pt-8 pb-4">
      {/* Logo */}
      <a href="/" className="flex items-center">
        <img className="hidden h-10 sm:block" src={logoWrap} alt={t`PlatformPlatform logo`} width={280} height={40} />
        <img className="h-10 sm:hidden" src={logoMark} alt={t`PlatformPlatform logo`} width={40} height={40} />
      </a>

      {/* Right side actions */}
      {isAuthenticated ? (
        <div className="flex items-center gap-6">
          <span className="flex gap-2">
            <ThemeModeSelector />
            <SupportButton />
            <LocaleSwitcher />
          </span>
          <AvatarButton />
        </div>
      ) : (
        <div className="flex items-center gap-6">
          <span className="flex gap-2">
            <LocaleSwitcher />
            <ThemeModeSelector aria-label={t`Change theme`} />
          </span>
          <span className="flex gap-2">
            <Link
              href={loginPath}
              variant="button"
              underline={false}
              className="h-10 rounded-lg px-4 text-foreground hover:bg-hover-background"
            >
              <Trans>Log in</Trans>
            </Link>
            <Link
              href={signUpPath}
              variant="button"
              underline={false}
              className="h-10 rounded-lg bg-primary px-4 text-primary-foreground hover:bg-primary/95"
            >
              <Trans>Sign up</Trans>
            </Link>
          </span>
        </div>
      )}
    </nav>
  );
}

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { productName } from "@repo/infrastructure/branding";
import { Button } from "@repo/ui/components/Button";
import { Link } from "@repo/ui/components/Link";
import { Logo } from "@repo/ui/components/Logo";
import { HomeIcon, LogOutIcon, ShieldXIcon } from "lucide-react";

function AccessDeniedNavigation() {
  return (
    <nav className="mx-auto flex w-full max-w-7xl items-center justify-between gap-4 px-6 pt-8 pb-4">
      <Link href="/" variant="logo" underline={false} className="shrink-0">
        <Logo variant="wordmark" className="hidden h-10 w-auto sm:block" alt={t`${productName} logo`} />
        <Logo variant="mark" className="size-10 sm:hidden" alt={t`${productName} logo`} />
      </Link>

      <div className="flex items-center gap-6">
        <Button
          variant="outline"
          aria-label={t`Log out`}
          onClick={() => {
            globalThis.location.href = "/.auth/logout";
          }}
        >
          <LogOutIcon size={16} />
          <span className="hidden sm:inline">
            <Trans>Log out</Trans>
          </span>
        </Button>
      </div>
    </nav>
  );
}

export function AccessDeniedPage() {
  return (
    <main className="flex min-h-screen w-full flex-col bg-background">
      <AccessDeniedNavigation />

      <div className="flex flex-1 flex-col items-center justify-center gap-8 px-6 pt-12 pb-32 text-center">
        <div className="flex w-full max-w-[32rem] flex-col items-center gap-6">
          <div className="flex size-20 items-center justify-center rounded-full bg-destructive/10">
            <ShieldXIcon className="size-10 text-destructive" />
          </div>

          <div className="flex flex-col gap-3">
            <h1>
              <Trans>No back-office access</Trans>
            </h1>
            <p className="text-lg text-muted-foreground">
              <Trans>Your account is not in the required group.</Trans>
              <br />
              <Trans>Contact your administrator.</Trans>
            </p>
          </div>

          <div className="flex justify-center gap-3 pt-2">
            <Button
              variant="default"
              onClick={() => {
                globalThis.location.href = "/";
              }}
            >
              <HomeIcon size={16} />
              <Trans>Go to home</Trans>
            </Button>
          </div>
        </div>
      </div>
    </main>
  );
}

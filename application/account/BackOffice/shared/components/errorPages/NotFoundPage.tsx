import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { productName } from "@repo/infrastructure/branding";
import { Button } from "@repo/ui/components/Button";
import { Link } from "@repo/ui/components/Link";
import { Logo } from "@repo/ui/components/Logo";
import { FileQuestionIcon, HomeIcon } from "lucide-react";

function NotFoundNavigation() {
  return (
    <nav className="mx-auto flex w-full max-w-7xl items-center justify-between gap-4 px-6 pt-8 pb-4">
      <Link href="/" variant="logo" underline={false} className="shrink-0">
        <Logo variant="wordmark" className="hidden h-10 w-auto sm:block" alt={t`${productName} logo`} />
        <Logo variant="mark" className="size-10 sm:hidden" alt={t`${productName} logo`} />
      </Link>
    </nav>
  );
}

export function NotFoundPage() {
  return (
    <main className="flex min-h-screen w-full flex-col bg-background">
      <NotFoundNavigation />

      <div className="flex flex-1 flex-col items-center justify-center gap-8 px-6 pt-12 pb-32 text-center">
        <div className="flex w-full max-w-[32rem] flex-col items-center gap-6">
          <div className="flex size-20 items-center justify-center rounded-full bg-muted">
            <FileQuestionIcon className="size-10 text-muted-foreground" />
          </div>

          <div className="flex flex-col gap-3">
            <h1>
              <Trans>Page not found</Trans>
            </h1>
            <p className="text-lg text-muted-foreground">
              <Trans>The page you are looking for does not exist or was moved.</Trans>
              <br />
              <Trans>Please check the URL or return to the home page.</Trans>
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

import type { ErrorComponentProps } from "@tanstack/react-router";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { applicationInsights } from "@repo/infrastructure/applicationInsights/ApplicationInsightsProvider";
import { isAccessDeniedError, isNotFoundError } from "@repo/infrastructure/auth/routeGuards";
import { productName } from "@repo/infrastructure/branding";
import { Button } from "@repo/ui/components/Button";
import { Link } from "@repo/ui/components/Link";
import { Logo } from "@repo/ui/components/Logo";
import { AlertTriangleIcon, HomeIcon, RefreshCwIcon } from "lucide-react";
import { useEffect, useState } from "react";

import { AccessDeniedPage } from "./AccessDeniedPage";
import { NotFoundPage } from "./NotFoundPage";

function ErrorNavigation() {
  return (
    <nav className="mx-auto flex w-full max-w-7xl items-center justify-between gap-4 px-6 pt-8 pb-4">
      <Link href="/" variant="logo" underline={false} className="shrink-0">
        <Logo variant="wordmark" className="hidden h-10 w-auto sm:block" alt={t`${productName} logo`} />
        <Logo variant="mark" className="size-10 sm:hidden" alt={t`${productName} logo`} />
      </Link>
    </nav>
  );
}

export function ErrorPage(props: Readonly<ErrorComponentProps>) {
  if (isNotFoundError(props.error)) {
    return <NotFoundPage />;
  }

  if (isAccessDeniedError(props.error)) {
    return <AccessDeniedPage />;
  }

  return <GeneralErrorPage {...props} />;
}

function GeneralErrorPage({ error }: Readonly<ErrorComponentProps>) {
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
    <main className="flex min-h-screen w-full flex-col bg-background">
      <ErrorNavigation />

      <div className="flex flex-1 flex-col items-center justify-center gap-8 px-6 pt-12 pb-32 text-center">
        <div className="flex w-full max-w-[32rem] flex-col items-center gap-6">
          <div className="flex size-20 items-center justify-center rounded-full bg-destructive/10">
            <AlertTriangleIcon className="size-10 text-destructive" />
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
              <RefreshCwIcon size={16} />
              <Trans>Try again</Trans>
            </Button>
            <Button
              variant="secondary"
              onClick={() => {
                globalThis.location.href = "/";
              }}
            >
              <HomeIcon size={16} />
              <Trans>Go to home</Trans>
            </Button>
          </div>

          {error?.message && (
            <div className="mt-4 w-full">
              <Button
                variant="ghost"
                onClick={() => setShowDetails(!showDetails)}
                className="text-sm text-muted-foreground"
              >
                {showDetails ? <Trans>Hide details</Trans> : <Trans>Show details</Trans>}
              </Button>
              {showDetails && (
                <div className="mt-3 rounded-lg border border-border bg-muted/50 p-4 text-left">
                  <p className="font-mono text-sm break-all text-muted-foreground">{error.message}</p>
                  {error.stack && (
                    <pre className="mt-2 max-h-40 overflow-auto font-mono text-xs text-muted-foreground">
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

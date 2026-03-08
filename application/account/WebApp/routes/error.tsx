import { Trans } from "@lingui/react/macro";
import { trackInteraction } from "@repo/infrastructure/applicationInsights/ApplicationInsightsProvider";
import { signUpPath } from "@repo/infrastructure/auth/constants";
import { isValidReturnPath } from "@repo/infrastructure/auth/util";
import { createFileRoute, Navigate, useNavigate } from "@tanstack/react-router";
import { useRef } from "react";

import { errorLabelMap, getErrorDisplay } from "./-components/errorDisplay";
import { ActionButton, ErrorNavigation } from "./-components/ErrorNavigation";

export const Route = createFileRoute("/error")({
  staticData: {},
  validateSearch: (search) => {
    const params = search as { error?: string; returnPath?: string; id?: string };
    return {
      error: params.error,
      returnPath: params.returnPath && isValidReturnPath(params.returnPath) ? params.returnPath : undefined,
      id: params.id && /^[a-zA-Z0-9-]+$/.test(params.id) ? params.id : undefined
    };
  },
  component: ErrorPage
});

function ErrorPage() {
  const { error, returnPath, id } = Route.useSearch();
  const navigate = useNavigate();
  const trackedError = useRef<string | null>(null);

  if (error && trackedError.current !== error) {
    trackedError.current = error;
    const label = errorLabelMap[error] ?? "Unknown error";
    trackInteraction("Error", "interaction", label);
  }

  if (!error) {
    return <Navigate to="/login" search={{ returnPath }} />;
  }

  const errorDisplay = getErrorDisplay(error);

  const handleLogIn = () => {
    navigate({ to: "/login", search: { returnPath } });
  };

  const handleSignUp = () => {
    navigate({ to: signUpPath });
  };

  return (
    <main style={{ minHeight: "100vh" }} className="flex w-full flex-col bg-background">
      <ErrorNavigation />

      <div style={{ flex: 1 }} className="flex flex-col items-center justify-center gap-8 px-6 pt-12 pb-32 text-center">
        <div className="flex w-full max-w-lg flex-col items-center gap-6">
          <div className={`flex size-20 items-center justify-center rounded-full ${errorDisplay.iconBackground}`}>
            {errorDisplay.icon}
          </div>

          <div className="flex flex-col gap-3">
            <h1>{errorDisplay.title}</h1>
            <p className="text-lg text-muted-foreground">{errorDisplay.message}</p>
          </div>

          <div className="flex flex-wrap justify-center gap-3 pt-2">
            <ActionButton
              action={errorDisplay.action}
              variant="default"
              onLogIn={handleLogIn}
              onSignUp={handleSignUp}
            />
            {errorDisplay.secondaryAction && (
              <ActionButton
                action={errorDisplay.secondaryAction}
                variant="outline"
                onLogIn={handleLogIn}
                onSignUp={handleSignUp}
              />
            )}
          </div>

          {id && (
            <p className="text-sm text-muted-foreground">
              <Trans>Reference ID: {id}</Trans>
            </p>
          )}
        </div>
      </div>
    </main>
  );
}

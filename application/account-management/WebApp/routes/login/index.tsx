import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { loggedInPath, signUpPath } from "@repo/infrastructure/auth/constants";
import { useIsAuthenticated } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { Link } from "@repo/ui/components/Link";
import { TextField } from "@repo/ui/components/TextField";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { createFileRoute, Navigate } from "@tanstack/react-router";
import { useState } from "react";
import FederatedErrorPage from "@/federated-modules/errorPages/FederatedErrorPage";
import logoMarkUrl from "@/shared/images/logo-mark.svg";
import logoWrapUrl from "@/shared/images/logo-wrap.svg";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { api } from "@/shared/lib/api/client";
import { getSignupState } from "../signup/-shared/signupState";
import { clearLoginState, getLoginState, setLoginState } from "./-shared/loginState";

export const Route = createFileRoute("/login/")({
  validateSearch: (search) => {
    const returnPath = search.returnPath as string | undefined;
    // Only allow paths starting with / to prevent open redirect attacks to external domains
    return {
      returnPath: returnPath?.startsWith("/") ? returnPath : undefined
    };
  },
  component: function LoginRoute() {
    const isAuthenticated = useIsAuthenticated();

    if (isAuthenticated) {
      return <Navigate to={loggedInPath} />;
    }

    return (
      <HorizontalHeroLayout>
        <LoginForm />
      </HorizontalHeroLayout>
    );
  },
  errorComponent: FederatedErrorPage
});

export function LoginForm() {
  const { email: savedEmail } = getLoginState();
  const { email: signupEmail } = getSignupState(); // Prefill from signup page if user navigated here
  const [email, setEmail] = useState(savedEmail || signupEmail || "");
  const { returnPath } = Route.useSearch();

  const startLoginMutation = api.useMutation("post", "/api/account-management/authentication/login/start");

  if (startLoginMutation.isSuccess) {
    const { loginId, emailConfirmationId, validForSeconds } = startLoginMutation.data;

    clearLoginState();
    setLoginState({
      loginId,
      emailConfirmationId,
      email,
      expireAt: new Date(Date.now() + validForSeconds * 1000)
    });

    return <Navigate to="/login/verify" search={{ returnPath }} />;
  }

  return (
    <Form
      onSubmit={mutationSubmitter(startLoginMutation)}
      validationErrors={startLoginMutation.error?.errors}
      validationBehavior="aria"
      className="flex w-full max-w-sm flex-col items-center gap-4 space-y-3 px-6 pt-8 pb-4"
    >
      <Link href="/" className="cursor-pointer">
        <img src={logoMarkUrl} className="h-12 w-12" alt={t`Logo`} />
      </Link>
      <h2>
        <Trans>Hi! Welcome back</Trans>
      </h2>
      <div className="text-center text-muted-foreground text-sm">
        <Trans>Enter your email below to log in</Trans>
      </div>
      <TextField
        name="email"
        type="email"
        label={t`Email`}
        autoFocus={true}
        isRequired={true}
        value={email}
        onChange={setEmail}
        autoComplete="email webauthn"
        placeholder={t`yourname@example.com`}
        className="flex w-full flex-col"
      />
      <Button type="submit" disabled={startLoginMutation.isPending} className="mt-4 w-full text-center">
        {startLoginMutation.isPending ? <Trans>Sending verification code...</Trans> : <Trans>Continue</Trans>}
      </Button>
      <p className="text-muted-foreground text-sm">
        <Trans>
          Don't have an account? <Link href={signUpPath}>Create one</Link>
        </Trans>
      </p>
      {/*
        Built with PlatformPlatform - https://github.com/platformplatform/PlatformPlatform
        We'd appreciate it if you keep this attribution to help others discover this free, open-source platform. Thank you! 🙏
      */}
      <div className="flex flex-col items-center gap-1">
        <span className="text-muted-foreground text-xs">
          <Trans>Built with</Trans>
        </span>
        <Link href="https://github.com/platformplatform/PlatformPlatform" className="cursor-pointer">
          <img src={logoWrapUrl} alt={t`PlatformPlatform`} className="h-6 w-auto" />
        </Link>
      </div>
    </Form>
  );
}

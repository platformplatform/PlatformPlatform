import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { signUpPath } from "@repo/infrastructure/auth/constants";
import { isValidReturnPath } from "@repo/infrastructure/auth/util";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { Link } from "@repo/ui/components/Link";
import { TextField } from "@repo/ui/components/TextField";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { createFileRoute, Navigate } from "@tanstack/react-router";
import { useEffect, useState } from "react";
import ErrorPage from "@/federated-modules/errorPages/ErrorPage";
import { useMainNavigation } from "@/shared/hooks/useMainNavigation";
import googleIconUrl from "@/shared/images/google-icon.svg";
import logoMarkUrl from "@/shared/images/logo-mark.svg";
import logoWrapUrl from "@/shared/images/logo-wrap.svg";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { api } from "@/shared/lib/api/client";
import { getSignupState } from "../signup/-shared/signupState";
import { clearLoginState, getLoginState, setLoginState } from "./-shared/loginState";

export const Route = createFileRoute("/login/")({
  validateSearch: (search) => {
    const returnPath = search.returnPath as string | undefined;
    return {
      returnPath: returnPath && isValidReturnPath(returnPath) ? returnPath : undefined
    };
  },
  component: function LoginRoute() {
    const { isAuthenticated } = import.meta.user_info_env;
    const { navigateToHome } = useMainNavigation();

    useEffect(() => {
      if (isAuthenticated) {
        navigateToHome();
      }
    }, [isAuthenticated, navigateToHome]);

    if (isAuthenticated) {
      return null;
    }

    return (
      <HorizontalHeroLayout>
        <LoginForm />
      </HorizontalHeroLayout>
    );
  },
  errorComponent: ErrorPage
});

export function LoginForm() {
  const { email: savedEmail } = getLoginState();
  const { email: signupEmail } = getSignupState(); // Prefill from signup page if user navigated here
  const [email, setEmail] = useState(savedEmail || signupEmail || "");
  const { returnPath } = Route.useSearch();

  const startLoginMutation = api.useMutation("post", "/api/account/authentication/email/login/start");
  const [isGoogleLoginPending, setIsGoogleLoginPending] = useState(false);

  const handleGoogleLogin = () => {
    setIsGoogleLoginPending(true);
    const params = new URLSearchParams();
    if (returnPath) {
      params.set("ReturnPath", returnPath);
    }
    try {
      const preferredTenantId = localStorage.getItem("preferred-tenant");
      if (preferredTenantId) {
        params.set("PreferredTenantId", preferredTenantId);
      }
    } catch {
      // Ignore localStorage errors
    }
    const queryString = params.toString();
    window.location.href = `/api/account/authentication/Google/login/start${queryString ? `?${queryString}` : ""}`;
  };

  if (startLoginMutation.isSuccess) {
    const { emailLoginId, validForSeconds } = startLoginMutation.data;

    clearLoginState();
    setLoginState({
      emailLoginId,
      email,
      expireAt: new Date(Date.now() + validForSeconds * 1000)
    });

    return <Navigate to="/login/verify" search={{ returnPath }} />;
  }

  const isPending = startLoginMutation.isPending || isGoogleLoginPending;

  return (
    <Form
      onSubmit={mutationSubmitter(startLoginMutation)}
      validationErrors={startLoginMutation.error?.errors}
      validationBehavior="aria"
      className="flex w-full max-w-[22rem] flex-col items-center gap-4 pt-8 pb-4"
    >
      <Link href="/" className="cursor-pointer">
        <img src={logoMarkUrl} className="size-12" alt={t`Logo`} />
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
        isDisabled={isPending}
      />
      <Button type="submit" disabled={isPending} className="mt-4 w-full text-center">
        {startLoginMutation.isPending ? <Trans>Sending verification code...</Trans> : <Trans>Log in with email</Trans>}
      </Button>
      {import.meta.runtime_env.PUBLIC_GOOGLE_OAUTH_ENABLED === "true" && (
        <>
          <div className="flex w-full items-center gap-4">
            <div className="h-px flex-1 bg-border" />
            <span className="text-muted-foreground text-sm">
              <Trans>or</Trans>
            </span>
            <div className="h-px flex-1 bg-border" />
          </div>
          <Button
            type="button"
            variant="outline"
            className="w-full"
            onClick={handleGoogleLogin}
            disabled={isPending}
            aria-busy={isGoogleLoginPending}
          >
            <img src={googleIconUrl} alt="" aria-hidden="true" className="size-5" />
            {isGoogleLoginPending ? <Trans>Redirecting...</Trans> : <Trans>Log in with Google</Trans>}
          </Button>
        </>
      )}
      <p className="text-muted-foreground text-sm">
        <Trans>
          Don't have an account?{" "}
          <Link href={signUpPath} aria-label={t`Create new account`}>
            Create one
          </Link>
        </Trans>
      </p>
      {/*
        Built with PlatformPlatform - https://github.com/platformplatform/PlatformPlatform
        We'd appreciate it if you keep this attribution to help others discover this free, open-source platform. Thank you! 🙏
      */}
      <div className="flex flex-col items-center gap-1">
        <span className="text-muted-foreground text-sm">
          <Trans>Built with</Trans>
        </span>
        <Link href="https://github.com/platformplatform/PlatformPlatform" className="cursor-pointer">
          <img src={logoWrapUrl} alt={t`PlatformPlatform`} className="h-6 w-auto" />
        </Link>
      </div>
    </Form>
  );
}

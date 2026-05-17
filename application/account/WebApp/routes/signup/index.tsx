import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { loginPath } from "@repo/infrastructure/auth/constants";
import { productName } from "@repo/infrastructure/branding";
import { useFeatureFlag } from "@repo/infrastructure/featureFlags/useFeatureFlag";
import { preferredLocaleKey } from "@repo/infrastructure/translations/constants";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { Link } from "@repo/ui/components/Link";
import { Logo } from "@repo/ui/components/Logo";
import { TextField } from "@repo/ui/components/TextField";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { createFileRoute, Navigate } from "@tanstack/react-router";
import { DotIcon } from "lucide-react";
import { useEffect, useState } from "react";

import ErrorPage from "@/federated-modules/errorPages/ErrorPage";
import { useMainNavigation } from "@/shared/hooks/useMainNavigation";
import googleIconUrl from "@/shared/images/google-icon.svg";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { api } from "@/shared/lib/api/client";

import { getLoginState } from "../login/-shared/loginState";
import { clearSignupState, getSignupState, setSignupState } from "./-shared/signupState";

export const Route = createFileRoute("/signup/")({
  staticData: { trackingTitle: "Sign up" },
  beforeLoad: () => ({ disableAuthSync: true }),
  component: function SignupRoute() {
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
        <StartSignupForm />
      </HorizontalHeroLayout>
    );
  },
  errorComponent: ErrorPage
});

export function StartSignupForm() {
  const { email: savedEmail } = getSignupState();
  const { email: loginEmail } = getLoginState(); // Prefill from login page if user navigated here
  const [email, setEmail] = useState(savedEmail || loginEmail || "");
  const { enabled: isGoogleOAuthEnabled } = useFeatureFlag("google-oauth");

  const startSignupMutation = api.useMutation("post", "/api/account/authentication/email/signup/start");
  const [isGoogleSignupPending, setIsGoogleSignupPending] = useState(false);

  const handleGoogleSignup = () => {
    setIsGoogleSignupPending(true);
    const locale = localStorage.getItem(preferredLocaleKey);
    const params = new URLSearchParams();
    if (locale) {
      params.set("Locale", locale);
    }
    const queryString = params.toString();
    window.location.href = `/api/account/authentication/Google/signup/start${queryString ? `?${queryString}` : ""}`;
  };

  if (startSignupMutation.isSuccess) {
    const { emailLoginId, validForSeconds } = startSignupMutation.data;

    clearSignupState();
    setSignupState({
      emailLoginId,
      email,
      expireAt: new Date(Date.now() + validForSeconds * 1000)
    });

    return <Navigate to="/signup/verify" />;
  }

  const isPending = startSignupMutation.isPending || isGoogleSignupPending;

  return (
    <Form
      onSubmit={mutationSubmitter(startSignupMutation)}
      validationErrors={startSignupMutation.error?.errors}
      validationBehavior="aria"
      className="flex w-full max-w-[22rem] flex-col items-center gap-4 rounded-lg pt-8 pb-4"
    >
      <Link href="/" className="cursor-pointer">
        <Logo variant="mark" className="size-12" alt={t`Logo`} />
      </Link>
      <h2>
        <Trans>Create your account</Trans>
      </h2>
      <div className="text-center text-sm text-muted-foreground">
        <Trans>Sign up in seconds to start building on {productName} – just like thousands of others.</Trans>
      </div>
      <TextField
        name="email"
        type="email"
        label={t`Email`}
        autoFocus={true}
        required={true}
        value={email}
        onChange={setEmail}
        autoComplete="email webauthn"
        placeholder={t`yourname@example.com`}
        className="flex w-full flex-col"
        disabled={isPending}
      />
      <Button
        type="submit"
        isPending={startSignupMutation.isPending}
        disabled={isPending}
        className="mt-4 w-full text-center"
      >
        {startSignupMutation.isPending ? (
          <Trans>Sending verification code...</Trans>
        ) : (
          <Trans>Sign up with email</Trans>
        )}
      </Button>
      {isGoogleOAuthEnabled && (
        <>
          <div className="flex w-full items-center gap-4">
            <div className="h-px flex-1 bg-border" />
            <span className="text-sm text-muted-foreground">
              <Trans>or</Trans>
            </span>
            <div className="h-px flex-1 bg-border" />
          </div>
          <Button
            type="button"
            variant="outline"
            className="w-full"
            onClick={handleGoogleSignup}
            isPending={isGoogleSignupPending}
            disabled={isPending}
          >
            {!isGoogleSignupPending && <img src={googleIconUrl} alt="" aria-hidden="true" className="size-5" />}
            {isGoogleSignupPending ? <Trans>Redirecting...</Trans> : <Trans>Sign up with Google</Trans>}
          </Button>
        </>
      )}
      <p className="text-sm text-muted-foreground">
        <Trans>Do you already have an account?</Trans>{" "}
        <Link href={loginPath}>
          <Trans>Log in</Trans>
        </Link>
      </p>
      <div className="text-center text-sm text-muted-foreground">
        <Trans>By continuing, you accept our policies</Trans>
        <div className="flex flex-wrap items-center justify-center">
          <Link href="/legal/terms">
            <Trans>Terms of use</Trans>
          </Link>
          <DotIcon className="size-4" />
          <Link href="/legal/privacy">
            <Trans>Privacy policies</Trans>
          </Link>
        </div>
      </div>
    </Form>
  );
}

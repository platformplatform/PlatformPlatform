import { ErrorMessage } from "@/shared/components/ErrorMessage";
import logoMarkUrl from "@/shared/images/logo-mark.svg";
import poweredByUrl from "@/shared/images/powered-by.svg";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { api } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { loggedInPath } from "@repo/infrastructure/auth/constants";
import { useIsAuthenticated } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { DigitPattern } from "@repo/ui/components/Digit";
import { Form } from "@repo/ui/components/Form";
import { Link } from "@repo/ui/components/Link";
import { OneTimeCodeInput, type OneTimeCodeInputRef } from "@repo/ui/components/OneTimeCodeInput";
import { toastQueue } from "@repo/ui/components/Toast";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { useExpirationTimeout } from "@repo/ui/hooks/useExpiration";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useEffect, useRef, useState } from "react";
import { clearLoginState, getLoginState, hasLoginState, setLoginState } from "./-shared/loginState";

export const Route = createFileRoute("/login/verify")({
  validateSearch: (search) => {
    const returnPath = search.returnPath as string | undefined;
    // Only allow paths starting with / to prevent open redirect attacks to external domains
    return {
      returnPath: returnPath?.startsWith("/") ? returnPath : undefined
    };
  },
  component: function LoginVerifyRoute() {
    const navigate = useNavigate();
    const isAuthenticated = useIsAuthenticated();
    const [hasShownToast, setHasShownToast] = useState(false);

    useEffect(() => {
      if (isAuthenticated) {
        navigate({ to: loggedInPath });
        return;
      }

      if (!hasLoginState() && !hasShownToast) {
        navigate({ to: "/login", search: { returnPath: undefined } });
        toastQueue.add({
          title: "No active login session",
          description: "Please start the login process again.",
          variant: "warning"
        });
        setHasShownToast(true);
      }
    }, [isAuthenticated, navigate, hasShownToast]);

    if (!hasLoginState()) {
      return null;
    }

    return (
      <HorizontalHeroLayout>
        <CompleteLoginForm />
      </HorizontalHeroLayout>
    );
  },
  errorComponent: (props) => {
    return (
      <HorizontalHeroLayout>
        <ErrorMessage {...props} />
      </HorizontalHeroLayout>
    );
  }
});

export function CompleteLoginForm() {
  const loginState = getLoginState();
  const { loginId, emailConfirmationId, email, expireAt } = loginState;
  const { expiresInString, isExpired } = useExpirationTimeout(expireAt);
  const { returnPath } = Route.useSearch();

  const oneTimeCodeInputRef = useRef<OneTimeCodeInputRef>(null);
  const [isOneTimeCodeComplete, setIsOneTimeCodeComplete] = useState(false);

  const completeLoginMutation = api.useMutation("post", "/api/account-management/authentication/login/{id}/complete");

  useEffect(() => {
    if (completeLoginMutation.isSuccess) {
      clearLoginState();
      window.location.href = returnPath ?? loggedInPath;
    }
  }, [completeLoginMutation.isSuccess, returnPath]);

  const resendLoginCodeMutation = api.useMutation(
    "post",
    "/api/account-management/authentication/login/{emailConfirmationId}/resend-code"
  );

  useEffect(() => {
    if (resendLoginCodeMutation.isSuccess && resendLoginCodeMutation.data) {
      setLoginState({
        ...loginState,
        expireAt: new Date(Date.now() + resendLoginCodeMutation.data.validForSeconds * 1000)
      });
      toastQueue.add({
        title: t`Verification code sent`,
        description: t`A new verification code has been sent to your email.`,
        variant: "success"
      });
    }
  }, [resendLoginCodeMutation.isSuccess, resendLoginCodeMutation.data, loginState]);

  useEffect(() => {
    if (isExpired) {
      clearLoginState();
      window.location.href = "/login/expired";
    }
  }, [isExpired]);

  // Focus first input after validation error
  useEffect(() => {
    if (completeLoginMutation.error) {
      oneTimeCodeInputRef.current?.focus();
    }
  }, [completeLoginMutation.error]);

  return (
    <div className="w-full max-w-sm space-y-3">
      <Form
        onSubmit={mutationSubmitter(completeLoginMutation, { path: { id: loginId } })}
        validationErrors={completeLoginMutation.error?.errors}
        validationBehavior="aria"
      >
        <input type="hidden" name="id" value={loginId} />
        <input type="hidden" name="emailConfirmationId" value={emailConfirmationId} />
        <div className="flex w-full flex-col gap-4 rounded-lg px-6 pt-8 pb-4">
          <div className="flex justify-center">
            <Link href="/">
              <img src={logoMarkUrl} className="h-12 w-12" alt={t`Logo`} />
            </Link>
          </div>
          <h1 className="mb-3 w-full text-center text-2xl">
            <Trans>Enter your verification code</Trans>
          </h1>
          <div className="text-center text-gray-500 text-sm">
            <Trans>
              Please check your email for a verification code sent to <span className="font-semibold">{email}</span>
            </Trans>
          </div>
          <div className="flex w-full flex-col gap-4">
            <OneTimeCodeInput
              ref={oneTimeCodeInputRef}
              name="oneTimePassword"
              digitPattern={DigitPattern.DigitsAndChars}
              length={6}
              autoFocus={true}
              ariaLabel={t`Login verification code`}
              onValueChange={(_, isComplete) => setIsOneTimeCodeComplete(isComplete)}
            />
          </div>
          {!isExpired ? (
            <p className="text-center text-neutral-500 text-xs">
              <Trans>Your verification code is valid for {expiresInString}</Trans>
            </p>
          ) : (
            <p className="text-center text-destructive text-xs">
              <Trans>Your verification code has expired</Trans>
            </p>
          )}
          <Button
            type="submit"
            className="mt-4 w-full text-center"
            isDisabled={completeLoginMutation.isPending || resendLoginCodeMutation.isPending || !isOneTimeCodeComplete}
          >
            {completeLoginMutation.isPending ? <Trans>Verifying...</Trans> : <Trans>Verify</Trans>}
          </Button>
        </div>
      </Form>

      <div className="flex flex-col items-center gap-6 px-6 text-neutral-500">
        <div className="text-center text-neutral-500 text-xs">
          <Form
            onSubmit={mutationSubmitter(resendLoginCodeMutation, {
              path: { emailConfirmationId: emailConfirmationId }
            })}
            className="inline"
          >
            <input type="hidden" name="id" value={loginId} />
            <input type="hidden" name="emailConfirmationId" value={emailConfirmationId} />
            <Button
              type="submit"
              variant="link"
              className="h-auto p-0 text-xs"
              isDisabled={completeLoginMutation.isPending || resendLoginCodeMutation.isPending}
            >
              <Trans>Didn't receive the code? Resend</Trans>
            </Button>
          </Form>
          <span className="ml-1 font-normal tabular-nums leading-none">({expiresInString})</span>
        </div>
        <p className="text-xs">
          <Trans>Can't find your code? Check your spam folder.</Trans>
        </p>
        <img src={poweredByUrl} alt={t`Powered by`} />
      </div>
    </div>
  );
}

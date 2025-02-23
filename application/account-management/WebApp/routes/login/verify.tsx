import { createFileRoute, Navigate } from "@tanstack/react-router";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { ErrorMessage } from "@/shared/components/ErrorMessage";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { DigitPattern } from "@repo/ui/components/Digit";
import { Form } from "@repo/ui/components/Form";
import { Link } from "@repo/ui/components/Link";
import { OneTimeCodeInput } from "@repo/ui/components/OneTimeCodeInput";
import { useExpirationTimeout } from "@repo/ui/hooks/useExpiration";
import logoMarkUrl from "@/shared/images/logo-mark.svg";
import poweredByUrl from "@/shared/images/powered-by.svg";
import { getLoginState, setLoginState } from "./-shared/loginState";
import { api } from "@/shared/lib/api/client";
import { loggedInPath } from "@repo/infrastructure/auth/constants";
import { useEffect } from "react";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { useIsAuthenticated } from "@repo/infrastructure/auth/hooks";
import { FormErrorMessage } from "@repo/ui/components/FormErrorMessage";

export const Route = createFileRoute("/login/verify")({
  validateSearch: (search) => {
    const returnPath = search.returnPath as string | undefined;
    // Only allow paths starting with / to prevent open redirect attacks to external domains
    return {
      returnPath: returnPath?.startsWith("/") ? returnPath : undefined
    };
  },
  component: function LoginVerifyRoute() {
    const isAuthenticated = useIsAuthenticated();

    if (isAuthenticated) {
      return <Navigate to={loggedInPath} />;
    }

    return (
      <HorizontalHeroLayout>
        <CompleteLoginForm />
      </HorizontalHeroLayout>
    );
  },
  errorComponent: (props) => (
    <HorizontalHeroLayout>
      <ErrorMessage {...props} />
    </HorizontalHeroLayout>
  )
});

export function CompleteLoginForm() {
  const { loginId, emailConfirmationId, email, expireAt } = getLoginState();
  const { expiresInString, isExpired } = useExpirationTimeout(expireAt);
  const { returnPath } = Route.useSearch();

  const completeLoginMutation = api.useMutation("post", "/api/account-management/authentication/login/{id}/complete");

  useEffect(() => {
    if (completeLoginMutation.isSuccess) {
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
        ...getLoginState(),
        expireAt: new Date(Date.now() + resendLoginCodeMutation.data.validForSeconds * 1000)
      });
    }
  }, [resendLoginCodeMutation.isSuccess, resendLoginCodeMutation.data]);

  useEffect(() => {
    if (isExpired) {
      window.location.href = "/login/expired";
    }
  }, [isExpired]);

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
              name="oneTimePassword"
              digitPattern={DigitPattern.DigitsAndChars}
              length={6}
              autoFocus
              ariaLabel={t`Login verification code`}
            />
          </div>
          <FormErrorMessage error={completeLoginMutation.error} />
          <Button
            type="submit"
            className="mt-4 w-full text-center"
            isDisabled={completeLoginMutation.isPending || resendLoginCodeMutation.isPending}
          >
            {completeLoginMutation.isPending ? <Trans>Verifying...</Trans> : <Trans>Verify</Trans>}
          </Button>
        </div>
      </Form>

      <div className="flex flex-col items-center gap-6 text-neutral-500 px-6">
        <div className="text-center text-neutral-500 text-xs">
          <Form
            onSubmit={mutationSubmitter(resendLoginCodeMutation, {
              path: { emailConfirmationId: emailConfirmationId }
            })} validationErrors={resendLoginCodeMutation.error?.errors}
            className="inline"
          >
            <input type="hidden" name="id" value={loginId} />
            <input type="hidden" name="emailConfirmationId" value={emailConfirmationId} />
            <FormErrorMessage error={resendLoginCodeMutation.error} />
            <Button
              type="submit"
              variant="link"
              className="text-xs p-0 h-auto"
              isDisabled={completeLoginMutation.isPending || resendLoginCodeMutation.isPending}
            >
              <Trans>Didn't receive the code? Resend</Trans>
            </Button>
          </Form>
          <span className="font-normal tabular-nums leading-none ml-1">({expiresInString})</span>
        </div>
        <p className="text-xs">
          <Trans>Can't find your code? Check your spam folder.</Trans>
        </p>
        <img src={poweredByUrl} alt={t`Powered by`} />
      </div>
    </div>
  );
}

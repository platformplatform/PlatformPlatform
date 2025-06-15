import { ErrorMessage } from "@/shared/components/ErrorMessage";
import logoMarkUrl from "@/shared/images/logo-mark.svg";
import logoWrapUrl from "@/shared/images/logo-wrap.svg";
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
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useCallback, useEffect, useRef, useState } from "react";

import {
  clearLoginState,
  getLoginState,
  hasLoginState,
  setLastSubmittedCode,
  setLoginState
} from "./-shared/loginState";

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

    useEffect(() => {
      if (isAuthenticated) {
        navigate({ to: loggedInPath });
        return;
      }

      if (!hasLoginState()) {
        navigate({ to: "/login", search: { returnPath: "" }, replace: true });
      }
    }, [isAuthenticated, navigate]);

    if (isAuthenticated || !hasLoginState()) {
      return null;
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

function useCountdown(expireAt: Date) {
  const [secondsRemaining, setSecondsRemaining] = useState(() =>
    Math.max(0, Math.ceil((expireAt.getTime() - Date.now()) / 1000))
  );

  // Reset the countdown when expireAt changes
  useEffect(() => {
    setSecondsRemaining(Math.max(0, Math.ceil((expireAt.getTime() - Date.now()) / 1000)));
  }, [expireAt]);

  useEffect(() => {
    const intervalId = setInterval(() => {
      setSecondsRemaining((prev) => {
        return Math.max(0, prev - 1);
      });
    }, 1000);

    return () => clearInterval(intervalId);
  }, []);

  return secondsRemaining;
}

export function CompleteLoginForm() {
  const initialState = getLoginState();
  const { email = "", emailConfirmationId = "" } = initialState;
  const initialExpireAt = initialState.expireAt ? new Date(initialState.expireAt) : new Date();
  const [expireAt, setExpireAt] = useState<Date>(initialExpireAt);
  const secondsRemaining = useCountdown(expireAt);
  const isExpired = secondsRemaining === 0;
  const oneTimeCodeInputRef = useRef<OneTimeCodeInputRef | null>(null);
  const [isOneTimeCodeComplete, setIsOneTimeCodeComplete] = useState(false);
  const [showRequestLink, setShowRequestLink] = useState(false);
  const [hasRequestedNewCode, setHasRequestedNewCode] = useState(false);
  const [isRateLimited, setIsRateLimited] = useState(false);
  const [autoSubmitCode, setAutoSubmitCode] = useState(true);
  const { returnPath } = Route.useSearch();

  useEffect(() => {
    if (!isExpired && !showRequestLink && !hasRequestedNewCode) {
      const timeoutId = setTimeout(() => {
        setShowRequestLink(true);
      }, 30000);
      return () => clearTimeout(timeoutId);
    }
  }, [isExpired, showRequestLink, hasRequestedNewCode]);

  const completeLoginMutation = api.useMutation("post", "/api/account-management/authentication/login/{id}/complete");

  const resendLoginCodeMutation = api.useMutation(
    "post",
    "/api/account-management/authentication/login/{emailConfirmationId}/resend-code"
  );

  useEffect(() => {
    if (completeLoginMutation.isSuccess) {
      clearLoginState();
      window.location.href = returnPath ?? loggedInPath;
    }
  }, [completeLoginMutation.isSuccess, returnPath]);

  useEffect(() => {
    if (completeLoginMutation.isError) {
      const statusCode = completeLoginMutation.error?.status;
      if (statusCode === 403) {
        setIsRateLimited(true);
        setExpireAt(new Date(0)); // Force expiration
      } else {
        setTimeout(() => {
          if (oneTimeCodeInputRef.current) {
            oneTimeCodeInputRef.current.focus?.();
          }
        }, 100);
      }
    }
  }, [completeLoginMutation.isError, completeLoginMutation.error]);

  const resetAfterResend = useCallback((validForSeconds: number) => {
    const newExpireAt = new Date();
    newExpireAt.setSeconds(newExpireAt.getSeconds() + validForSeconds);
    setExpireAt(newExpireAt);
    getLoginState().expireAt = newExpireAt;

    setIsOneTimeCodeComplete(false);
    setShowRequestLink(false);
    setIsRateLimited(false);

    setTimeout(() => {
      oneTimeCodeInputRef.current?.reset?.();
      oneTimeCodeInputRef.current?.focus?.();
    }, 100);
  }, []);

  useEffect(() => {
    if (resendLoginCodeMutation.isSuccess && resendLoginCodeMutation.data) {
      resetAfterResend(resendLoginCodeMutation.data.validForSeconds);
      setHasRequestedNewCode(true);
      toastQueue.add({
        title: t`Verification code sent`,
        description: t`A new verification code has been sent to your email.`,
        variant: "success"
      });
    }
  }, [resendLoginCodeMutation.isSuccess, resendLoginCodeMutation.data, resetAfterResend]);

  const expiresInString = `${Math.floor(secondsRemaining / 60)}:${String(secondsRemaining % 60).padStart(2, "0")}`;

  return (
    <div className="w-full max-w-sm space-y-3">
      <Form
        onSubmit={(event) => {
          const formData = new FormData(event.currentTarget);
          const oneTimePassword = formData.get("oneTimePassword") as string;
          if (oneTimePassword.length === 6) {
            setLastSubmittedCode(oneTimePassword);
          }
          const handler = mutationSubmitter(completeLoginMutation, { path: { id: getLoginState().loginId ?? "" } });
          return handler(event);
        }}
        validationErrors={completeLoginMutation.error?.errors}
        validationBehavior="aria"
      >
        <input type="hidden" name="id" value={getLoginState().loginId} />
        <input type="hidden" name="emailConfirmationId" value={emailConfirmationId} />
        <div className="flex w-full flex-col gap-4 rounded-lg px-6 pt-8 pb-4">
          <div className="flex justify-center">
            <Link href="/">
              <img src={logoMarkUrl} alt={t`Logo`} className="h-12 w-12" />
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
              disabled={isExpired || resendLoginCodeMutation.isPending}
              onValueChange={(value: string, isComplete: boolean) => {
                setIsOneTimeCodeComplete(isComplete);

                getLoginState().currentOtpValue = value;

                if (isComplete && autoSubmitCode) {
                  setAutoSubmitCode(false);
                  setTimeout(() => {
                    document.querySelector("form")?.requestSubmit();
                  }, 10);
                }
              }}
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
            isDisabled={
              !isOneTimeCodeComplete ||
              isExpired ||
              completeLoginMutation.isPending ||
              resendLoginCodeMutation.isPending ||
              getLoginState()?.currentOtpValue === getLoginState()?.lastSubmittedCode
            }
          >
            {completeLoginMutation.isPending ? <Trans>Verifying...</Trans> : <Trans>Verify</Trans>}
          </Button>
        </div>
      </Form>

      <div className="flex flex-col items-center gap-2 px-6 text-neutral-500 text-xs">
        <div className="text-center text-sm">
          <Trans>Can&apos;t find your code?</Trans>{" "}
          {/* Show either the spam folder message or the request link message based on conditions */}
          {!showRequestLink || isRateLimited ? (
            <Trans>Check your spam folder.</Trans>
          ) : (
            <Form
              onSubmit={(e) => {
                mutationSubmitter(resendLoginCodeMutation, { path: { emailConfirmationId } })(e);
              }}
              validationErrors={resendLoginCodeMutation.error?.errors}
              className="inline"
            >
              <Button
                type="submit"
                variant="link"
                isDisabled={resendLoginCodeMutation.isPending}
                className="h-auto p-0 text-sm"
              >
                <Trans>Request a new code</Trans>
              </Button>
            </Form>
          )}
        </div>
        <Link
          href="/login"
          className="mt-2 text-xs"
          onPress={() => {
            const loginState = getLoginState();
            clearLoginState();
            setLoginState({ email: loginState?.email ?? "" });
          }}
        >
          <Trans>Back to login</Trans>
        </Link>
        <div className="mt-6 flex flex-col items-center gap-1">
          <span className="text-muted-foreground text-xs">
            <Trans>Powered by</Trans>
          </span>
          <img src={logoWrapUrl} alt={t`PlatformPlatform`} className="h-6 w-auto" />
        </div>
      </div>
    </div>
  );
}

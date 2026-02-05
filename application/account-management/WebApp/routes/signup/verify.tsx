import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { loggedInPath } from "@repo/infrastructure/auth/constants";
import { useIsAuthenticated } from "@repo/infrastructure/auth/hooks";
import { preferredLocaleKey } from "@repo/infrastructure/translations/constants";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { InputOtp, InputOtpGroup, InputOtpSlot } from "@repo/ui/components/InputOtp";
import { Link } from "@repo/ui/components/Link";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { REGEXP_ONLY_DIGITS_AND_CHARS } from "input-otp";
import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import FederatedErrorPage from "@/federated-modules/errorPages/FederatedErrorPage";
import logoMarkUrl from "@/shared/images/logo-mark.svg";
import logoWrapUrl from "@/shared/images/logo-wrap.svg";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { api } from "@/shared/lib/api/client";

import {
  clearSignupState,
  getSignupState,
  hasSignupState,
  setLastSubmittedCode,
  setSignupState
} from "./-shared/signupState";

export const Route = createFileRoute("/signup/verify")({
  component: function SignupVerifyRoute() {
    const navigate = useNavigate();
    const isAuthenticated = useIsAuthenticated();

    useEffect(() => {
      if (isAuthenticated) {
        navigate({ to: loggedInPath });
        return;
      }

      if (!hasSignupState()) {
        navigate({ to: "/signup", replace: true });
      }
    }, [isAuthenticated, navigate]);

    if (isAuthenticated || !hasSignupState()) {
      return null;
    }

    return (
      <HorizontalHeroLayout>
        <CompleteSignupForm />
      </HorizontalHeroLayout>
    );
  },
  errorComponent: FederatedErrorPage
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

export function CompleteSignupForm() {
  const initialState = getSignupState();
  const { email = "", emailConfirmationId = "" } = initialState;
  const initialExpireAt = initialState.expireAt ? new Date(initialState.expireAt) : new Date();
  const [expireAt, setExpireAt] = useState<Date>(initialExpireAt);
  const secondsRemaining = useCountdown(expireAt);
  const isExpired = secondsRemaining === 0;
  const [otpValue, setOtpValue] = useState("");
  const isOneTimeCodeComplete = otpValue.length === 6;
  const [showRequestLink, setShowRequestLink] = useState(false);
  const [hasRequestedNewCode, setHasRequestedNewCode] = useState(false);
  const [isRateLimited, setIsRateLimited] = useState(false);
  const [autoSubmitCode, setAutoSubmitCode] = useState(true);

  useEffect(() => {
    if (!isExpired && !showRequestLink && !hasRequestedNewCode) {
      const timeoutId = setTimeout(() => {
        setShowRequestLink(true);
      }, 30000);
      return () => clearTimeout(timeoutId);
    }
  }, [isExpired, showRequestLink, hasRequestedNewCode]);

  const resetAfterResend = useCallback((validForSeconds: number) => {
    const newExpireAt = new Date();
    newExpireAt.setSeconds(newExpireAt.getSeconds() + validForSeconds);
    setExpireAt(newExpireAt);
    getSignupState().expireAt = newExpireAt;

    setOtpValue("");
    setShowRequestLink(false);
    setIsRateLimited(false);

    setTimeout(() => {
      const input = document.querySelector<HTMLInputElement>('[data-slot="input-otp"]');
      input?.focus();
    }, 100);
  }, []);

  const completeSignupMutation = api.useMutation(
    "post",
    "/api/account-management/signups/{emailConfirmationId}/complete",
    {
      onSuccess: () => {
        clearSignupState();
        window.location.href = loggedInPath;
      }
    }
  );

  const resendSignupCodeMutation = api.useMutation(
    "post",
    "/api/account-management/signups/{emailConfirmationId}/resend-code",
    {
      onSuccess: (data) => {
        if (data) {
          resetAfterResend(data.validForSeconds);
          setHasRequestedNewCode(true);
          toast.success(t`Verification code sent`, {
            description: t`A new verification code has been sent to your email.`
          });
        }
      }
    }
  );

  useEffect(() => {
    if (completeSignupMutation.isError) {
      const statusCode = completeSignupMutation.error?.status;
      if (statusCode === 403) {
        setIsRateLimited(true);
        setExpireAt(new Date(0)); // Force expiration
      } else {
        // Clear the input and reset auto-submit for next attempt
        setOtpValue("");
        setAutoSubmitCode(false);
        setTimeout(() => {
          const input = document.querySelector<HTMLInputElement>('[data-slot="input-otp"]');
          input?.focus();
        }, 100);
      }
    }
  }, [completeSignupMutation.isError, completeSignupMutation.error]);

  const expiresInString = `${Math.floor(secondsRemaining / 60)}:${String(secondsRemaining % 60).padStart(2, "0")}`;

  return (
    <div className="w-full max-w-[18rem] space-y-3">
      <Form
        onSubmit={(event) => {
          event.preventDefault();
          if (otpValue.length === 6) {
            setLastSubmittedCode(otpValue);
          }

          completeSignupMutation.mutate({
            params: {
              path: { emailConfirmationId }
            },
            body: {
              oneTimePassword: otpValue,
              preferredLocale: localStorage.getItem(preferredLocaleKey) ?? ""
            }
          });
        }}
        validationErrors={completeSignupMutation.error?.errors}
        validationBehavior="aria"
      >
        <div className="flex w-full flex-col gap-3 rounded-lg pt-6 pb-4 sm:gap-4 sm:pt-8">
          <div className="flex justify-center">
            <Link href="/" className="cursor-pointer">
              <img src={logoMarkUrl} alt={t`Logo`} className="size-12" />
            </Link>
          </div>
          <h2 className="mb-3 text-center">
            <Trans>Enter your verification code</Trans>
          </h2>
          <div className="text-center text-gray-500 text-sm">
            <Trans>
              Please check your email for a verification code sent to <span className="font-semibold">{email}</span>
            </Trans>
          </div>
          <InputOtp
            containerClassName="justify-center"
            maxLength={6}
            value={otpValue}
            onChange={(value) => {
              const upperValue = value.toUpperCase();
              setOtpValue(upperValue);
              getSignupState().currentOtpValue = upperValue;

              if (upperValue.length === 6 && autoSubmitCode) {
                setAutoSubmitCode(false);
                setTimeout(() => {
                  document.querySelector("form")?.requestSubmit();
                }, 10);
              }
            }}
            disabled={isExpired || resendSignupCodeMutation.isPending}
            autoFocus={true}
            inputMode="text"
            pattern={REGEXP_ONLY_DIGITS_AND_CHARS}
            aria-label={t`Signup verification code`}
            autoComplete="one-time-code"
          >
            <InputOtpGroup>
              <InputOtpSlot index={0} className="size-14" />
              <InputOtpSlot index={1} className="size-14" />
              <InputOtpSlot index={2} className="size-14" />
              <InputOtpSlot index={3} className="size-14" />
              <InputOtpSlot index={4} className="size-14" />
              <InputOtpSlot index={5} className="size-14" />
            </InputOtpGroup>
          </InputOtp>
          {!isExpired ? (
            <p className="text-center text-neutral-500 text-sm">
              <Trans>Your verification code is valid for {expiresInString}</Trans>
            </p>
          ) : (
            <p className="text-center text-destructive text-sm">
              <Trans>Your verification code has expired</Trans>
            </p>
          )}
          <Button
            type="submit"
            className="mt-4 w-full text-center"
            disabled={
              !isOneTimeCodeComplete ||
              isExpired ||
              completeSignupMutation.isPending ||
              resendSignupCodeMutation.isPending ||
              getSignupState()?.currentOtpValue === getSignupState()?.lastSubmittedCode
            }
          >
            {completeSignupMutation.isPending ? <Trans>Verifying...</Trans> : <Trans>Verify</Trans>}
          </Button>
        </div>
      </Form>

      <div className="flex flex-col items-center gap-2 text-neutral-500 text-sm">
        <div className="text-center text-sm">
          <Trans>Can&apos;t find your code?</Trans>{" "}
          {/* Show either the spam folder message or the request link message based on conditions */}
          {!showRequestLink || isRateLimited ? (
            <Trans>Check your spam folder.</Trans>
          ) : (
            <Form
              onSubmit={(e) => {
                mutationSubmitter(resendSignupCodeMutation, { path: { emailConfirmationId } })(e);
              }}
              validationErrors={resendSignupCodeMutation.error?.errors}
              className="inline"
            >
              <Button
                type="submit"
                variant="link"
                disabled={resendSignupCodeMutation.isPending}
                className="h-auto p-0 text-sm"
              >
                <Trans>Request a new code</Trans>
              </Button>
            </Form>
          )}
        </div>
        <Link
          href="/signup"
          className="mt-2 text-sm"
          onClick={() => {
            const signupState = getSignupState();
            clearSignupState();
            setSignupState({ email: signupState?.email ?? "" });
          }}
        >
          <Trans>Back to signup</Trans>
        </Link>
        <div className="mt-6 flex flex-col items-center gap-1">
          <span className="text-muted-foreground text-sm">
            <Trans>Powered by</Trans>
          </span>
          <Link href="https://github.com/platformplatform/PlatformPlatform" className="cursor-pointer">
            <img src={logoWrapUrl} alt={t`PlatformPlatform`} className="h-6 w-auto" />
          </Link>
        </div>
      </div>
    </div>
  );
}

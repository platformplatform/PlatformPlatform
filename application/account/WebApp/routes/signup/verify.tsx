import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { loggedInPath } from "@repo/infrastructure/auth/constants";
import { Link } from "@repo/ui/components/Link";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useCallback, useEffect, useRef, useState } from "react";

import ErrorPage from "@/federated-modules/errorPages/ErrorPage";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";

import { OtpVerificationForm } from "../-components/OtpVerificationForm";
import { useCountdown } from "../-components/useCountdown";
import { VerificationFooter } from "../-components/VerificationFooter";
import { clearSignupState, getSignupState, hasSignupState, setSignupState } from "./-shared/signupState";
import { useSignupVerification } from "./-shared/useSignupVerification";

export const Route = createFileRoute("/signup/verify")({
  staticData: { trackingTitle: "Verify sign up" },
  component: function SignupVerifyRoute() {
    const { isAuthenticated } = import.meta.user_info_env;
    const navigate = useNavigate();

    useEffect(() => {
      if (isAuthenticated) {
        window.location.href = loggedInPath;
      } else if (!hasSignupState()) {
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
  errorComponent: ErrorPage
});

export function CompleteSignupForm() {
  const otpInputRef = useRef<HTMLInputElement>(null);
  const initialState = getSignupState();
  const { email = "", emailLoginId = "" } = initialState;
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
      otpInputRef.current?.focus();
    }, 100);
  }, []);

  const { completeSignupMutation, resendSignupCodeMutation, submitVerification } = useSignupVerification({
    emailLoginId,
    onResendSuccess: (validForSeconds) => {
      resetAfterResend(validForSeconds);
      setHasRequestedNewCode(true);
    }
  });

  useEffect(() => {
    if (completeSignupMutation.isError) {
      const statusCode = completeSignupMutation.error?.status;
      if (statusCode === 403) {
        setIsRateLimited(true);
        setExpireAt(new Date(0));
      } else {
        setOtpValue("");
        setAutoSubmitCode(false);
        setTimeout(() => {
          otpInputRef.current?.focus();
        }, 100);
      }
    }
  }, [completeSignupMutation.isError, completeSignupMutation.error]);

  const expiresInString = `${Math.floor(secondsRemaining / 60)}:${String(secondsRemaining % 60).padStart(2, "0")}`;

  return (
    <div className="w-full max-w-[22rem] space-y-3">
      <OtpVerificationForm
        otpInputRef={otpInputRef}
        email={email}
        otpValue={otpValue}
        onOtpChange={(value) => {
          const upperValue = value.toUpperCase();
          setOtpValue(upperValue);
          getSignupState().currentOtpValue = upperValue;
          if (upperValue.length === 6 && autoSubmitCode) {
            setAutoSubmitCode(false);
            submitVerification(upperValue);
          }
        }}
        isExpired={isExpired}
        isResending={resendSignupCodeMutation.isPending}
        expiresInString={expiresInString}
        isSubmitDisabled={
          !isOneTimeCodeComplete ||
          isExpired ||
          completeSignupMutation.isPending ||
          resendSignupCodeMutation.isPending ||
          getSignupState()?.currentOtpValue === getSignupState()?.lastSubmittedCode
        }
        isSubmitting={completeSignupMutation.isPending}
        validationErrors={completeSignupMutation.error?.errors}
        onSubmit={(event) => {
          event.preventDefault();
          if (otpValue.length === 6) {
            submitVerification(otpValue);
          }
        }}
        ariaLabel={t`Signup verification code`}
      />

      <VerificationFooter
        showRequestLink={showRequestLink}
        isRateLimited={isRateLimited}
        resendMutation={resendSignupCodeMutation}
        resendPath={{ id: emailLoginId }}
        backLink={
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
        }
      />
    </div>
  );
}

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { loggedInPath } from "@repo/infrastructure/auth/constants";
import { isValidReturnPath } from "@repo/infrastructure/auth/util";
import { Link } from "@repo/ui/components/Link";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useCallback, useEffect, useRef, useState } from "react";

import ErrorPage from "@/federated-modules/errorPages/ErrorPage";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";

import { OtpVerificationForm } from "../-components/OtpVerificationForm";
import { useCountdown } from "../-components/useCountdown";
import { VerificationFooter } from "../-components/VerificationFooter";
import { clearLoginState, getLoginState, hasLoginState, setLoginState } from "./-shared/loginState";
import { useLoginVerification } from "./-shared/useLoginVerification";

export const Route = createFileRoute("/login/verify")({
  staticData: { trackingTitle: "Verify login" },
  validateSearch: (search) => {
    const returnPath = search.returnPath as string | undefined;
    return {
      returnPath: returnPath && isValidReturnPath(returnPath) ? returnPath : undefined
    };
  },
  component: function LoginVerifyRoute() {
    const { isAuthenticated } = import.meta.user_info_env;
    const navigate = useNavigate();

    useEffect(() => {
      if (isAuthenticated) {
        window.location.href = loggedInPath;
      } else if (!hasLoginState()) {
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
  errorComponent: ErrorPage
});

export function CompleteLoginForm() {
  const otpInputRef = useRef<HTMLInputElement>(null);
  const initialState = getLoginState();
  const { email = "", emailLoginId } = initialState;
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
  const { returnPath } = Route.useSearch();

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
    getLoginState().expireAt = newExpireAt;
    setOtpValue("");
    setShowRequestLink(false);
    setIsRateLimited(false);
    setTimeout(() => {
      otpInputRef.current?.focus();
    }, 100);
  }, []);

  const { completeLoginMutation, resendLoginCodeMutation, submitVerification } = useLoginVerification({
    emailLoginId: emailLoginId ?? "",
    email,
    returnPath,
    onResendSuccess: (validForSeconds) => {
      resetAfterResend(validForSeconds);
      setHasRequestedNewCode(true);
    }
  });

  useEffect(() => {
    if (completeLoginMutation.isError) {
      const statusCode = completeLoginMutation.error?.status;
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
  }, [completeLoginMutation.isError, completeLoginMutation.error]);

  const expiresInString = `${Math.floor(secondsRemaining / 60)}:${String(secondsRemaining % 60).padStart(2, "0")}`;

  if (!emailLoginId) {
    return null;
  }

  return (
    <div className="w-full max-w-[22rem] space-y-3">
      <OtpVerificationForm
        otpInputRef={otpInputRef}
        email={email}
        otpValue={otpValue}
        onOtpChange={(value) => {
          const upperValue = value.toUpperCase();
          setOtpValue(upperValue);
          getLoginState().currentOtpValue = upperValue;
          if (upperValue.length === 6 && autoSubmitCode) {
            setAutoSubmitCode(false);
            submitVerification(upperValue);
          }
        }}
        isExpired={isExpired}
        isResending={resendLoginCodeMutation.isPending}
        expiresInString={expiresInString}
        isSubmitDisabled={
          !isOneTimeCodeComplete ||
          isExpired ||
          completeLoginMutation.isPending ||
          resendLoginCodeMutation.isPending ||
          getLoginState()?.currentOtpValue === getLoginState()?.lastSubmittedCode
        }
        isSubmitting={completeLoginMutation.isPending}
        validationErrors={completeLoginMutation.error?.errors}
        onSubmit={(event) => {
          event.preventDefault();
          if (otpValue.length === 6) {
            submitVerification(otpValue);
          }
        }}
        ariaLabel={t`Login verification code`}
      >
        <input type="hidden" name="id" value={getLoginState().emailLoginId} />
      </OtpVerificationForm>

      <VerificationFooter
        showRequestLink={showRequestLink}
        isRateLimited={isRateLimited}
        resendMutation={resendLoginCodeMutation}
        resendPath={{ id: emailLoginId }}
        backLink={
          <Link
            href="/login"
            className="mt-2 text-sm"
            onClick={() => {
              const loginState = getLoginState();
              clearLoginState();
              setLoginState({ email: loginState?.email ?? "" });
            }}
          >
            <Trans>Back to login</Trans>
          </Link>
        }
      />
    </div>
  );
}

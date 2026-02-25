import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { authSyncService, type UserLoggedInMessage } from "@repo/infrastructure/auth/AuthSyncService";
import { loggedInPath } from "@repo/infrastructure/auth/constants";
import { isValidReturnPath } from "@repo/infrastructure/auth/util";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { InputOtp, InputOtpGroup, InputOtpSlot } from "@repo/ui/components/InputOtp";
import { Link } from "@repo/ui/components/Link";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { REGEXP_ONLY_DIGITS_AND_CHARS } from "input-otp";
import { useCallback, useEffect, useRef, useState } from "react";
import { toast } from "sonner";
import ErrorPage from "@/federated-modules/errorPages/ErrorPage";
import logoMarkUrl from "@/shared/images/logo-mark.svg";
import logoWrapUrl from "@/shared/images/logo-wrap.svg";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { api } from "@/shared/lib/api/client";

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

  // Get preferred tenant from localStorage
  const getPreferredTenantId = useCallback(() => {
    try {
      const stored = localStorage.getItem("preferred-tenant");
      return stored || null;
    } catch {
      return null;
    }
  }, []);

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

  const completeLoginMutation = api.useMutation("post", "/api/account/authentication/email/login/{id}/complete", {
    onSuccess: () => {
      // Broadcast login event to other tabs
      // Since the API returns 204 No Content, we don't have the user ID yet
      const message: Omit<UserLoggedInMessage, "timestamp"> = {
        type: "USER_LOGGED_IN",
        userId: "", // We don't have the user ID at this point
        tenantId: getPreferredTenantId() || "",
        email: email || ""
      };
      authSyncService.broadcast(message);

      clearLoginState();
      // Full page reload to get new antiforgery token with authenticated user
      window.location.href = returnPath || loggedInPath;
    }
  });

  const resendLoginCodeMutation = api.useMutation("post", "/api/account/authentication/email/login/{id}/resend-code", {
    onSuccess: (data) => {
      if (data) {
        resetAfterResend(data.validForSeconds);
        setHasRequestedNewCode(true);
        toast.success(t`Verification code sent`, {
          description: t`A new verification code has been sent to your email.`
        });
      }
    }
  });

  useEffect(() => {
    if (completeLoginMutation.isError) {
      const statusCode = completeLoginMutation.error?.status;
      if (statusCode === 403) {
        setIsRateLimited(true);
        setExpireAt(new Date(0)); // Force expiration
      } else {
        // Clear the input and reset auto-submit for next attempt
        setOtpValue("");
        setAutoSubmitCode(false);
        setTimeout(() => {
          otpInputRef.current?.focus();
        }, 100);
      }
    }
  }, [completeLoginMutation.isError, completeLoginMutation.error]);

  const expiresInString = `${Math.floor(secondsRemaining / 60)}:${String(secondsRemaining % 60).padStart(2, "0")}`;

  const submitVerification = useCallback(
    (code: string) => {
      if (!emailLoginId) {
        return;
      }
      setLastSubmittedCode(code);
      completeLoginMutation.mutate({
        params: {
          path: { id: emailLoginId }
        },
        body: {
          oneTimePassword: code,
          preferredTenantId: getPreferredTenantId() || null
        }
      });
    },
    [completeLoginMutation, emailLoginId, getPreferredTenantId]
  );

  if (!emailLoginId) {
    return null;
  }

  return (
    <div className="w-full max-w-[22rem] space-y-3">
      <Form
        onSubmit={(event) => {
          event.preventDefault();
          if (otpValue.length === 6) {
            submitVerification(otpValue);
          }
        }}
        validationErrors={completeLoginMutation.error?.errors}
        validationBehavior="aria"
      >
        <input type="hidden" name="id" value={getLoginState().emailLoginId} />
        <div className="flex w-full flex-col gap-3 rounded-lg pt-6 pb-4 sm:gap-4 sm:pt-8">
          <div className="flex justify-center">
            <Link href="/" className="cursor-pointer">
              <img src={logoMarkUrl} alt={t`Logo`} className="size-12" />
            </Link>
          </div>
          <h2 className="mb-3 text-center">
            <Trans>Enter your verification code</Trans>
          </h2>
          <div className="text-center text-muted-foreground text-sm">
            <Trans>
              Please check your email for a verification code sent to <span className="font-semibold">{email}</span>
            </Trans>
          </div>
          <InputOtp
            ref={otpInputRef}
            containerClassName="justify-center"
            maxLength={6}
            value={otpValue}
            onChange={(value) => {
              const upperValue = value.toUpperCase();
              setOtpValue(upperValue);
              getLoginState().currentOtpValue = upperValue;

              if (upperValue.length === 6 && autoSubmitCode) {
                setAutoSubmitCode(false);
                submitVerification(upperValue);
              }
            }}
            disabled={isExpired || resendLoginCodeMutation.isPending}
            autoFocus={true}
            inputMode="text"
            pattern={REGEXP_ONLY_DIGITS_AND_CHARS}
            aria-label={t`Login verification code`}
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
          <div aria-live={isExpired ? "polite" : "off"}>
            {!isExpired ? (
              <p className="text-center text-muted-foreground text-sm">
                <Trans>Your verification code is valid for {expiresInString}</Trans>
              </p>
            ) : (
              <p className="text-center text-destructive text-sm">
                <Trans>Your verification code has expired</Trans>
              </p>
            )}
          </div>
          <Button
            type="submit"
            className="mt-4 w-full text-center"
            disabled={
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

      <div className="flex flex-col items-center gap-2 text-muted-foreground text-sm">
        <div className="text-center text-sm">
          <Trans>Can&apos;t find your code?</Trans>{" "}
          {/* Show either the spam folder message or the request link message based on conditions */}
          {!showRequestLink || isRateLimited ? (
            <Trans>Check your spam folder.</Trans>
          ) : (
            <Form
              onSubmit={(e) => {
                mutationSubmitter(resendLoginCodeMutation, { path: { id: emailLoginId } })(e);
              }}
              validationErrors={resendLoginCodeMutation.error?.errors}
              className="inline"
            >
              <Button
                type="submit"
                variant="link"
                disabled={resendLoginCodeMutation.isPending}
                className="h-auto p-0 text-sm"
              >
                <Trans>Request a new code</Trans>
              </Button>
            </Form>
          )}
        </div>
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

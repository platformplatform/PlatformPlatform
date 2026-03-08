import type { RefObject } from "react";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { InputOtp, InputOtpGroup, InputOtpSlot } from "@repo/ui/components/InputOtp";
import { Link } from "@repo/ui/components/Link";
import { REGEXP_ONLY_DIGITS_AND_CHARS } from "input-otp";

import logoMarkUrl from "@/shared/images/logo-mark.svg";

interface OtpVerificationFormProps {
  otpInputRef: RefObject<HTMLInputElement | null>;
  email: string;
  otpValue: string;
  onOtpChange: (value: string) => void;
  isExpired: boolean;
  isResending: boolean;
  expiresInString: string;
  isSubmitDisabled: boolean;
  isSubmitting: boolean;
  validationErrors: Record<string, string[]> | undefined;
  onSubmit: (event: React.FormEvent<HTMLFormElement>) => void;
  ariaLabel: string;
  children?: React.ReactNode;
}

export function OtpVerificationForm({
  otpInputRef,
  email,
  otpValue,
  onOtpChange,
  isExpired,
  isResending,
  expiresInString,
  isSubmitDisabled,
  isSubmitting,
  validationErrors,
  onSubmit,
  ariaLabel,
  children
}: OtpVerificationFormProps) {
  return (
    <Form onSubmit={onSubmit} validationErrors={validationErrors} validationBehavior="aria">
      {children}
      <div className="flex w-full flex-col gap-3 rounded-lg pt-6 pb-4 sm:gap-4 sm:pt-8">
        <div className="flex justify-center">
          <Link href="/" className="cursor-pointer">
            <img src={logoMarkUrl} alt={t`Logo`} className="size-12" />
          </Link>
        </div>
        <h2 className="mb-3 text-center">
          <Trans>Enter your verification code</Trans>
        </h2>
        <div className="text-center text-sm text-muted-foreground">
          <Trans>
            Please check your email for a verification code sent to <span className="font-semibold">{email}</span>
          </Trans>
        </div>
        <InputOtp
          ref={otpInputRef}
          containerClassName="justify-center"
          maxLength={6}
          value={otpValue}
          onChange={onOtpChange}
          disabled={isExpired || isResending}
          autoFocus={true}
          inputMode="text"
          pattern={REGEXP_ONLY_DIGITS_AND_CHARS}
          aria-label={ariaLabel}
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
            <p className="text-center text-sm text-muted-foreground">
              <Trans>Your verification code is valid for {expiresInString}</Trans>
            </p>
          ) : (
            <p className="text-center text-sm text-destructive">
              <Trans>Your verification code has expired</Trans>
            </p>
          )}
        </div>
        <Button type="submit" className="mt-4 w-full text-center" disabled={isSubmitDisabled}>
          {isSubmitting ? <Trans>Verifying...</Trans> : <Trans>Verify</Trans>}
        </Button>
      </div>
    </Form>
  );
}

import { createFileRoute, Navigate } from "@tanstack/react-router";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { ErrorMessage } from "@/shared/components/ErrorMessage";
import { Trans } from "@lingui/macro";
import { Button } from "@repo/ui/components/Button";
import { DigitPattern } from "@repo/ui/components/Digit";
import { Form } from "@repo/ui/components/Form";
import { Link } from "@repo/ui/components/Link";
import { OneTimeCodeInput } from "@repo/ui/components/OneTimeCodeInput";
import { useExpirationTimeout } from "@repo/ui/hooks/useExpiration";
import logoMarkUrl from "@/shared/images/logo-mark.svg";
import poweredByUrl from "@/shared/images/powered-by.svg";
import { useFormState } from "react-dom";
import { getRegistration } from "./-shared/registrationState";
import { api } from "@/shared/lib/api/client";
import { FormErrorMessage } from "@repo/ui/components/FormErrorMessage";

export const Route = createFileRoute("/register/verify")({
  component: () => (
    <HorizontalHeroLayout>
      <CompleteAccountRegistrationForm />
    </HorizontalHeroLayout>
  ),
  errorComponent: (props) => (
    <HorizontalHeroLayout>
      <ErrorMessage {...props} />
    </HorizontalHeroLayout>
  )
});

export function CompleteAccountRegistrationForm() {
  const { email, accountRegistrationId, expireAt } = getRegistration();
  const { expiresInString, isExpired } = useExpirationTimeout(expireAt);
  const [{ success, title, message, errors }, action] = useFormState(
    api.action("/api/account-management/account-registrations/{id}/complete"),
    {
      success: null
    }
  );

  if (isExpired) return <Navigate to="/register/expired" />;

  if (success) return <Navigate to="/admin/users" />;

  return (
    <Form action={action} validationErrors={errors} validationBehavior="aria" className="w-full max-w-sm space-y-3">
      <input type="hidden" name="id" value={accountRegistrationId} />
      <div className="flex w-full flex-col gap-4 rounded-lg px-6 pt-8 pb-4">
        <div className="flex justify-center">
          <Link href="/">
            <img src={logoMarkUrl} className="h-12 w-12" alt="logo mark" />
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
          <OneTimeCodeInput name="oneTimePassword" digitPattern={DigitPattern.DigitsAndChars} length={6} autoFocus />
          <div className="text-center text-neutral-500 text-xs">
            <Link href="/">
              <Trans>Didn't receive the code? Resend</Trans>
            </Link>
            <span className="font-normal tabular-nums leading-none">({expiresInString})</span>
          </div>
        </div>
        <FormErrorMessage title={title} message={message} />
        <Button type="submit" className="mt-4 w-full text-center">
          <Trans>Verify</Trans>
        </Button>
        <input type="hidden" name="accountRegistrationId" value={accountRegistrationId} />
        <div className="flex flex-col items-center gap-6 text-neutral-500">
          <p className="text-xs ">
            <Trans>Can't find your code? Check your spam folder</Trans>
          </p>
          <img src={poweredByUrl} alt="powered by" />
        </div>
      </div>
    </Form>
  );
}

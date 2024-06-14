import { useFormState, useFormStatus } from "react-dom";
import { Trans } from "@lingui/macro";
import { Navigate } from "@tanstack/react-router";
import type { State } from "./actions.ts";
import { completeAccountRegistration, registration } from "./actions.ts";
import { Button } from "@/ui/components/Button";
import { Form } from "@/ui/components/Form";
import { useExpirationTimeout } from "@/ui/oneTimePassword/useExpiration";
import { OneTimeCodeInput } from "@/ui/oneTimePassword/OneTimeCodeInput";
import { DigitPattern } from "@/ui/oneTimePassword/DigitPattern";
import { Link } from "@/ui/components/Link";
import poweredByUrl from "@/ui/images/powered-by.png";
import logoMarkUrl from "@/ui/images/logo-mark.png";

export function CompleteAccountRegistrationForm() {
  const initialState: State = { message: null, errors: {} };

  if (!registration.current)
    throw new Error("Account registration ID is missing.");

  const { email, accountRegistrationId, expireAt } = registration.current;

  const { expiresInString, isExpired } = useExpirationTimeout(expireAt);

  const [state, action] = useFormState(completeAccountRegistration, initialState);

  if (isExpired)
    return <Navigate to="/register/expired" />;

  if (state.success)
    return <Navigate to="/admin/users" />;

  return (
    <Form action={action} validationErrors={state.errors} className="space-y-3 w-full max-w-sm">
      <div className="flex flex-col gap-4 rounded-lg px-6 pb-4 pt-8 w-full">
        <div className="flex justify-center">
          <img src={logoMarkUrl} className="h-12 w-12" alt="logo mark" />
        </div>
        <h1 className="mb-3 text-2xl w-full text-center">
          <Trans>Enter your verification code</Trans>
        </h1>
        <div className="text-gray-500 text-sm text-center">
          <Trans>
            Please check your email for a verification code sent to <span className="font-semibold">{email}</span>
          </Trans>
        </div>
        <div className="w-full flex flex-col gap-4">
          <OneTimeCodeInput name="oneTimePassword" digitPattern={DigitPattern.DigitsAndChars} length={6} />
          <div className="text-xs text-neutral-500 text-center">
            <Link href="/" bold>
              <Trans>Did't receive the code? Resend</Trans>
            </Link>{" "}
            <span className="font-normal leading-none tabular-nums">({expiresInString})</span>
          </div>
        </div>
        <CompleteAccountRegistrationButton />
        <input type="hidden" name="accountRegistrationId" value={accountRegistrationId} />
        <div className="flex flex-col text-neutral-500 items-center gap-6">
          <p className="text-xs ">
            <Trans>Can't find your code? Check your spam folder</Trans>
          </p>
          <img src={poweredByUrl} alt="powered by" />
        </div>
      </div>
    </Form>
  );
}

function CompleteAccountRegistrationButton() {
  const { pending } = useFormStatus();

  return (
    <Button type="submit" className="mt-4 w-full text-center" aria-disabled={pending}>
      <Trans>Verify</Trans>
    </Button>
  );
}

import { useEffect } from "react";
import type { ErrorComponentProps } from "@tanstack/react-router";
import { VerificationExpirationError } from "@/ui/oneTimePassword/VerificationExpirationError";

export function ErrorPage({ error, reset, info }: ErrorComponentProps) {
  // eslint-disable-next-line no-console
  console.log("ErrorPage", error, info);
  useEffect(() => {
    // Log the error to an error reporting service
    console.error(error);
  }, [error]);

  if (error instanceof VerificationExpirationError)
    return <VerificationExpired accountRegistrationId={error.accountRegistrationId} />;

  // Generic error message
  return (
    <div>
      <h2>Something went wrong!</h2>
      <p>There was an error verifying your registration code.</p>
      <button
        type="button"
        onClick={
          // Attempt to recover by trying to re-render the segment
          () => reset()
        }
      >
        Try again
      </button>
    </div>
  );
}

interface VerificationExpiredProps {
  accountRegistrationId: string;
}

function VerificationExpired({ accountRegistrationId }: Readonly<VerificationExpiredProps>) {
  return (
    <div className="flex flex-col text-center p-8">
      <h2>Verification code expired</h2>
      <p>The verification code you are trying to use has expired.</p>
      <p>Account Registration ID: {accountRegistrationId}</p>
      <button type="button" className="font-semibold">
        Try again
      </button>
    </div>
  );
}

import { useEffect } from "react";
import { VerificationExpirationError } from "@/ui/oneTimePassword/VerificationExpirationError";

interface ErrorProps {
  params: {
    accountRegistrationId: string,
  };
  error: Error;
  reset: () => void;
}

export default function ErrorPage({ error, reset, params: { accountRegistrationId } }: Readonly<ErrorProps>) {
  useEffect(() => {
    // Log the error to an error reporting service
    console.error(error);
  }, [error]);

  if (error instanceof VerificationExpirationError)
    return <VerificationExpired accountRegistrationId={accountRegistrationId} />;

  // Generic error message
  return (
    <div>
      <h2>Something went wrong!</h2>
      <p>There was an error verifying your registration code.</p>
      <button
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

function VerificationExpired({ accountRegistrationId }: VerificationExpiredProps) {
  return (
    <div className="flex flex-col text-center p-8">
      <h2>Verification code expired</h2>
      <p>The verification code you are trying to use has expired.</p>
      <p>Account Registration ID: {accountRegistrationId}</p>
      <button className="font-semibold">
        Try again
      </button>
    </div>
  );
}

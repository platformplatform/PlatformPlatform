import { useEffect } from "react";
import { VerificationExpirationError } from "@/ui/Auth/otp/VerificationExpirationError";

interface ErrorProps {
  params: {
    registrationId: string,
  };
  error: Error;
  reset: () => void;
}

export default function ErrorPage({ error, reset, params: { registrationId } }: Readonly<ErrorProps>) {
  useEffect(() => {
    // Log the error to an error reporting service
    console.error(error);
  }, [error]);

  if (error instanceof VerificationExpirationError)
    return <VerificationExpired registrationId={registrationId} />;

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
  registrationId: string;
}

function VerificationExpired({ registrationId }: VerificationExpiredProps) {
  return (
    <div className="flex flex-col text-center p-8">
      <h2>Verification code expired</h2>
      <p>The verification code you are trying to use has expired.</p>
      <p>Registration ID: {registrationId}</p>
      <button className="font-semibold">
        Try again
      </button>
    </div>
  );
}

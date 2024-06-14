import { createFileRoute } from "@tanstack/react-router";
import { registration } from "../-components/actions";
import { Link } from "@/ui/components/Link";

export const Route = createFileRoute("/register/expired/")({
  component: VerificationExpired,
});

function VerificationExpired() {
  if (!registration.current)
    throw new Error("Expected registration to be active");

  const { accountRegistrationId } = registration.current;
  return (
    <div className="flex flex-col text-center p-8">
      <h2>Verification code expired</h2>
      <p>The verification code you are trying to use has expired.</p>
      <p>Account Registration ID: {accountRegistrationId}</p>
      <Link href="/register">Try again</Link>
    </div>
  );
}

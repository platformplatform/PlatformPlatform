import { createFileRoute } from "@tanstack/react-router";
import { registration } from "../-state/actions";
import { Link } from "@repo/ui/components/Link";
import { RegisterLayout } from "@/pages/register/_layout";

export const Route = createFileRoute("/register/expired/")({
  component: WrappedVerificationExpiredPage
});

export default function WrappedVerificationExpiredPage() {
  return (
    <RegisterLayout>
      <VerificationExpiredPage />
    </RegisterLayout>
  );
}

function VerificationExpiredPage() {
  if (!registration.current) throw new Error("Expected registration to be active");

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

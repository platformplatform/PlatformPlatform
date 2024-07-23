import { createFileRoute } from "@tanstack/react-router";
import { registration } from "../../shared/ui/auth/actions";
import { Link } from "@repo/ui/components/Link";
import { HorizontalHeroLayout } from "@/shared/ui/layout/HorizontalHeroLayout";

export const Route = createFileRoute("/(auth)/register/expired")({
  component: () => (
    <HorizontalHeroLayout>
      <VerificationExpiredPage />
    </HorizontalHeroLayout>
  )
});

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

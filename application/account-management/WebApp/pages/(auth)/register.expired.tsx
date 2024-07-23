import { createFileRoute } from "@tanstack/react-router";
import { Link } from "@repo/ui/components/Link";
import { HorizontalHeroLayout } from "@/shared/ui/layout/HorizontalHeroLayout";
import { useRegistration } from "@/shared/ui/auth/actions";

export const Route = createFileRoute("/(auth)/register/expired")({
  component: () => (
    <HorizontalHeroLayout>
      <VerificationExpiredPage />
    </HorizontalHeroLayout>
  )
});

function VerificationExpiredPage() {
  const { accountRegistrationId } = useRegistration();

  return (
    <div className="flex flex-col text-center p-8">
      <h2>Verification code expired</h2>
      <p>The verification code you are trying to use has expired.</p>
      <p>Account Registration ID: {accountRegistrationId}</p>
      <Link href="/register">Try again</Link>
    </div>
  );
}

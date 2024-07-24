import { createFileRoute } from "@tanstack/react-router";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { ErrorMessage } from "@/shared/components/ErrorMessage";
import Timeout from "@spectrum-icons/illustrations/Timeout";
import { Link } from "@repo/ui/components/Link";
import { useLogin } from "./-shared/actions";
import { Content, Heading, IllustratedMessage } from "@repo/ui/components/IllustratedMessage";

export const Route = createFileRoute("/login/expired")({
  component: () => (
    <HorizontalHeroLayout>
      <VerificationCodeExpiredMessage />
    </HorizontalHeroLayout>
  ),
  errorComponent: (props) => (
    <HorizontalHeroLayout>
      <ErrorMessage {...props} />
    </HorizontalHeroLayout>
  )
});

export function VerificationCodeExpiredMessage() {
  const { loginId } = useLogin();

  return (
    <IllustratedMessage>
      <Timeout />
      <Heading>Error: Verification code expired</Heading>
      <Content>The verification code you are trying to use has expired for Login ID: {loginId}</Content>
      <Link href="/login">Try again</Link>
    </IllustratedMessage>
  );
}

import { createFileRoute } from "@tanstack/react-router";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { ErrorMessage } from "@/shared/components/ErrorMessage";
import Timeout from "@spectrum-icons/illustrations/Timeout";
import { Link } from "@repo/ui/components/Link";
import { useRegistration } from "./-shared/actions";
import { Content, Heading, IllustratedMessage } from "@repo/ui/components/IllustratedMessage";

export const Route = createFileRoute("/register/expired")({
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
  const { accountRegistrationId } = useRegistration();

  return (
    <IllustratedMessage>
      <Timeout />
      <Heading>Error: Verification code expired</Heading>
      <Content>
        The verification code you are trying to use has expired for Account Registration ID: {accountRegistrationId}
      </Content>
      <Link href="/register">Try again</Link>
    </IllustratedMessage>
  );
}

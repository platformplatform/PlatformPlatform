import { createFileRoute } from "@tanstack/react-router";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { ErrorMessage } from "@/shared/components/ErrorMessage";
import Timeout from "@spectrum-icons/illustrations/Timeout";
import { Link } from "@repo/ui/components/Link";
import { Content, Heading, IllustratedMessage } from "@repo/ui/components/IllustratedMessage";
import { getSignupState } from "./-shared/signupState";
import { signUpPath } from "@repo/infrastructure/auth/constants";

export const Route = createFileRoute("/signup/expired")({
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
  const { signupId } = getSignupState();

  return (
    <IllustratedMessage>
      <Timeout />
      <Heading>Error: Verification code expired</Heading>
      <Content>The verification code you are trying to use has expired for Signup ID: {signupId}</Content>
      <Link href={signUpPath}>Try again</Link>
    </IllustratedMessage>
  );
}

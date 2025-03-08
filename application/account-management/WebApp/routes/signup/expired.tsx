import { ErrorMessage } from "@/shared/components/ErrorMessage";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { Trans } from "@lingui/react/macro";
import { signUpPath } from "@repo/infrastructure/auth/constants";
import { Content, Heading, IllustratedMessage } from "@repo/ui/components/IllustratedMessage";
import { Link } from "@repo/ui/components/Link";
import Timeout from "@spectrum-icons/illustrations/Timeout";
import { createFileRoute } from "@tanstack/react-router";
import { getSignupState } from "./-shared/signupState";

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
  const { emailConfirmationId } = getSignupState();

  return (
    <IllustratedMessage>
      <Timeout />
      <Heading>
        <Trans>Error: Verification code has expired</Trans>
      </Heading>
      <Content>
        <Trans>
          The verification code you are trying to use has expired for Email Confirmation ID: {emailConfirmationId}
        </Trans>
      </Content>
      <Link href={signUpPath}>
        <Trans>Try again</Trans>
      </Link>
    </IllustratedMessage>
  );
}

import { createFileRoute } from "@tanstack/react-router";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { ErrorMessage } from "@/shared/components/ErrorMessage";
import Timeout from "@spectrum-icons/illustrations/Timeout";
import { Link } from "@repo/ui/components/Link";
import { Content, Heading, IllustratedMessage } from "@repo/ui/components/IllustratedMessage";
import { getSignupState } from "./-shared/signupState";
import { signUpPath } from "@repo/infrastructure/auth/constants";
import { Trans } from "@lingui/react/macro";

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

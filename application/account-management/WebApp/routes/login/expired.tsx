import { ErrorMessage } from "@/shared/components/ErrorMessage";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { Trans } from "@lingui/react/macro";
import { loginPath } from "@repo/infrastructure/auth/constants";
import { Content, Heading, IllustratedMessage } from "@repo/ui/components/IllustratedMessage";
import { Link } from "@repo/ui/components/Link";
import Timeout from "@spectrum-icons/illustrations/Timeout";
import { createFileRoute } from "@tanstack/react-router";
import { getLoginState } from "./-shared/loginState";

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
  const { loginId } = getLoginState();

  return (
    <IllustratedMessage>
      <Timeout />
      <Heading>
        <Trans>Error: Verification code has expired</Trans>
      </Heading>
      <Content>
        <Trans>The verification code you are trying to use has expired for Login ID: {loginId}</Trans>
      </Content>
      <Link href={loginPath}>
        <Trans>Try again</Trans>
      </Link>
    </IllustratedMessage>
  );
}

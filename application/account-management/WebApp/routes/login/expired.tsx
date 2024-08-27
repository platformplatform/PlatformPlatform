import { createFileRoute } from "@tanstack/react-router";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { ErrorMessage } from "@/shared/components/ErrorMessage";
import Timeout from "@spectrum-icons/illustrations/Timeout";
import { Link } from "@repo/ui/components/Link";
import { Content, Heading, IllustratedMessage } from "@repo/ui/components/IllustratedMessage";
import { getLoginState } from "./-shared/loginState";
import { loginPath } from "@repo/infrastructure/auth/constants";

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
      <Heading>Error: Verification code expired</Heading>
      <Content>The verification code you are trying to use has expired for Login ID: {loginId}</Content>
      <Link href={loginPath}>Try again</Link>
    </IllustratedMessage>
  );
}

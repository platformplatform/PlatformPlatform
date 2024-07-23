import Timeout from "@spectrum-icons/illustrations/Timeout";
import { Link } from "@repo/ui/components/Link";
import { useRegistration } from "@/shared/ui/auth/actions/accountRegistration";
import { Content, Heading, IllustratedMessage } from "@repo/ui/components/IllustratedMessage";

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

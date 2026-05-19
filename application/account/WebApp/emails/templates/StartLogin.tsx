// @jsxRuntime automatic
import { Trans } from "@lingui/react/macro";
import { Section, Text } from "@react-email/components";
import { Heading } from "@repo/emails/components/Heading";
import { Subject } from "@repo/emails/components/Subject";
import { TransactionalEmail } from "@repo/emails/components/TransactionalEmail";
import { OtpAutofill } from "@repo/emails/helpers/OtpAutofill";
import { Value } from "@repo/emails/helpers/Value";

type StartLoginProps = {
  locale: string;
};

export default function StartLogin({ locale }: Readonly<StartLoginProps>) {
  return (
    <TransactionalEmail
      locale={locale}
      preview="{{ProductName}} login verification code"
      otpAutofill={<OtpAutofill code="OneTimePassword" domain="Domain" />}
    >
      <Subject>
        <Trans>{`'{{'ProductName'}}' login verification code`}</Trans>
      </Subject>

      <Heading level={1} className="text-center">
        <Trans>Your confirmation code is below</Trans>
      </Heading>

      <Text className="m-[0px] mb-[16px] text-center text-[14px] leading-[24px]">
        <Trans>Enter it in your open browser window. It is only valid for a few minutes.</Trans>
      </Text>

      <Section className="email-otp-box my-[16px] rounded-[8px] bg-[#f1f5f9] p-[16px] text-center">
        <Text className="email-otp-text m-[0px] text-center font-mono text-[32px] tracking-[8px] text-[#0f172a]">
          <Value path="OneTimePassword" sample="ABC123" />
        </Text>
      </Section>

      <Text className="email-muted m-[0px] mt-[16px] text-center text-[13px] leading-[20px] text-[#64748b]">
        <Trans>If you didn't try to log in, you can safely ignore this email — your account is secure.</Trans>
      </Text>
    </TransactionalEmail>
  );
}

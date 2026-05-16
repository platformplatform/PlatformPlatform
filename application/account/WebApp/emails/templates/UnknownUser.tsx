// @jsxRuntime automatic
import { Trans } from "@lingui/react/macro";
import { Button, Section, Text } from "@react-email/components";
import { Heading } from "@repo/emails/components/Heading";
import { Subject } from "@repo/emails/components/Subject";
import { TransactionalEmail } from "@repo/emails/components/TransactionalEmail";
import { Value } from "@repo/emails/helpers/Value";

type UnknownUserProps = {
  locale: string;
};

export default function UnknownUser({ locale }: Readonly<UnknownUserProps>) {
  return (
    <TransactionalEmail locale={locale} preview="No {{ProductName}} account found">
      <Subject>
        <Trans>No account found</Trans>
      </Subject>

      <Heading level={1} className="text-center">
        <Trans>Is this the right email address?</Trans>
      </Heading>

      <Text className="m-[0px] mb-[16px] text-center text-[14px] leading-[24px]">
        <Trans>
          It looks like there isn't a {"'{{'ProductName'}}'"} account tied to{" "}
          <strong>
            <Value path="Email" sample="alex@example.com" />
          </strong>
          .
        </Trans>
      </Text>

      <Text className="m-[0px] text-center text-[14px] leading-[24px]">
        <Trans>You can try again with a different email, or sign up for a new account.</Trans>
      </Text>

      <Section className="mt-[24px] text-center">
        <Button
          href="{{SignupUrl}}"
          className="email-button-default rounded-[8px] bg-[#0f172a] px-[24px] py-[12px] text-[14px] font-medium text-white"
        >
          <Trans>Sign up for an account</Trans>
        </Button>
      </Section>

      <Text className="email-muted m-[0px] mt-[24px] text-center text-[13px] leading-[20px] text-[#64748b]">
        <Trans>If this wasn't you, no action is needed — no account was created.</Trans>
      </Text>
    </TransactionalEmail>
  );
}

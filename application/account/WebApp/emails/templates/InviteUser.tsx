// @jsxRuntime automatic
import { Trans } from "@lingui/react/macro";
import { Link, Text } from "@react-email/components";
import { Heading } from "@repo/emails/components/Heading";
import { Subject } from "@repo/emails/components/Subject";
import { TransactionalEmail } from "@repo/emails/components/TransactionalEmail";
import { Value } from "@repo/emails/helpers/Value";

type InviteUserProps = {
  locale: string;
};

export default function InviteUser({ locale }: Readonly<InviteUserProps>) {
  return (
    <TransactionalEmail locale={locale} preview="You have been invited to {{ProductName}}">
      <Subject>
        <Trans>{`You have been invited to join '{{'TenantName'}}' on '{{'ProductName'}}'`}</Trans>
      </Subject>

      <Heading level={1} className="text-center">
        <Trans>
          <Value path="InviterName" sample="Alex Taylor" /> invited you to join {"'{{'ProductName'}}'"}.
        </Trans>
      </Heading>

      <Text className="m-[0px] text-center text-[14px] leading-[24px]">
        <Trans>
          To gain access,{" "}
          <Link href="{{LoginUrl}}" className="email-link text-[#0f172a] underline">
            go to this page in your open browser
          </Link>{" "}
          and login using{" "}
          <strong>
            <Value path="Email" sample="invitee@example.com" />
          </strong>
          .
        </Trans>
      </Text>

      <Text className="email-muted m-[0px] mt-[16px] text-center text-[13px] leading-[20px] text-[#64748b]">
        <Trans>If you don't recognize the sender, you can safely ignore this email.</Trans>
      </Text>
    </TransactionalEmail>
  );
}

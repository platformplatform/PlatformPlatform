// @jsxRuntime automatic
import { Trans } from "@lingui/react/macro";
import { Link, Section, Text } from "@react-email/components";
import { Heading } from "@repo/emails/components/Heading";
import { Subject } from "@repo/emails/components/Subject";
import { TransactionalEmail } from "@repo/emails/components/TransactionalEmail";
import { Value } from "@repo/emails/helpers/Value";

type SupportStaffReplyProps = {
  locale: string;
};

export default function SupportStaffReply({ locale }: Readonly<SupportStaffReplyProps>) {
  return (
    <TransactionalEmail locale={locale} preview="Re: {{Subject}}">
      <Subject>
        <Trans>{`Re: '{{'Subject'}}' · #'{{'ShortDisplayId'}}'`}</Trans>
      </Subject>

      <Heading level={1}>
        <Trans>
          <Value path="StaffName" sample="Lars" /> replied to your ticket
        </Trans>
      </Heading>

      <Section className="email-quote my-[16px] rounded-[8px] border border-[#e2e8f0] bg-[#f8fafc] p-[16px]">
        <Text className="m-[0px] text-[14px] leading-[22px] whitespace-pre-line">
          <Value path="Body" sample="Thanks for reaching out — we are looking into this now." />
        </Text>
      </Section>

      <Section className="my-[16px] text-center">
        <Link
          href="{{TicketUrl}}"
          className="email-link inline-block rounded-[8px] bg-[#0f172a] px-[20px] py-[12px] text-[14px] font-semibold text-white no-underline"
        >
          <Trans>Open ticket</Trans>
        </Link>
      </Section>

      <Text className="email-muted m-[0px] text-center text-[13px] leading-[20px] text-[#64748b]">
        <Trans>Reply by opening the ticket — replies to this email are not monitored.</Trans>
      </Text>
    </TransactionalEmail>
  );
}

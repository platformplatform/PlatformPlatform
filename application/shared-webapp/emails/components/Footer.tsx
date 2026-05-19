import { Trans } from "@lingui/react/macro";
import { Hr, Link, Section, Text } from "@react-email/components";
import { loadPlatformSettings } from "@repo/build/platformSettings";

import { Value } from "../helpers/Value";

// Email components only ever render in Node (the email build and the React Email dev server), so
// reading platform-settings.jsonc directly is safe. The values become the <Value> `sample` props,
// keeping the dev preview brand-accurate; production output still emits the {{ }} Scriban
// placeholders that ScribanEmailRenderer substitutes per request.
const branding = loadPlatformSettings().branding;

// Adapted from react.email/components (MIT) — composes the official `<Section>`, `<Hr>`, `<Text>`,
// and `<Link>` primitives into a transactional-email footer with three layers:
//
//   1. Brand identity — wordmark name + a one-liner explaining what the product is, so the
//      recipient sees the brand context even if they don't recall signing up.
//   2. <Hr> divider — react.email/components/divider styled with `.email-separator` so dark mode
//      gets the existing `border-top-color: #404040` flip from EMAIL_STYLES.
//   3. Legal links — Privacy / Terms / DPA / Compliance, each routed through the {{PublicUrl}}
//      Scriban global so localhost dev, staging, and production all link to their own host.
//
// TransactionalEmail renders this OUTSIDE the white card so it visually appears as a separate
// footer band (Stripe / Linear / Notion convention).
export function Footer({ locale }: { readonly locale: string }) {
  // The mail tagline is per-locale; production substitution is handled server-side by
  // ScribanEmailRenderer. This `sample` only feeds the dev preview, so fall back to en-US if the
  // locale has no entry.
  const mailTagline = branding.tagline.mail[locale] ?? branding.tagline.mail["en-US"];

  return (
    <Section className="email-footer mx-auto mb-[40px] w-full max-w-[600px] text-center">
      <Section>
        <Text className="m-[0px] text-[14px] leading-[20px] font-semibold">
          <Value path="ProductName" sample={branding.productName} />
        </Text>
        <Text className="email-muted m-[0px] mt-[4px] text-[13px] leading-[20px] text-[#64748b]">
          <Value path="Tagline" sample={mailTagline} />
        </Text>
      </Section>

      <Hr className="email-separator my-[16px] border-t border-[#e2e8f0]" />

      <Section>
        <Text className="email-muted m-[0px] text-[13px] leading-[20px] text-[#64748b]">
          <Link href="{{PublicUrl}}/legal/privacy" className="email-link text-[#64748b] underline">
            <Trans>Privacy</Trans>
          </Link>
          {" · "}
          <Link href="{{PublicUrl}}/legal/terms" className="email-link text-[#64748b] underline">
            <Trans>Terms</Trans>
          </Link>
          {" · "}
          <Link href="{{PublicUrl}}/legal/dpa" className="email-link text-[#64748b] underline">
            <Trans>DPA</Trans>
          </Link>
          {" · "}
          <Link href="{{PublicUrl}}/legal" className="email-link text-[#64748b] underline">
            <Trans>Compliance</Trans>
          </Link>
        </Text>
      </Section>
    </Section>
  );
}

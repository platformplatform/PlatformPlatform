import type { ReactNode } from "react";

import { Body, Container, Head, Html, Preview, Section, Tailwind } from "@react-email/components";

import { emailHeaderBackground } from "../helpers/brand";
import { Footer } from "./Footer";
import { Header } from "./Header";

type TransactionalEmailProps = {
  locale: string;
  preview: string;
  // Optional render slot for the OTP autofill suffix. Rendered AFTER <Footer /> so the
  // `@<domain> #<code>` line lands on the very last line of the plaintext output, which is what
  // Apple Mail's domain-bound autofill scanner reads. Templates without an OTP simply omit this prop.
  otpAutofill?: ReactNode;
  children: ReactNode;
};

// System font stack — no @font-face. Apple Mail / Outlook / Gmail / Yahoo all support
// these natively. Keeping the list short avoids margin-of-error in legacy clients.
const FONT_STACK = "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";

// Custom <style> block injected into <Head> with STANDARD CSS (not nested). It serves two purposes:
//
// 1. Set <html> background-color in both modes. The body element only fills the content height; the
//    html element extends to the viewport. Without an explicit html bg the page area outside the
//    body shows whatever's behind it (white iframe chrome in webmail, a stark white margin in some
//    desktop clients). Setting html bg ensures the visible page is uniform light gray (light mode)
//    or neutral dark gray (dark mode) regardless of how the client wraps the message.
//
// 2. Dark-mode opt-in. React Email's Tailwind plugin emits non-standard nested syntax for dark:
//    variants ( .foo { @media ... { ... } } ) which some email clients misinterpret as
//    unconditional. Plain @media (prefers-color-scheme: dark) at the top level is the email-
//    industry-standard pattern and degrades gracefully — clients that strip <style> blocks or
//    don't honor the media query render the inline light defaults.
//
// Dark palette is neutral gray (matching Gmail/Outlook native dark mode) so downstream forks aren't
// coupled to a brand-specific navy. The body bg #1f1f1f is the same value Gmail/Outlook clients use
// when they auto-invert light themes, so the rendered email blends naturally with the client chrome.
//
// React Email's <Body> renders <body><table><tr><td style={...}>{children}</td></tr></table></body>.
// The body's `style` prop (background, color, font) lands on the INNER td — which has no class — so
// the dark-mode rules below also target ".email-body td" to flip that inner cell. Without this, the
// outer <body> recolors via @media but the inner td stays inline-light, leaving a visible light
// strip inside a dark frame (and unstyled text inside still inherits the inline dark color). The
// same descendant selector also flips the default text color for unclassed elements (paragraphs,
// divs) inside the card — they inherit the td's color.
// `headerBackground` is the brand-coded email-header band color -- a Scriban placeholder in build
// mode, the real platform-settings.jsonc value in preview mode (see helpers/brand). The .email-header
// dark-mode rules below keep the band at that color even in dark email clients: without them the
// `.email-card td !important` rule would re-paint the header's inner td with #2a2a2a, masking it.
function buildEmailStyles(headerBackground: string): string {
  return `
html { background-color: #f4f4f5; }
@media (prefers-color-scheme: dark) {
  html { background-color: #1f1f1f !important; }
  .email-body { background-color: #1f1f1f !important; }
  .email-body td { background-color: #1f1f1f !important; color: #e5e5e5 !important; }
  .email-card { background-color: #2a2a2a !important; color: #e5e5e5 !important; }
  .email-card td { background-color: #2a2a2a !important; color: #e5e5e5 !important; }
  .email-header { background-color: ${headerBackground} !important; }
  .email-header td { background-color: ${headerBackground} !important; }
  .email-heading { color: #fafafa !important; }
  .email-otp-box { background-color: #171717 !important; }
  .email-otp-box td { background-color: #171717 !important; }
  .email-otp-text { color: #fafafa !important; }
  .email-muted { color: #a3a3a3 !important; }
  .email-link { color: #e5e5e5 !important; }
  .email-button-default { background-color: #f6f6f6 !important; color: #171717 !important; }
  .email-progressbar-track { background-color: #404040 !important; }
  .email-progressbar-fill { background-color: #f6f6f6 !important; }
  .email-separator { border-top-color: #404040 !important; }
  .email-alert-default { border-color: #404040 !important; background-color: #2a2a2a !important; color: #e5e5e5 !important; }
  .email-avatar { background-color: #404040 !important; color: #d4d4d4 !important; }
}
/* Phones: card spans the full width flush to the top edge — drop the desktop floating margin and
   the corner radius (rounded corners on a full-bleed card look like a rendering glitch). */
@media (max-width: 600px) {
  .email-card { margin-top: 0 !important; border-radius: 0 !important; }
}
`.trim();
}

export function TransactionalEmail({ locale, preview, otpAutofill, children }: Readonly<TransactionalEmailProps>) {
  return (
    <Tailwind>
      <Html lang={locale}>
        <Head>
          <meta name="color-scheme" content="light dark" />
          <meta name="supported-color-schemes" content="light dark" />
          <style dangerouslySetInnerHTML={{ __html: buildEmailStyles(emailHeaderBackground()) }} />
        </Head>
        <Preview>{preview}</Preview>
        <Body style={{ fontFamily: FONT_STACK }} className="email-body m-[0px] bg-[#f4f4f5] p-[0px] text-[#0f172a]">
          <Container className="email-card mx-auto mt-[40px] mb-[24px] w-full max-w-[600px] overflow-hidden rounded-[12px] bg-white">
            <Header />
            <Section className="px-[32px] py-[32px]">{children}</Section>
          </Container>
          <Footer locale={locale} />
          {otpAutofill}
        </Body>
      </Html>
    </Tailwind>
  );
}

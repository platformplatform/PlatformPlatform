import { Img, Section } from "@react-email/components";

// Brand wordmark at the top of every transactional email. The logo PNG lives at
// application/main/WebApp/public/email/logo-640x88.png so it ships with the brand surface and
// survives changes to any individual email template — the URL stays valid even if every template
// is rewritten. The dimensions are baked into the filename so a future rebrand can introduce a new
// asset (e.g. logo-800x110.png) without invalidating the URL referenced by emails already in users'
// inboxes.
//
// The Scriban {{PublicUrl}} global resolves to the running deploy's host (localhost dev, staging,
// production) so the same markup works everywhere without per-environment template variants.
//
// PNG (not SVG) because Outlook desktop, Yahoo, and several webmail clients don't reliably render
// SVG. The source PNG is 640×88 (2× retina) displayed at 320×44 for crisp rendering on Apple
// ProMotion and high-DPI Android. Width/height attributes are required for Outlook's table-based
// renderer to size the image correctly even when CSS is stripped.
export function Header() {
  return (
    <Section className="email-header bg-[#11161f] px-[32px] py-[24px] text-center">
      <Img
        src="{{PublicUrl}}/email/logo-640x88.png"
        alt="{{ProductName}}"
        width="320"
        height="44"
        className="mx-auto"
      />
    </Section>
  );
}

import { Img, Section } from "@react-email/components";

import { emailHeaderBackground, emailProductName } from "../helpers/brand";

// Full-bleed brand banner at the top of every transactional email. The PNG lives at
// application/main/WebApp/public/email/logo-1200x184.png — the URL stays valid even if templates
// are rewritten. The Scriban {{PublicUrl}} global resolves to the running deploy's host (localhost
// dev, staging, production) so one markup works across environments.
//
// PNG (not SVG) because Outlook desktop, Yahoo, and several webmail clients don't render SVG
// reliably. The source is 1200×184 (2× retina) displayed at 600×92 — the full width of the email
// card — with the logo centered inside the PNG so placement is pixel-identical in every client.
// width="600" sizes it for Outlook's table renderer; width:100%/max-width:600px/height:auto scales
// it down on narrow mobile viewports instead of overflowing. The image is transparent; the band
// background is brand-coded -- one color for both light- and dark-mode email clients.
export function Header() {
  return (
    <Section className="email-header" style={{ backgroundColor: emailHeaderBackground() }}>
      <Img
        src="{{PublicUrl}}/email/logo-1200x184.png"
        alt={emailProductName()}
        width="600"
        style={{ width: "100%", maxWidth: "600px", height: "auto", display: "block" }}
      />
    </Section>
  );
}

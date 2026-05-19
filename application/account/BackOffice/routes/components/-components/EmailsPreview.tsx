import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { useState } from "react";

const TEMPLATES = ["StartSignup", "StartLogin", "ResendEmailLogin", "UnknownUser", "InviteUser"] as const;
const SUPPORTED_PREVIEW_LOCALES = ["en-US", "da-DK"] as const;
const FALLBACK_PREVIEW_LOCALE = "en-US";

type Template = (typeof TEMPLATES)[number];
type PreviewLocale = (typeof SUPPORTED_PREVIEW_LOCALES)[number];

export function EmailsPreview() {
  const { i18n } = useLingui();
  const [template, setTemplate] = useState<Template>("StartSignup");

  // The iframe locale follows the SPA's active locale (set by the avatar-menu locale switcher) so
  // designers don't have to toggle locales twice. Falls back to en-US if the user's chosen locale
  // doesn't have a rendered email artifact yet — the email build only emits the locales listed in
  // application/shared-webapp/infrastructure/translations/i18n.config.json that have .po catalogs.
  const locale: PreviewLocale = (SUPPORTED_PREVIEW_LOCALES as readonly string[]).includes(i18n.locale)
    ? (i18n.locale as PreviewLocale)
    : FALLBACK_PREVIEW_LOCALE;

  const iframeSrc = `/emails/assets/${template}.${locale}.preview.html`;

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-col gap-2">
        <h4>
          <Trans>Template</Trans>
        </h4>
        <div className="flex flex-wrap gap-2">
          {TEMPLATES.map((name) => (
            <Button
              key={name}
              variant={template === name ? "default" : "outline"}
              size="sm"
              onClick={() => setTemplate(name)}
            >
              {name}
            </Button>
          ))}
        </div>
      </div>

      <div className="overflow-hidden rounded-md border border-border">
        <iframe
          key={iframeSrc}
          src={iframeSrc}
          title={`${template} (${locale})`}
          className="block h-[40rem] w-full border-0 bg-white"
        />
      </div>
    </div>
  );
}

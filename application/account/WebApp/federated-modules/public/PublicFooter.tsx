import type { ReactNode } from "react";

import { t } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import { contactEmail, productName, socialLinks, webTaglines } from "@repo/infrastructure/branding";
import { Link } from "@repo/ui/components/Link";
import { Logo } from "@repo/ui/components/Logo";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { MailIcon } from "lucide-react";

// Brand icons removed from lucide-react v1 for trademark reasons; inlined here as SVGs.
function GithubIcon({ className }: { readonly className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
      <path d="M12 .5C5.65.5.5 5.65.5 12c0 5.08 3.29 9.39 7.86 10.91.58.1.79-.25.79-.56v-2c-3.2.7-3.87-1.36-3.87-1.36-.52-1.32-1.27-1.67-1.27-1.67-1.04-.71.08-.7.08-.7 1.15.08 1.76 1.18 1.76 1.18 1.02 1.76 2.69 1.25 3.35.96.1-.74.4-1.25.72-1.54-2.55-.29-5.24-1.27-5.24-5.66 0-1.25.45-2.27 1.18-3.07-.12-.29-.51-1.46.11-3.04 0 0 .96-.31 3.15 1.17a10.9 10.9 0 0 1 5.74 0c2.18-1.48 3.14-1.17 3.14-1.17.62 1.58.23 2.75.12 3.04.74.8 1.18 1.82 1.18 3.07 0 4.4-2.69 5.36-5.26 5.65.41.36.78 1.05.78 2.13v3.16c0 .31.21.67.8.55A11.5 11.5 0 0 0 23.5 12C23.5 5.65 18.35.5 12 .5z" />
    </svg>
  );
}

function LinkedinIcon({ className }: { readonly className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
      <path d="M20.45 20.45h-3.55v-5.57c0-1.33-.02-3.04-1.85-3.04-1.85 0-2.13 1.45-2.13 2.94v5.67H9.36V9h3.41v1.56h.05c.47-.9 1.64-1.85 3.37-1.85 3.6 0 4.27 2.37 4.27 5.46v6.28zM5.34 7.43a2.06 2.06 0 1 1 0-4.12 2.06 2.06 0 0 1 0 4.12zM7.12 20.45H3.56V9h3.56v11.45zM22.22 0H1.78C.79 0 0 .77 0 1.73v20.54C0 23.23.79 24 1.78 24h20.44c.99 0 1.78-.77 1.78-1.73V1.73C24 .77 23.21 0 22.22 0z" />
    </svg>
  );
}

function YoutubeIcon({ className }: { readonly className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
      <path d="M23.5 6.2a3 3 0 0 0-2.1-2.12C19.55 3.5 12 3.5 12 3.5s-7.55 0-9.4.58A3 3 0 0 0 .5 6.2 31.5 31.5 0 0 0 0 12a31.5 31.5 0 0 0 .5 5.8 3 3 0 0 0 2.1 2.12c1.85.58 9.4.58 9.4.58s7.55 0 9.4-.58a3 3 0 0 0 2.1-2.12A31.5 31.5 0 0 0 24 12a31.5 31.5 0 0 0-.5-5.8zM9.6 15.57V8.43L15.82 12 9.6 15.57z" />
    </svg>
  );
}

function XIcon({ className }: { readonly className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
      <path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231 5.45-6.231zm-1.161 17.52h1.833L7.084 4.126H5.117L17.083 19.77z" />
    </svg>
  );
}

function FacebookIcon({ className }: { readonly className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
      <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z" />
    </svg>
  );
}

function InstagramIcon({ className }: { readonly className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
      <path d="M12 2.163c3.204 0 3.584.012 4.85.07 3.252.148 4.771 1.691 4.919 4.919.058 1.265.069 1.645.069 4.849 0 3.205-.012 3.584-.069 4.849-.149 3.225-1.664 4.771-4.919 4.919-1.266.058-1.644.07-4.85.07-3.204 0-3.584-.012-4.849-.07-3.26-.149-4.771-1.699-4.919-4.92-.058-1.265-.07-1.644-.07-4.849 0-3.204.013-3.583.07-4.849.149-3.227 1.664-4.771 4.919-4.919 1.266-.057 1.645-.069 4.849-.069zM12 0C8.741 0 8.333.014 7.053.072 2.695.272.273 2.69.073 7.052.014 8.333 0 8.741 0 12c0 3.259.014 3.668.072 4.948.2 4.358 2.618 6.78 6.98 6.98C8.333 23.986 8.741 24 12 24c3.259 0 3.668-.014 4.948-.072 4.354-.2 6.782-2.618 6.979-6.98.059-1.28.073-1.689.073-4.948 0-3.259-.014-3.667-.072-4.947-.196-4.354-2.617-6.78-6.979-6.98C15.668.014 15.259 0 12 0zm0 5.838a6.162 6.162 0 1 0 0 12.324 6.162 6.162 0 0 0 0-12.324zM12 16a4 4 0 1 1 0-8 4 4 0 0 1 0 8zm6.406-11.845a1.44 1.44 0 1 0 0 2.881 1.44 1.44 0 0 0 0-2.881z" />
    </svg>
  );
}

interface SocialLinkButtonProps {
  readonly href: string;
  readonly ariaLabel: string;
  readonly tooltip: ReactNode;
  readonly children: ReactNode;
}

function SocialLinkButton({ href, ariaLabel, tooltip, children }: SocialLinkButtonProps) {
  if (!href) return null;
  return (
    <Tooltip>
      <TooltipTrigger
        render={
          <Link href={href} aria-label={ariaLabel} variant="icon" underline={false}>
            {children}
          </Link>
        }
      />
      <TooltipContent>{tooltip}</TooltipContent>
    </Tooltip>
  );
}

export default function PublicFooter() {
  const currentYear = new Date().getFullYear();
  const { i18n } = useLingui();
  const tagline = webTaglines[i18n.locale];

  return (
    <footer className="w-full bg-input-background">
      <div className="mx-auto max-w-[66rem] px-4 pt-14 pb-8 md:pb-10">
        {/* Main Footer Content - Desktop: side by side, Mobile: stacked */}
        <div className="flex flex-col items-center gap-8 sm:flex-row sm:items-center sm:gap-6">
          {/* Logo - Full logo on mobile, mark on desktop */}
          <div className="shrink-0">
            <Logo
              variant="wordmark"
              alt={t`${productName} logo`}
              className="h-10 w-auto opacity-90 transition-opacity hover:opacity-100 sm:hidden"
            />
            <Logo
              variant="mark"
              alt={t`${productName} logo`}
              className="hidden size-16 opacity-90 transition-opacity hover:opacity-100 sm:block"
            />
          </div>

          {/* Content */}
          <div className="flex flex-1 flex-col gap-8 text-center sm:text-left">
            {/* Description */}
            <div className="space-y-3">
              <h3 className="hidden sm:block">{productName}</h3>
              {tagline && <p className="leading-relaxed text-muted-foreground">{tagline}</p>}
            </div>
          </div>
        </div>

        {/* Bottom Section */}
        <div className="mt-14 flex flex-col items-center gap-6 border-t border-border pt-14 sm:flex-row sm:justify-between">
          {/* Left: Copyright and Legal Links - the legal links wrap to their own line as a single
              group when there is no room beside the copyright, and always stack below it on mobile. */}
          <div className="flex flex-col items-center gap-1 text-center text-muted-foreground sm:flex-row sm:flex-wrap sm:items-center sm:gap-x-6 sm:gap-y-1 sm:text-left">
            <div className="sm:shrink-0">
              <Trans>
                © {currentYear} {productName} - All rights reserved
              </Trans>
            </div>
            <div className="flex flex-wrap items-center justify-center gap-x-3 sm:shrink-0">
              <Link href="/legal/" className="text-muted-foreground hover:text-foreground">
                <Trans>Compliance</Trans>
              </Link>
              <span>·</span>
              <Link href="/legal/terms" className="text-muted-foreground hover:text-foreground">
                <Trans>Terms</Trans>
              </Link>
              <span>·</span>
              <Link href="/legal/privacy" className="text-muted-foreground hover:text-foreground">
                <Trans>Privacy</Trans>
              </Link>
              <span>·</span>
              <Link href="/legal/dpa" className="text-muted-foreground hover:text-foreground">
                <Trans>DPA</Trans>
              </Link>
            </div>
          </div>

          {/* Right: Social Links */}
          <div className="flex items-center gap-4">
            <SocialLinkButton
              href={contactEmail ? `mailto:${contactEmail}` : ""}
              ariaLabel={t`Email`}
              tooltip={<Trans>Email</Trans>}
            >
              <MailIcon className="size-5" />
            </SocialLinkButton>
            <SocialLinkButton href={socialLinks.linkedIn} ariaLabel="LinkedIn" tooltip={<Trans>LinkedIn</Trans>}>
              <LinkedinIcon className="size-5" />
            </SocialLinkButton>
            <SocialLinkButton href={socialLinks.x} ariaLabel="X" tooltip={<Trans>X</Trans>}>
              <XIcon className="size-5" />
            </SocialLinkButton>
            <SocialLinkButton href={socialLinks.youTube} ariaLabel="YouTube" tooltip={<Trans>YouTube</Trans>}>
              <YoutubeIcon className="size-5" />
            </SocialLinkButton>
            <SocialLinkButton href={socialLinks.gitHub} ariaLabel="GitHub" tooltip={<Trans>GitHub</Trans>}>
              <GithubIcon className="size-5" />
            </SocialLinkButton>
            <SocialLinkButton href={socialLinks.facebook} ariaLabel="Facebook" tooltip={<Trans>Facebook</Trans>}>
              <FacebookIcon className="size-5" />
            </SocialLinkButton>
            <SocialLinkButton href={socialLinks.instagram} ariaLabel="Instagram" tooltip={<Trans>Instagram</Trans>}>
              <InstagramIcon className="size-5" />
            </SocialLinkButton>
          </div>
        </div>
      </div>
    </footer>
  );
}

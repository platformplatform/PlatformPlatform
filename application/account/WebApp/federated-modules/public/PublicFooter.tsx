import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { productName, socialLinks, supportEmail } from "@repo/infrastructure/branding";
import { Link } from "@repo/ui/components/Link";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { MailIcon } from "lucide-react";

import logoMark from "@/shared/images/logo-mark.svg";
import logoWrap from "@/shared/images/logo-wrap.svg";

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

export default function PublicFooter() {
  const currentYear = new Date().getFullYear();

  return (
    <footer className="w-full bg-input-background">
      <div className="mx-auto max-w-7xl px-6 pt-14 pb-8 md:pb-10">
        {/* Main Footer Content - Desktop: side by side, Mobile: stacked */}
        <div className="flex flex-col items-center gap-8 sm:flex-row sm:items-center sm:gap-6">
          {/* Logo - Full logo on mobile, mark on desktop */}
          <div className="shrink-0">
            <img
              src={logoWrap}
              alt={t`${productName} logo`}
              className="h-10 w-[17.5rem] opacity-90 transition-opacity hover:opacity-100 sm:hidden"
              loading="lazy"
            />
            <img
              src={logoMark}
              alt={t`${productName} logo`}
              className="hidden size-16 opacity-90 transition-opacity hover:opacity-100 sm:block"
              loading="lazy"
            />
          </div>

          {/* Content */}
          <div className="flex flex-1 flex-col gap-8 text-center sm:text-left">
            {/* Description */}
            <div className="space-y-3">
              <h3 className="hidden sm:block">{productName}</h3>
              <p className="leading-relaxed text-muted-foreground">
                <Trans>Free, open-source .NET and React starter kit for building modern SaaS applications.</Trans>
              </p>
            </div>
          </div>
        </div>

        {/* Bottom Section */}
        <div className="mt-14 flex flex-col items-center gap-6 border-t border-border pt-14 sm:flex-row sm:justify-between">
          {/* Left: Copyright and Legal Links */}
          <div className="flex flex-wrap items-center justify-center gap-x-3 text-center text-muted-foreground sm:justify-start sm:text-left">
            <div>
              <Trans>
                © {currentYear} {productName}. All rights reserved.
              </Trans>
            </div>
            <span className="hidden sm:inline">·</span>
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

          {/* Right: Social Links */}
          <div className="flex items-center gap-4">
            <Tooltip>
              <TooltipTrigger
                render={
                  <Link href={`mailto:${supportEmail}`} aria-label={t`Email`} variant="icon" underline={false}>
                    <MailIcon className="size-5" />
                  </Link>
                }
              />
              <TooltipContent>
                <Trans>Email</Trans>
              </TooltipContent>
            </Tooltip>
            <Tooltip>
              <TooltipTrigger
                render={
                  <Link href={socialLinks.linkedIn} aria-label="LinkedIn" variant="icon" underline={false}>
                    <LinkedinIcon className="size-5" />
                  </Link>
                }
              />
              <TooltipContent>
                <Trans>LinkedIn</Trans>
              </TooltipContent>
            </Tooltip>
            <Tooltip>
              <TooltipTrigger
                render={
                  <Link href={socialLinks.youTube} aria-label="YouTube" variant="icon" underline={false}>
                    <YoutubeIcon className="size-5" />
                  </Link>
                }
              />
              <TooltipContent>
                <Trans>YouTube</Trans>
              </TooltipContent>
            </Tooltip>
            <Tooltip>
              <TooltipTrigger
                render={
                  <Link href={socialLinks.gitHub} aria-label="GitHub" variant="icon" underline={false}>
                    <GithubIcon className="size-5" />
                  </Link>
                }
              />
              <TooltipContent>
                <Trans>GitHub</Trans>
              </TooltipContent>
            </Tooltip>
          </div>
        </div>
      </div>
    </footer>
  );
}

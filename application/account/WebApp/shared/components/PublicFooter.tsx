import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Link } from "@repo/ui/components/Link";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { Github, Linkedin, MailIcon, Youtube } from "lucide-react";
import logoMark from "@/shared/images/logo-mark.svg";
import logoWrap from "@/shared/images/logo-wrap.svg";

export function PublicFooter() {
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
              alt={t`PlatformPlatform logo`}
              className="h-10 w-[17.5rem] opacity-90 transition-opacity hover:opacity-100 sm:hidden"
              loading="lazy"
            />
            <img
              src={logoMark}
              alt={t`PlatformPlatform logo`}
              className="hidden size-16 opacity-90 transition-opacity hover:opacity-100 sm:block"
              loading="lazy"
            />
          </div>

          {/* Content */}
          <div className="flex flex-1 flex-col gap-8 text-center sm:text-left">
            {/* Description */}
            <div className="space-y-3">
              <h3 className="hidden sm:block">
                <Trans>PlatformPlatform</Trans>
              </h3>
              <p className="text-muted-foreground leading-relaxed">
                <Trans>Free, open-source .NET and React starter kit for building modern SaaS applications.</Trans>
              </p>
            </div>
          </div>
        </div>

        {/* Bottom Section */}
        <div className="mt-14 flex flex-col items-center gap-6 border-border border-t pt-14 sm:flex-row sm:justify-between">
          {/* Left: Copyright and Legal Links */}
          <div className="flex flex-wrap items-center justify-center gap-x-3 text-center text-muted-foreground sm:justify-start sm:text-left">
            <div>
              <Trans>© {currentYear} PlatformPlatform. All rights reserved.</Trans>
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
                  <Link
                    href="mailto:support@platformplatform.net"
                    aria-label={t`Email`}
                    variant="icon"
                    underline={false}
                  >
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
                  <Link
                    href="https://www.linkedin.com/company/platformplatform/"
                    aria-label="LinkedIn"
                    variant="icon"
                    underline={false}
                  >
                    <Linkedin className="size-5" />
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
                  <Link
                    href="https://www.youtube.com/@PlatformPlatform"
                    aria-label="YouTube"
                    variant="icon"
                    underline={false}
                  >
                    <Youtube className="size-5" />
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
                  <Link
                    href="https://github.com/platformplatform/PlatformPlatform"
                    aria-label="GitHub"
                    variant="icon"
                    underline={false}
                  >
                    <Github className="size-5" />
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

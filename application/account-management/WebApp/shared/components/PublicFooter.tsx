import { Trans } from "@lingui/react/macro";
import { Image } from "@repo/ui/components/Image";
import { Link } from "@repo/ui/components/Link";
import { Tooltip, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { GithubIcon, LinkedinIcon, MailIcon, YoutubeIcon } from "lucide-react";
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
            <Image
              src={logoWrap}
              alt="PlatformPlatform"
              className="h-10 opacity-90 transition-opacity hover:opacity-100 sm:hidden"
              width={280}
              height={40}
              priority={true}
            />
            <Image
              src={logoMark}
              alt="PlatformPlatform"
              className="hidden h-16 opacity-90 transition-opacity hover:opacity-100 sm:block"
              width={64}
              height={64}
              priority={true}
            />
          </div>

          {/* Content */}
          <div className="flex flex-1 flex-col gap-8 text-center sm:text-left">
            {/* Description */}
            <div className="space-y-3">
              <h3 className="hidden font-semibold text-foreground text-lg sm:block">
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
          {/* Copyright */}
          <div className="text-center text-muted-foreground text-sm sm:text-left">
            <Trans>© {currentYear} PlatformPlatform. All rights reserved.</Trans>
          </div>

          {/* Social Links */}
          <div className="flex items-center gap-4">
            <TooltipTrigger>
              <a
                href="mailto:support@platformplatform.net"
                aria-label="Email"
                className="flex h-10 w-10 items-center justify-center rounded-lg bg-background/50 text-muted-foreground transition-all hover:bg-background hover:text-foreground"
              >
                <MailIcon className="h-5 w-5" />
              </a>
              <Tooltip>
                <Trans>Email</Trans>
              </Tooltip>
            </TooltipTrigger>
            <TooltipTrigger>
              <Link
                href="https://www.linkedin.com/company/platformplatform/"
                target="_blank"
                rel="noopener noreferrer"
                aria-label="LinkedIn"
                className="flex h-10 w-10 items-center justify-center rounded-lg bg-background/50 text-muted-foreground transition-all hover:bg-background hover:text-foreground"
              >
                <LinkedinIcon className="h-5 w-5" />
              </Link>
              <Tooltip>
                <Trans>LinkedIn</Trans>
              </Tooltip>
            </TooltipTrigger>
            <TooltipTrigger>
              <Link
                href="https://www.youtube.com/@PlatformPlatform"
                target="_blank"
                rel="noopener noreferrer"
                aria-label="YouTube"
                className="flex h-10 w-10 items-center justify-center rounded-lg bg-background/50 text-muted-foreground transition-all hover:bg-background hover:text-foreground"
              >
                <YoutubeIcon className="h-5 w-5" />
              </Link>
              <Tooltip>
                <Trans>YouTube</Trans>
              </Tooltip>
            </TooltipTrigger>
            <TooltipTrigger>
              <Link
                href="https://github.com/platformplatform/PlatformPlatform"
                target="_blank"
                rel="noopener noreferrer"
                aria-label="GitHub"
                className="flex h-10 w-10 items-center justify-center rounded-lg bg-background/50 text-muted-foreground transition-all hover:bg-background hover:text-foreground"
              >
                <GithubIcon className="h-5 w-5" />
              </Link>
              <Tooltip>
                <Trans>GitHub</Trans>
              </Tooltip>
            </TooltipTrigger>
          </div>
        </div>
      </div>
    </footer>
  );
}

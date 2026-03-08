import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Card, CardDescription, CardTitle } from "@repo/ui/components/Card";
import { Link } from "@repo/ui/components/Link";
import {
  DatabaseIcon,
  ExternalLinkIcon,
  FingerprintIcon,
  KeyIcon,
  LayersIcon,
  ServerIcon,
  ShieldCheckIcon
} from "lucide-react";

import gdprBadge from "@/shared/images/compliance/gdpr.png";
import platformLogo from "@/shared/images/logo-mark.svg";

export function AzureInfrastructureSection() {
  return (
    <section className="bg-input-background px-6 py-16">
      <div className="mx-auto max-w-5xl">
        <div className="mb-12 text-center">
          <h2 className="marketing">
            <Trans>Enterprise-grade Azure infrastructure</Trans>
          </h2>
          <p className="text-muted-foreground">
            <Trans>Microsoft Azure Platform-as-a-Service (PaaS) with enterprise-grade reliability.</Trans>
          </p>
        </div>

        <ComplianceBadges />
        <SecurityFeaturesGrid />
        <PlatformPlatformCredit />
      </div>
    </section>
  );
}

function ComplianceBadges() {
  return (
    <div className="mb-12 flex flex-wrap items-center justify-center gap-6 sm:gap-8 md:gap-12">
      <div className="flex flex-col items-center gap-3">
        <div className="flex size-24 items-center justify-center">
          <img src={gdprBadge} alt={t`GDPR compliant`} className="h-20 w-auto object-contain" />
        </div>
        <span className="font-semibold text-foreground">
          <Trans>GDPR Compliant</Trans>
        </span>
      </div>
      <div className="flex flex-col items-center gap-3">
        <div className="flex size-24 items-center justify-center rounded-full bg-[#0078d4]/10">
          <svg viewBox="0 0 96 96" className="size-14" fill="none" xmlns="http://www.w3.org/2000/svg">
            <title>Microsoft Azure</title>
            <path d="M48 12L12 30v36l36 18 36-18V30L48 12z" fill="#0078d4" />
            <path d="M48 12v36L12 30l36-18z" fill="#50e6ff" />
            <path d="M48 48v36l36-18V30L48 48z" fill="#0078d4" />
            <path d="M48 48L12 30v36l36 18V48z" fill="#1490df" />
          </svg>
        </div>
        <span className="font-semibold text-foreground">
          <Trans>Microsoft Azure</Trans>
        </span>
      </div>
    </div>
  );
}

function SecurityFeaturesGrid() {
  return (
    <div className="mb-12 grid gap-6 md:grid-cols-2 lg:grid-cols-3">
      <Card className="px-6 transition-colors hover:bg-hover-background">
        <div className="mb-3 flex size-10 items-center justify-center rounded-lg bg-primary/10">
          <ServerIcon className="size-6 text-primary" />
        </div>
        <CardTitle>
          <Trans>Fully managed infrastructure</Trans>
        </CardTitle>
        <CardDescription>
          <Trans>Microsoft patches and secures all PaaS infrastructure automatically</Trans>
        </CardDescription>
      </Card>
      <Card className="px-6 transition-colors hover:bg-hover-background">
        <div className="mb-3 flex size-10 items-center justify-center rounded-lg bg-primary/10">
          <FingerprintIcon className="size-6 text-primary" />
        </div>
        <CardTitle>
          <Trans>Managed identities</Trans>
        </CardTitle>
        <CardDescription>
          <Trans>Only trusted personnel access data through Azure AD authentication</Trans>
        </CardDescription>
      </Card>
      <Card className="px-6 transition-colors hover:bg-hover-background">
        <div className="mb-3 flex size-10 items-center justify-center rounded-lg bg-primary/10">
          <LayersIcon className="size-6 text-primary" />
        </div>
        <CardTitle>
          <Trans>Environment isolation</Trans>
        </CardTitle>
        <CardDescription>
          <Trans>Strict separation prevents production data from leaking to other environments</Trans>
        </CardDescription>
      </Card>
      <Card className="px-6 transition-colors hover:bg-hover-background">
        <div className="mb-3 flex size-10 items-center justify-center rounded-lg bg-primary/10">
          <KeyIcon className="size-6 text-primary" />
        </div>
        <CardTitle>
          <Trans>Passwordless deployments</Trans>
        </CardTitle>
        <CardDescription>
          <Trans>GitHub deploys directly to Azure without passwords or API keys</Trans>
        </CardDescription>
      </Card>
      <Card className="px-6 transition-colors hover:bg-hover-background">
        <div className="mb-3 flex size-10 items-center justify-center rounded-lg bg-primary/10">
          <DatabaseIcon className="size-6 text-primary" />
        </div>
        <CardTitle>
          <Trans>Data residency</Trans>
        </CardTitle>
        <CardDescription>
          <Trans>Your data stays in the Azure region you select at signup</Trans>
        </CardDescription>
      </Card>
      <Card className="px-6 transition-colors hover:bg-hover-background">
        <div className="mb-3 flex size-10 items-center justify-center rounded-lg bg-primary/10">
          <ShieldCheckIcon className="size-6 text-primary" />
        </div>
        <CardTitle>
          <Trans>100% Security Score</Trans>
        </CardTitle>
        <CardDescription>
          <Trans>Achieved in Microsoft Defender for Cloud following Azure best practices</Trans>
        </CardDescription>
      </Card>
    </div>
  );
}

function PlatformPlatformCredit() {
  return (
    <div className="flex items-center justify-center gap-3">
      <img src={platformLogo} alt={t`PlatformPlatform logo`} className="size-10" loading="lazy" />
      <p className="text-sm text-muted-foreground">
        <Trans>
          Built on{" "}
          <Link
            href="https://github.com/platformplatform/PlatformPlatform"
            target="_blank"
            rel="noopener noreferrer"
            aria-label={t`PlatformPlatform on GitHub (opens in new window)`}
            className="inline-flex items-center gap-1 font-medium text-primary"
          >
            PlatformPlatform
            <ExternalLinkIcon className="size-3" />
          </Link>{" "}
          - an open-source platform by industry experts showcasing how to build enterprise-grade B2B SaaS products
        </Trans>
      </p>
    </div>
  );
}
